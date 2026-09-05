using System;
using System.Windows;
using Wpf.Ui.Appearance;

namespace Mageki.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Enable WM_POINTER stack for accurate multi-touch & pen input on Tablet PC / Surface.
            // Must be called before any Window is created.
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);

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
