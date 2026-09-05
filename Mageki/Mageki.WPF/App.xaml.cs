using System.Windows;
using Wpf.Ui.Appearance;

namespace Mageki.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationThemeManager.ApplySystemTheme();
            base.OnStartup(e);
            Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Mageki.StaticIO.Shutdown();
        }
    }
}
