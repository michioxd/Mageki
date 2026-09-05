using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Mageki.WPF
{
    public partial class SettingsWindow : FluentWindow
    {
        private bool _loading;

        public SettingsWindow()
        {
            _loading = true;
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            LoadSettings();
        }

        private void LoadSettings()
        {
            _loading = true;
            PortBox.Text = Settings.Port.ToString();
            IpBox.Text = Settings.IP;
            TcpRadio.IsChecked = Settings.Protocol == Protocol.TCP;
            UdpRadio.IsChecked = Settings.Protocol == Protocol.UDP;
            HideButtonsCheck.IsChecked = Settings.HideGameButtons;
            BottomMarginSlider.Value = Settings.ButtonBottomMargin;
            AntiMisTouchCheck.IsChecked = Settings.AntiMisTouch;
            RelativeRadio.IsChecked = Settings.LeverMoveMode == LeverMoveMode.Relative;
            AbsoluteRadio.IsChecked = Settings.LeverMoveMode == LeverMoveMode.Absolute;
            LinearitySlider.Value = Settings.LeverLinearity;
            OverflowCheck.IsChecked = Settings.EnableLeverOverflowHandling;
            CompositeCheck.IsChecked = Settings.EnableCompositeMode;
            HapticCheck.IsChecked = Settings.EnableHapticFeedback;
            AimeBox.Text = Settings.AimeId;
            StatusText.Text = StaticIO.Status.ToString();
            CanvasThemeCombo.SelectedIndex = (int)Settings.CanvasTheme;
            UpdateLabels();
            _loading = false;
        }

        private void Protocol_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            Settings.Protocol = TcpRadio.IsChecked == true ? Protocol.TCP : Protocol.UDP;
        }

        private void LeverMode_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            Settings.LeverMoveMode =
                RelativeRadio.IsChecked == true ? LeverMoveMode.Relative : LeverMoveMode.Absolute;
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            Settings.HideGameButtons = HideButtonsCheck.IsChecked == true;
            Settings.AntiMisTouch = AntiMisTouchCheck.IsChecked == true;
            Settings.EnableLeverOverflowHandling = OverflowCheck.IsChecked == true;
            Settings.EnableCompositeMode = CompositeCheck.IsChecked == true;
            Settings.EnableHapticFeedback = HapticCheck.IsChecked == true;
        }

        private void CanvasTheme_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || CanvasThemeCombo.SelectedItem is not ComboBoxItem item)
                return;

            Settings.CanvasTheme = Enum.Parse<CanvasTheme>(item.Tag?.ToString() ?? "Auto");
        }

        private void BottomMargin_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading)
                return;
            Settings.ButtonBottomMargin = (float)e.NewValue;
            UpdateLabels();
        }

        private void Linearity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading)
                return;
            Settings.LeverLinearity = (int)e.NewValue;
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (BottomMarginSlider != null && BottomMarginText != null)
                BottomMarginText.Text = BottomMarginSlider.Value.ToString("F2");
            if (LinearitySlider != null && LinearityText != null)
                LinearityText.Text = $"1/{(int)LinearitySlider.Value}";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ushort.TryParse(PortBox.Text, out var port) && port > 0)
                Settings.Port = port;
            if (IpBox.Text != null)
                Settings.IP = IpBox.Text.Trim();
            Settings.AimeId = AimeBox.Text?.Trim() ?? string.Empty;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
