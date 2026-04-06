using System.IO;
using System.Windows;
using Retruxel.Core.Services;
using Retruxel.Services;

namespace Retruxel;

public partial class App : Application
{
    private async void App_Startup(object sender, StartupEventArgs e)
    {
        // Load settings
        var settings = SettingsService.Load();

        // Initialize localization
        var localizationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Localization");
        LocalizationService.Instance.DiscoverLanguages(localizationPath);
        
        // Auto-detect system language on first run
        var languageToLoad = settings.General.Language;
        if (string.IsNullOrEmpty(languageToLoad))
        {
            languageToLoad = LocalizationService.Instance.DetectSystemLanguage();
            settings.General.Language = languageToLoad;
            SettingsService.Save(settings);
        }
        
        LocalizationService.Instance.Load(languageToLoad, localizationPath);

        // Initialize target registry
        TargetRegistry.Initialize();

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