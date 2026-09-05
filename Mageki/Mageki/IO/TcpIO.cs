using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Mageki
{
    public class TcpIO : IO
    {
        private byte helloRandomValue;
        private Timer heartbeatTimer = new Timer(400) { AutoReset = true };
        private Timer disconnectTimer = new Timer(1500) { AutoReset = false };
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream networkStream;
        private bool connecting = false;
        private Thread readingThread;
        private byte[] _inBuffer = new byte[32];
        private bool disposedValue;
        public int Port { get; private set; }

        public TcpIO()
            : this(Settings.Port) { }

        public TcpIO(int port)
        {
            Port = port;
        }

        public override void Init()
        {
            try
            {
                helloRandomValue = (byte)(new Random().Next() % 255);
                if (disposedValue)
                    throw new ObjectDisposedException(GetType().Name);

                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.Start();
                disconnectTimer.Elapsed += DisconnectTimer_Elapsed;

                IPAddress ip = new IPAddress(new byte[] { 0, 0, 0, 0 });
                listener = new TcpListener(ip, Port);
                listener.Start();

                readingThread = new Thread(PollThread);
                readingThread.IsBackground = true;
                Status = Status.Disconnected;
                readingThread.Start();
            }
            catch (Exception e)
            {
                App.Logger.Error(e);
                Status = Status.Error;
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                SendMessage(new byte[] { (byte)MessageType.Hello, helloRandomValue });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void DisconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Disconnect();
        }

        public async void Reconnect()
        {
            if (connecting || disposedValue)
                return;
            connecting = true;
            Disconnect();
            try
            {
                var newClient = await listener.AcceptTcpClientAsync();
                networkStream = newClient.GetStream();
                client = newClient;
                Status = Status.Connected;
                RequestValues();
                disconnectTimer.Stop();
                disconnectTimer.Start();
            }
            catch (Exception ex)
            {
                await Task.Delay(1000);
                App.Logger.Error(ex);
            }
            connecting = false;
        }

        private void Disconnect()
        {
            var tmpClient = client;
            var tmpStream = networkStream;
            client = null;
            networkStream = null;
            tmpClient?.Dispose();
            tmpStream?.Dispose();
            if (Status == Status.Connected)
                Status = Status.Disconnected;
        }

        public override void SetGameButton(int index, byte value)
        {
            base.SetGameButton(index, value);
            SendMessage(new byte[] { (byte)MessageType.ButtonStatus, (byte)index, value });
        }

        public override void SetLever(short value)
        {
            base.SetLever(value);
            SendMessage(
                new byte[] { (byte)MessageType.MoveLever }
                    .Concat(BitConverter.GetBytes(value))
                    .ToArray()
            );
        }

        public override void SetAime(byte scanning, byte[] packet)
        {
            base.SetAime(scanning, packet);
            SendMessage(
                new byte[] { (byte)MessageType.Scan, Convert.ToByte(scanning) }
                    .Concat(Data.AimePacket)
                    .ToArray()
            );
        }

        public override void SetOptionButton(OptionButtons button, bool pressed)
        {
            base.SetOptionButton(button, pressed);
            MessageType type = button switch
            {
                OptionButtons.Test => MessageType.Test,
                OptionButtons.Service => MessageType.Service,
                _ => throw new NotImplementedException(),
            };
            SendMessage(new byte[] { (byte)type, Convert.ToByte(pressed) }.ToArray());
        }

        private void SendMessage(byte[] data)
        {
            if (Status != Status.Connected && data[0] != (byte)MessageType.Hello)
            {
                return;
            }
            if (!disposedValue)
            {
                var stream = networkStream;
                if (!client?.Connected ?? false || stream == null)
                {
                    Reconnect();
                    return;
                }
                try
                {
                    stream.Write(data, 0, data.Length);
                }
                catch
                {
                    if (!disposedValue)
                    {
                        Disconnect();
                    }
                }
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        private void PollThread()
        {
            while (!disposedValue)
            {
                try
                {
                    var stream = networkStream;
                    if ((!client?.Connected) ?? true || stream == null)
                    {
                        Reconnect();
                        continue;
                    }
                    int len = stream.Read(_inBuffer, 0, 1);
                    if (len <= 0)
                    {
                        // Server closed the connection (EOF)
                        Disconnect();
                        continue;
                    }
                    Receive((MessageType)_inBuffer[0]);
                }
                catch (Exception ex)
                {
                    if (!disposedValue)
                    {
                        Debug.WriteLine(ex);
                        Disconnect();
                    }
                }
            }
        }

        private void Receive(MessageType type)
        {
            var stream = networkStream;
            if (stream == null)
                return;
            int i = 0;
            if (type == MessageType.SetLed && (i = stream.Read(_inBuffer, 0, 18)) > 0)
            {
                SetLed(_inBuffer);
            }
            else if (type == MessageType.SetLever && stream.Read(_inBuffer, 0, 2) > 0)
            {
                short lever = BitConverter.ToInt16(_inBuffer, 0);
                SetLever(lever);
            }
            else if (type == MessageType.Hello && stream.Read(_inBuffer, 0, 1) > 0)
            {
                if (Status != Status.Connected)
                {
                    Status = Status.Connected;
                    RequestValues();
                }
                disconnectTimer.Stop();
                disconnectTimer.Start();
            }
        }

        private void RequestValues()
        {
            SendMessage(new byte[] { (byte)MessageType.RequestValues });
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (Status == Status.Connected)
                        Status = Status.Disconnected;
                    networkStream?.Dispose();
                    client?.Dispose();
                    listener?.Stop();
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~IO()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public override void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
