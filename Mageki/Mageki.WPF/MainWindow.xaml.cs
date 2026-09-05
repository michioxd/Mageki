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

            // Mouse (fallback for non-touch devices)
            SkiaCanvas.MouseDown += Canvas_MouseDown;
            SkiaCanvas.MouseMove += Canvas_MouseMove;
            SkiaCanvas.MouseUp += Canvas_MouseUp;

            // WPF Touch events (multi-touch fingers)
            SkiaCanvas.TouchDown += Canvas_TouchDown;
            SkiaCanvas.TouchMove += Canvas_TouchMove;
            SkiaCanvas.TouchUp += Canvas_TouchUp;
            SkiaCanvas.LostTouchCapture += Canvas_LostTouchCapture;

            // Stylus / Pen events (Surface Pen, active stylus)
            SkiaCanvas.StylusDown += Canvas_StylusDown;
            SkiaCanvas.StylusMove += Canvas_StylusMove;
            SkiaCanvas.StylusUp += Canvas_StylusUp;
            SkiaCanvas.StylusLeave += Canvas_StylusLeave;

            // Disable manipulation to avoid the built-in 100 ms delay on touch
            SkiaCanvas.IsManipulationEnabled = false;

            // Prevent stylus from promoting to mouse events (avoids ghost clicks on tablet)
            Stylus.SetIsPressAndHoldEnabled(SkiaCanvas, false);
            Stylus.SetIsFlicksEnabled(SkiaCanvas, false);
            Stylus.SetIsTapFeedbackEnabled(SkiaCanvas, false);
            Stylus.SetIsTouchFeedbackEnabled(SkiaCanvas, false);
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
            // Ignore synthesised mouse events that WPF promotes from touch/stylus
            if (e.StylusDevice != null)
                return;

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
            if (e.StylusDevice != null)
                return;
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
            if (e.StylusDevice != null)
                return;
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

        // Release touch when capture is lost (e.g. system dialog pops up)
        private void Canvas_LostTouchCapture(object? sender, TouchEventArgs e)
        {
            _panel.OnTouchAction(e.TouchDevice.Id, TouchActionType.Cancelled, SKPoint.Empty);
            SkiaCanvas.InvalidateVisual();
        }

        // Stylus / Pen — use a dedicated high ID range to avoid conflicts with finger touch IDs
        private const long StylusTouchId = 0xF000_0001L;

        private void Canvas_StylusDown(object? sender, StylusDownEventArgs e)
        {
            e.StylusDevice.Capture(SkiaCanvas);
            var pos = e.GetPosition(SkiaCanvas);
            _panel.OnTouchAction(
                StylusTouchId,
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

        private void Canvas_StylusMove(object? sender, StylusEventArgs e)
        {
            var pos = e.GetPosition(SkiaCanvas);
            _panel.OnTouchAction(
                StylusTouchId,
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

        private void Canvas_StylusUp(object? sender, StylusEventArgs e)
        {
            _panel.OnTouchAction(StylusTouchId, TouchActionType.Released, SKPoint.Empty);
            SkiaCanvas.InvalidateVisual();
            e.Handled = true;
        }

        private void Canvas_StylusLeave(object? sender, StylusEventArgs e)
        {
            _panel.OnTouchAction(StylusTouchId, TouchActionType.Cancelled, SKPoint.Empty);
            SkiaCanvas.InvalidateVisual();
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
