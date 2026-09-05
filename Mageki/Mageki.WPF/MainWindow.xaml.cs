using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Mageki.Drawables;
using Mageki.TouchTracking;
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
            SystemThemeWatcher.Watch(this, WindowBackdropType.None, false);
            Mageki.PreferenceStore.Current = new WpfPreferenceStore();

            ServiceLocator.Register<Mageki.DependencyServices.ICloseApplication>(
                new CloseApplication()
            );
            ServiceLocator.Register<Mageki.DependencyServices.INfcService>(new NfcService());

            _panel = new ControllerPanelWPF(SkiaCanvas);

            StaticIO.OnStatusChanged += StaticIO_StatusChanged;
            UpdateStatusText();

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
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
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
                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                WindowState = _previousWindowState;
                _isFullScreen = false;
                FullScreenButton.Content = "Full screen";
                return;
            }

            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
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
