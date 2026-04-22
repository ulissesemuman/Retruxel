using Retruxel.Core.Interfaces;
using Retruxel.Toolchain.Builders;
using System.Reflection;

namespace Retruxel.Toolchain;

/// <summary>
/// Central orchestrator that discovers and routes build requests to appropriate toolchain builders.
/// Supports both auto-discovery from Plugins/Toolchains/*.dll and custom builders from targets.
/// </summary>
public static class ToolchainOrchestrator
{
    private static Dictionary<string, IToolchainBuilder>? _builderCache;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a toolchain builder for the specified target.
    /// First checks for custom builder override, then searches discovered builders, finally falls back to embedded builders.
    /// </summary>
    public static IToolchainBuilder GetBuilder(string targetId, object? customBuilder = null)
    {
        // Priority 1: Custom builder provided by target
        if (customBuilder is IToolchainBuilder builder)
            return builder;

        // Priority 2: Auto-discovered builders from Plugins/Toolchains/
        EnsureBuildersDiscovered();
        if (_builderCache!.TryGetValue(targetId.ToLowerInvariant(), out var discoveredBuilder))
            return discoveredBuilder;

        // Priority 3: Embedded builders (backward compatibility)
        return targetId.ToLowerInvariant() switch
        {
            "sms" => new SmsToolchainBuilder(),
            "nes" => new NesToolchainBuilder(),
            "gg" or "gamegear" => new GameGearToolchainBuilder(),
            "sg1000" => new Sg1000ToolchainBuilder(),
            "coleco" or "colecovision" => new ColecoVisionToolchainBuilder(),
            _ => throw new NotSupportedException($"No toolchain builder found for target '{targetId}'. " +
                                                  $"Ensure a builder with TargetId='{targetId}' exists in Plugins/Toolchains/ or provide a custom builder.")
        };
    }

    /// <summary>
    /// Discovers all IToolchainBuilder implementations from Plugins/Toolchains/*.dll
    /// </summary>
    private static void EnsureBuildersDiscovered()
    {
        if (_builderCache != null) return;

        lock (_lock)
        {
            if (_builderCache != null) return;

            _builderCache = new Dictionary<string, IToolchainBuilder>(StringComparer.OrdinalIgnoreCase);

            var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Toolchains");
            if (!Directory.Exists(pluginsPath))
                return;

            var dlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);

            foreach (var dll in dlls)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    var builderTypes = assembly.GetTypes()
                        .Where(t => typeof(IToolchainBuilder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in builderTypes)
                    {
                        if (Activator.CreateInstance(type) is IToolchainBuilder builder)
                        {
                            _builderCache[builder.TargetId.ToLowerInvariant()] = builder;
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be loaded or don't contain builders
                }
            }
        }
    }

    /// <summary>
    /// Clears the builder cache. Used for testing or hot-reload scenarios.
    /// </summary>
    public static void ClearCache()
    {
        lock (_lock)
        {
            _builderCache = null;
        }
    }
}
