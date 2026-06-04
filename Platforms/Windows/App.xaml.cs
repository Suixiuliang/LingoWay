using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LingoWay.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            TrySetMicaBackdrop();
        }

        private void TrySetMicaBackdrop()
        {
            try
            {
                var window = Application.Windows.OfType<Microsoft.UI.Xaml.Window>().FirstOrDefault();
                if (window == null) return;
                window.SystemBackdrop = new MicaBackdrop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mica backdrop failed: {ex.Message}");
            }
        }
    }

}
