using System.Windows;

namespace Retruxel;

public partial class App : Application
{
    private async void App_Startup(object sender, StartupEventArgs e)
    {
        var splash = new SplashScreen();
        splash.Show();

        var mainWindow = new MainWindow();

        await splash.RunAsync(async progress =>
        {
            progress.Report("INITIALIZING_CORE...");
            await Task.Delay(300);

            progress.Report("LOADING_GRAPHIC_MODULES...");
            await Task.Delay(300);

            progress.Report("LOADING_LOGIC_MODULES...");
            await Task.Delay(300);

            progress.Report("LOADING_AUDIO_MODULES...");
            await Task.Delay(300);

            progress.Report("LOADING_TARGET_SMS...");
            await Task.Delay(300);

            progress.Report("EXTRACTING_TOOLCHAIN...");
            await Task.Delay(300);

            progress.Report("VERIFYING_TOOLCHAIN...");
            await Task.Delay(300);

            progress.Report("SYSTEM_READY");
            await Task.Delay(200);

        }, () =>
        {
            mainWindow.Show();
            splash.Close();
        });
    }
}