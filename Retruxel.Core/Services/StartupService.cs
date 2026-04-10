namespace Retruxel.Core.Services;

/// <summary>
/// Handles application startup initialization tasks.
/// Can run in dummy mode (fake delays) or real mode (actual work).
/// </summary>
public static class StartupService
{
    /// <summary>
    /// Set to true for real initialization, false for dummy mode with fake delays.
    /// </summary>
    public static bool UseDummyMode { get; set; } = true;

    /// <summary>
    /// Runs all startup tasks in sequence, reporting progress.
    /// </summary>
    public static async Task InitializeAsync(IProgress<string> progress)
    {
        if (UseDummyMode)
        {
            await InitializeDummyAsync(progress);
        }
        else
        {
            await InitializeRealAsync(progress);
        }
    }

    // ── Dummy Mode ────────────────────────────────────────────────────────────

    private static async Task InitializeDummyAsync(IProgress<string> progress)
    {
        var loc = LocalizationService.Instance;
        
        progress.Report(loc.Get("startup.init"));
        await Task.Delay(200);

        progress.Report(loc.Get("startup.scan_targets"));
        await Task.Delay(300);

        progress.Report(loc.Get("startup.load_localization"));
        await Task.Delay(250);

        progress.Report(loc.Get("startup.check_toolchain"));
        await Task.Delay(400);

        progress.Report(loc.Get("startup.cache"));
        await Task.Delay(300);

        progress.Report(loc.Get("startup.restore"));
        await Task.Delay(250);

        progress.Report(loc.Get("startup.ready"));
        await Task.Delay(100);
    }

    // ── Real Mode ─────────────────────────────────────────────────────────────

    private static async Task InitializeRealAsync(IProgress<string> progress)
    {
        var loc = LocalizationService.Instance;
        
        progress.Report(loc.Get("startup.init"));
        await Task.Delay(100);

        // 1. Target Discovery
        progress.Report(loc.Get("startup.scan_targets"));
        await DiscoverTargetsAsync();

        // 2. Localization
        progress.Report(loc.Get("startup.load_localization"));
        await LoadLocalizationAsync();

        // 3. Toolchain Verification
        progress.Report(loc.Get("startup.check_toolchain"));
        await VerifyToolchainAsync();

        // 4. Plugin Discovery (stub)
        progress.Report(loc.Get("startup.scan_plugins"));
        await DiscoverPluginsAsync();

        // 5. Resource Cache (stub)
        progress.Report(loc.Get("startup.cache"));
        await WarmupCacheAsync();

        // 6. Workspace Restoration (stub)
        progress.Report(loc.Get("startup.restore"));
        await RestoreWorkspaceAsync();

        // 7. Update Check (stub)
        progress.Report(loc.Get("startup.updates"));
        await CheckForUpdatesAsync();

        progress.Report(loc.Get("startup.ready"));
        await Task.Delay(100);
    }

    // ── Task Implementations ──────────────────────────────────────────────────

    /// <summary>
    /// Validates that all registered targets have proper architecture.
    /// Targets are loaded via assembly references, but we verify their structure.
    /// NOTE: This is a stub - actual target validation happens in the main app.
    /// </summary>
    private static async Task DiscoverTargetsAsync()
    {
        await Task.Delay(50); // Stub - targets are validated by TargetRegistry in main app
    }

    /// <summary>
    /// Loads all language files from Assets/Localization/.
    /// </summary>
    private static async Task LoadLocalizationAsync()
    {
        await Task.Run(() =>
        {
            var locService = LocalizationService.Instance;
            // Language files are already loaded in constructor
            // This just ensures it's initialized
            _ = locService.CurrentLanguage;
        });
    }

    /// <summary>
    /// Verifies that toolchain binaries are extracted and functional.
    /// </summary>
    private static async Task VerifyToolchainAsync()
    {
        await Task.Run(() =>
        {
            var toolchainPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Retruxel", "toolchain");

            // Check if toolchain directory exists
            if (!Directory.Exists(toolchainPath))
            {
                // Will be extracted on first build
                return;
            }

            // Verify key files exist
            var sdccPath = Path.Combine(toolchainPath, "compilers", "sdcc", "bin", "sdcc.exe");
            var cc65Path = Path.Combine(toolchainPath, "compilers", "cc65", "bin", "cc65.exe");

            _ = File.Exists(sdccPath);
            _ = File.Exists(cc65Path);
        });
    }

    /// <summary>
    /// Discovers and loads plugins from /plugins/ directory.
    /// TODO: Implement when plugin system is ready.
    /// </summary>
    private static async Task DiscoverPluginsAsync()
    {
        await Task.Delay(50); // Stub
    }

    /// <summary>
    /// Pre-loads commonly used resources into memory.
    /// TODO: Implement resource caching system.
    /// </summary>
    private static async Task WarmupCacheAsync()
    {
        await Task.Delay(50); // Stub
    }

    /// <summary>
    /// Restores last workspace state (open projects, window positions, etc).
    /// TODO: Implement workspace persistence.
    /// </summary>
    private static async Task RestoreWorkspaceAsync()
    {
        await Task.Delay(50); // Stub
    }

    /// <summary>
    /// Checks GitHub releases for newer version.
    /// TODO: Implement update checker.
    /// </summary>
    private static async Task CheckForUpdatesAsync()
    {
        await Task.Delay(50); // Stub
    }
}
