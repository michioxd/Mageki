using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using Wpf.Ui.Appearance;

namespace Mageki.WPF
{
    public partial class App : Application
    {
        private const string MutexName = "Mageki.WPF.SingleInstance";
        private const string PipeName = "Mageki.WPF.Activate";

        private Mutex? _mutex;
        private Thread? _pipeThread;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);

            _mutex = new Mutex(true, MutexName, out bool isNew);
            if (!isNew)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    client.Connect(1000);
                    using var writer = new StreamWriter(client);
                    writer.WriteLine("activate");
                }
                catch { }

                _mutex.Dispose();
                Shutdown();
                return;
            }

            _pipeThread = new Thread(PipeServer) { IsBackground = true };
            _pipeThread.Start();

            ApplicationThemeManager.ApplySystemTheme();
            base.OnStartup(e);
            Exit += App_Exit;
        }

        private void PipeServer()
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1);
                    server.WaitForConnection();
                    using var reader = new StreamReader(server);
                    var msg = reader.ReadLine();
                    if (msg == "activate")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var win = MainWindow;
                            if (win == null)
                                return;
                            if (win.WindowState == WindowState.Minimized)
                                win.WindowState = WindowState.Normal;
                            win.Activate();
                            win.Focus();
                        });
                    }
                }
                catch { }
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Mageki.StaticIO.Shutdown();
        }
    }
}
