using System;
using System.Threading.Tasks;
using System.Windows;
using AppCleanerPro.Views;

namespace AppCleanerPro
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Remove the default StartupUri flow — we handle it manually
            var splash = new SplashWindow();
            splash.Show();

            await splash.AnimateAndLoad();

            var main = new MainWindow();
            main.Show();

            splash.Close();
        }
    }
}
