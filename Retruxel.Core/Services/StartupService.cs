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
    /// targets: list of registered targets passed from the shell.
    /// toolRegistry: registry for visual tools (IVisualTool).
    /// toolLoader: loader for standard tools (ITool).
    /// basePath: application base directory.
    /// </summary>
    public static async Task InitializeAsync(
        IProgress<string> progress,
        IEnumerable<Core.Interfaces.ITarget> targets,
        object toolRegistry,
        object toolLoader,
        string basePath)
    {
        var loc = LocalizationService.Instance;

        // 1. Localization — already loaded in App_Startup, just confirm
        progress.Report(loc.Get("startup.load_localization"));
        await Task.Delay(50);

        // 2. Targets — received from shell
        progress.Report(loc.Get("startup.scan_targets"));
        foreach (var target in targets)
            progress.Report($"  TARGET: {target.TargetId.ToUpper()}");
        await Task.Delay(50);

        // 3. Toolchain verification — the only real work at startup
        progress.Report(loc.Get("startup.check_toolchain"));
        await VerifyToolchainsAsync(progress, targets);

        // 4. Tool discovery — discover ITool and IVisualTool plugins
        progress.Report(loc.Get("startup.scan_plugins"));
        await DiscoverToolsAsync(progress, toolRegistry, toolLoader, basePath);

        progress.Report(loc.Get("startup.ready"));
        await Task.Delay(100);
    }

    // ── Toolchain Verification ────────────────────────────────────────────────

    /// <summary>
    /// Checks whether toolchain binaries are present for each registered target.
    /// Reports found/missing status per target.
    /// Binaries are extracted on first build — missing here is not a fatal error.
    /// </summary>
    private static async Task VerifyToolchainsAsync(IProgress<string> progress, IEnumerable<Core.Interfaces.ITarget> targets)
    {
        await Task.Run(() =>
        {
            foreach (var target in targets)
            {
                var binaryPaths = target.GetRequiredToolchainBinaries()
                    .Select(relativePath => Path.Combine(ToolchainRoot, relativePath))
                    .ToArray();

                var allFound = binaryPaths.All(File.Exists);
                var status = allFound ? "OK" : "NOT EXTRACTED";
                progress.Report($"  TOOLCHAIN [{target.TargetId.ToUpper()}]: {status}");

                if (!allFound)
                {
                    // Report which specific binary is missing for diagnosis
                    foreach (var path in binaryPaths.Where(p => !File.Exists(p)))
                        progress.Report($"    MISSING: {Path.GetFileName(path)}");
                }
            }
        });
    }

    // ── Tool Discovery ────────────────────────────────────────────────

    /// <summary>
    /// Discovers and registers tools from Plugins/Tools/ folder.
    /// Reports progress for each tool discovered.
    /// </summary>
    private static async Task DiscoverToolsAsync(
        IProgress<string> progress,
        object toolRegistry,
        object toolLoader,
        string basePath)
    {
        await Task.Run(() =>
        {
            // Discover standard tools (ITool) via ToolLoader
            try
            {
                var discoverMethod = toolLoader.GetType().GetMethod("DiscoverTools");
                discoverMethod?.Invoke(toolLoader, null);

                var getToolsMethod = toolLoader.GetType().GetMethod("GetAllTools");
                if (getToolsMethod != null)
                {
                    var tools = getToolsMethod.Invoke(toolLoader, null) as System.Collections.IEnumerable;
                    if (tools != null)
                    {
                        foreach (var tool in tools)
                        {
                            var toolIdProp = tool.GetType().GetProperty("ToolId");
                            if (toolIdProp != null)
                            {
                                var toolId = toolIdProp.GetValue(tool) as string;
                                progress.Report($"  TOOL: {toolId?.ToUpper()}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress.Report($"  WARN: Tool discovery failed - {ex.Message}");
            }

            // Discover visual tools (IVisualTool) via ToolRegistry
            try
            {
                var pluginsPath = Path.Combine(basePath, "Plugins");
                var discoverToolsMethod = toolRegistry.GetType().GetMethod("DiscoverTools");
                if (discoverToolsMethod != null)
                {
                    var progressWrapper = new Progress<string>(msg => progress.Report(msg));
                    discoverToolsMethod.Invoke(toolRegistry, new object[] { pluginsPath, progressWrapper });
                }
            }
            catch (Exception ex)
            {
                progress.Report($"  WARN: Visual tool discovery failed - {ex.Message}");
            }
        });
    }
}
