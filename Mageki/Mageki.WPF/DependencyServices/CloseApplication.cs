using Mageki.DependencyServices;

namespace Mageki.WPF.DependencyServices
{
    /// <summary>
    /// WPF: Close the application window.
    /// </summary>
    public class CloseApplication : ICloseApplication
    {
        public void Close()
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
