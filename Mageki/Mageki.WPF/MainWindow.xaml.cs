using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Mageki.Drawables;
using Mageki.TouchTracking;
using Mageki.Utils;
using Mageki.WPF.DependencyServices;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Mageki.WPF
{
    public partial class MainWindow : FluentWindow
    {
        private readonly ControllerPanelWPF _panel;
        private SettingsWindow? _settingsWindow;
        private bool _isFullScreen;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
            Mageki.PreferenceStore.Current = new WpfPreferenceStore();

            ServiceLocator.Register<Mageki.DependencyServices.ICloseApplication>(
                new CloseApplication()
            );
            ServiceLocator.Register<Mageki.DependencyServices.INfcService>(new NfcService());

            _panel = new ControllerPanelWPF(SkiaCanvas);

            StaticIO.OnStatusChanged += StaticIO_StatusChanged;
            UpdateStatusText();
            UpdateCardButton();

            SkiaCanvas.MouseDown += Canvas_MouseDown;
            SkiaCanvas.MouseMove += Canvas_MouseMove;
            SkiaCanvas.MouseUp += Canvas_MouseUp;
            SkiaCanvas.TouchDown += Canvas_TouchDown;
            SkiaCanvas.TouchMove += Canvas_TouchMove;
            SkiaCanvas.TouchUp += Canvas_TouchUp;
            SkiaCanvas.IsManipulationEnabled = true;
        }

        private void UpdateStatusText()
        {
            var status = StaticIO.Status;
            StatusText.Text = status.ToString();
            StatusText.Foreground = status switch
            {
                Status.Connected => System.Windows.Media.Brushes.LightGreen,
                _ => System.Windows.Media.Brushes.Gray,
            };
        }

        private void UpdateCardButton()
        {
            CardButton.IsEnabled = !string.IsNullOrWhiteSpace(Settings.AimeId);
        }

        private bool _cardScanning;

        private async void CardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cardScanning)
                return;

            // Build 10-byte BCD packet from configured Aime ID string.
            byte[] aimePacket = Enumerable.Repeat((byte)0xFF, 10).ToArray();
            if (BigInteger.TryParse(Settings.AimeId, out BigInteger id))
            {
                byte[] bcd = id.ToBcd();
                // right-align into 10 bytes (pad with 0x00 on the left)
                int offset = 10 - bcd.Length;
                Array.Copy(bcd, 0, aimePacket, offset, bcd.Length);
            }

            _cardScanning = true;
            CardButton.IsEnabled = false;
            CardButton.Content = "Scanning...";
            try
            {
                StaticIO.SetAime(1, aimePacket);
                await Task.Delay(3000);
                StaticIO.SetAime(0, Array.Empty<byte>());
            }
            finally
            {
                _cardScanning = false;
                CardButton.Content = "Card";
                UpdateCardButton();
            }
        }

        private void StaticIO_StatusChanged(object sender, OnStatusChangedEventArgs e)
        {
            if (!Dispatcher.HasShutdownStarted)
                Dispatcher.Invoke(UpdateStatusText);
        }

        private void SkiaCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _panel.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow { Owner = this };
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                // Refresh card button state after settings are saved.
                UpdateCardButton();
            };
            _settingsWindow.Show();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void TestButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Test, true);
        }

        private void TestButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Test, false);
        }

        private void TestButton_TouchDown(object? sender, TouchEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Test, true);
            e.Handled = true;
        }

        private void TestButton_TouchUp(object? sender, TouchEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Test, false);
            e.Handled = true;
        }

        private void ServiceButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Service, true);
        }

        private void ServiceButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Service, false);
        }

        private void ServiceButton_TouchDown(object? sender, TouchEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Service, true);
            e.Handled = true;
        }

        private void ServiceButton_TouchUp(object? sender, TouchEventArgs e)
        {
            StaticIO.SetOptionButton(OptionButtons.Service, false);
            e.Handled = true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (_isFullScreen)
            {
                _isFullScreen = false;
                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                WindowState = _previousWindowState;
                FullScreenButton.Content = "Full screen";
                return;
            }

            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;
            _previousWindowState =
                WindowState == WindowState.Maximized ? WindowState.Normal : WindowState;

            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
            FullScreenButton.Content = "Exit full screen";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (_isFullScreen && WindowState != WindowState.Maximized)
            {
                _isFullScreen = false;
                FullScreenButton.Content = "Full screen";
                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            StaticIO.OnStatusChanged -= StaticIO_StatusChanged;
            _settingsWindow?.Close();
            base.OnClosed(e);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SkiaCanvas.CaptureMouse();
            var pos = e.GetPosition(SkiaCanvas);
            _panel.OnTouchAction(
                0,
                TouchActionType.Pressed,
                ToSkPoint(
                    pos,
                    SkiaCanvas.ActualWidth,
                    SkiaCanvas.ActualHeight,
                    SkiaCanvas.CanvasSize.Width,
                    SkiaCanvas.CanvasSize.Height
                )
            );
            SkiaCanvas.InvalidateVisual();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            var pos = e.GetPosition(SkiaCanvas);
            _panel.OnTouchAction(
                0,
                TouchActionType.Moved,
                ToSkPoint(
                    pos,
                    SkiaCanvas.ActualWidth,
                    SkiaCanvas.ActualHeight,
                    SkiaCanvas.CanvasSize.Width,
                    SkiaCanvas.CanvasSize.Height
                )
            );
            SkiaCanvas.InvalidateVisual();
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SkiaCanvas.ReleaseMouseCapture();
            _panel.OnTouchAction(0, TouchActionType.Released, SKPoint.Empty);
            SkiaCanvas.InvalidateVisual();
        }

        private void Canvas_TouchDown(object? sender, TouchEventArgs e)
        {
            e.TouchDevice.Capture(SkiaCanvas);
            var pos = e.GetTouchPoint(SkiaCanvas).Position;
            _panel.OnTouchAction(
                e.TouchDevice.Id,
                TouchActionType.Pressed,
                ToSkPoint(
                    pos,
                    SkiaCanvas.ActualWidth,
                    SkiaCanvas.ActualHeight,
                    SkiaCanvas.CanvasSize.Width,
                    SkiaCanvas.CanvasSize.Height
                )
            );
            SkiaCanvas.InvalidateVisual();
            e.Handled = true;
        }

        private void Canvas_TouchMove(object? sender, TouchEventArgs e)
        {
            var pos = e.GetTouchPoint(SkiaCanvas).Position;
            _panel.OnTouchAction(
                e.TouchDevice.Id,
                TouchActionType.Moved,
                ToSkPoint(
                    pos,
                    SkiaCanvas.ActualWidth,
                    SkiaCanvas.ActualHeight,
                    SkiaCanvas.CanvasSize.Width,
                    SkiaCanvas.CanvasSize.Height
                )
            );
            SkiaCanvas.InvalidateVisual();
            e.Handled = true;
        }

        private void Canvas_TouchUp(object? sender, TouchEventArgs e)
        {
            _panel.OnTouchAction(e.TouchDevice.Id, TouchActionType.Released, SKPoint.Empty);
            SkiaCanvas.InvalidateVisual();
            e.Handled = true;
        }

        private static SKPoint ToSkPoint(
            System.Windows.Point pos,
            double wpfW,
            double wpfH,
            float skW,
            float skH
        )
        {
            return new SKPoint((float)(skW * pos.X / wpfW), (float)(skH * pos.Y / wpfH));
        }
    }
}
