using Retruxel.Core.Services;
using Retruxel.Services;
using System.IO;
using System.Windows;

namespace Retruxel;

public partial class App : Application
{
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

        // 3. Target Registry — shell owns this, Core never references it directly
        TargetRegistry.Initialize();
        var targetIds = TargetRegistry.GetAllTargets().Select(t => t.TargetId);

        // 4. Splash + real startup tasks
        var splash = new SplashScreen();
        var mainWindow = new MainWindow();

        splash.Show();

        await splash.RunAsync(
            progress => StartupService.InitializeAsync(progress, targetIds),
            () =>
            {
                mainWindow.Show();
                splash.Close();
            });
    }
}
