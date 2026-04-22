using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.IO;
using System.Windows;

namespace Retruxel;

public partial class App : Application
{
    public static ToolRegistry ToolRegistry { get; private set; } = null!;
    public static ToolLoader ToolLoader { get; private set; } = null!;

    private async void App_Startup(object sender, StartupEventArgs e)
    {
        // 1. Settings
        var settings = SettingsService.Load();

        // 2. Localization
        var locPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Localization");
        LocalizationService.Instance.DiscoverLanguages(locPath);

        var language = settings.General.Language;
        if (string.IsNullOrEmpty(language))
        {
            language = LocalizationService.Instance.DetectSystemLanguage();
            settings.General.Language = language;
            SettingsService.Save(settings);
        }

        LocalizationService.Instance.Load(language, locPath);

        // 2.1. Register localization service in SDK for plugin access
        RetruxelServices.Localization = LocalizationService.Instance;

        // 3. Target Registry — shell owns this, Core never references it directly
        TargetRegistry.Initialize();
        var targets = TargetRegistry.GetAllTargets();

        // 4. Tool Discovery — initialize before splash to make available globally
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        ToolLoader = new ToolLoader(basePath);
        ToolRegistry = new ToolRegistry();

        // 5. Splash + real startup tasks
        var splash = new SplashScreen();
        var mainWindow = new MainWindow();

        splash.Show();

        await splash.RunAsync(
            progress => StartupService.InitializeAsync(progress, targets, ToolRegistry, ToolLoader, basePath),
            () =>
            {
                mainWindow.Show();
                splash.Close();
            });
    }
}
