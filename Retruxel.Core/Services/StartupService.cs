using System.IO;

namespace Retruxel.Core.Services;

/// <summary>
/// Handles application startup initialization.
/// Responsibilities at startup:
///   - Report registered targets to the splash screen
///   - Verify that toolchain binaries are present for each target
///
/// NOT responsible for:
///   - Loading modules (depends on target — happens in MainWindow.OnProjectCreated)
///   - Plugin discovery (stub until plugin system is ready)
/// </summary>
public static class StartupService
{
    private static readonly string ToolchainRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "toolchain");

    /// <summary>
    /// Runs all startup tasks in sequence, reporting progress to the splash screen.
    /// targetIds: list of registered target IDs passed from the shell — avoids
    /// a circular reference between Retruxel.Core and the shell's TargetRegistry.
    /// </summary>
    public static async Task InitializeAsync(IProgress<string> progress, IEnumerable<string> targetIds)
    {
        var loc = LocalizationService.Instance;

        // 1. Localization — already loaded in App_Startup, just confirm
        progress.Report(loc.Get("startup.load_localization"));
        await Task.Delay(50);

        // 2. Targets — received from shell, no registry reference needed here
        progress.Report(loc.Get("startup.scan_targets"));
        foreach (var id in targetIds)
            progress.Report($"  TARGET: {id.ToUpper()}");
        await Task.Delay(50);

        // 3. Toolchain verification — the only real work at startup
        progress.Report(loc.Get("startup.check_toolchain"));
        await VerifyToolchainsAsync(progress);

        // 4. Stubs — placeholders for future systems
        progress.Report(loc.Get("startup.scan_plugins"));
        await Task.Delay(50);

        progress.Report(loc.Get("startup.ready"));
        await Task.Delay(100);
    }

    // ── Toolchain Verification ────────────────────────────────────────────────

    /// <summary>
    /// Checks whether toolchain binaries are present for each registered target.
    /// Reports found/missing status per target.
    /// Binaries are extracted on first build — missing here is not a fatal error.
    /// </summary>
    private static async Task VerifyToolchainsAsync(IProgress<string> progress)
    {
        await Task.Run(() =>
        {
            foreach (var (targetId, binaryPaths) in GetToolchainBinaries())
            {
                var allFound = binaryPaths.All(File.Exists);
                var status   = allFound ? "OK" : "NOT EXTRACTED";
                progress.Report($"  TOOLCHAIN [{targetId.ToUpper()}]: {status}");

                if (!allFound)
                {
                    // Report which specific binary is missing for diagnosis
                    foreach (var path in binaryPaths.Where(p => !File.Exists(p)))
                        progress.Report($"    MISSING: {Path.GetFileName(path)}");
                }
            }
        });
    }

    /// <summary>
    /// Returns the expected binary paths per target.
    /// These will be extracted to ToolchainRoot on first build.
    /// </summary>
    private static Dictionary<string, string[]> GetToolchainBinaries() => new()
    {
        ["sms"] =
        [
            Path.Combine(ToolchainRoot, "compilers", "sdcc", "bin", "sdcc.exe"),
            Path.Combine(ToolchainRoot, "utils",     "sega", "bin", "ihx2sms.exe")
        ],
        ["gg"] =
        [
            Path.Combine(ToolchainRoot, "compilers", "sdcc", "bin", "sdcc.exe"),
            Path.Combine(ToolchainRoot, "utils",     "sega", "bin", "ihx2sms.exe")
        ],
        ["sg1000"] =
        [
            Path.Combine(ToolchainRoot, "compilers", "sdcc", "bin", "sdcc.exe")
        ],
        ["coleco"] =
        [
            Path.Combine(ToolchainRoot, "compilers", "sdcc", "bin", "sdcc.exe")
        ],
        ["nes"] =
        [
            Path.Combine(ToolchainRoot, "compilers", "cc65", "bin", "cc65.exe"),
            Path.Combine(ToolchainRoot, "compilers", "cc65", "bin", "ld65.exe")
        ]
    };
}
