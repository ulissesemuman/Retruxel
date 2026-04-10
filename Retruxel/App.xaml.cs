using System.IO;
using System.Windows;
using Retruxel.Core.Services;
using Retruxel.Services;

namespace Retruxel;

public partial class App : Application
{
    private async void App_Startup(object sender, StartupEventArgs e)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // STARTUP MODE CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════════
        // Set to false for real initialization (slower but functional)
        // Set to true for dummy mode (fast fake delays for development)
        StartupService.UseDummyMode = false;
        // ═══════════════════════════════════════════════════════════════════════

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
            await StartupService.InitializeAsync(progress);
        }, () =>
        {
            mainWindow.Show();
            splash.Close();
        });
    }
}