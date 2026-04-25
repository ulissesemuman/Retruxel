using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Centralized registry of all available target platforms.
/// Discovers targets dynamically from Plugins/Targets/ folder.
/// </summary>
public static class TargetRegistry
{
    private const string TargetsFolder = "Plugins/Targets";
    private static readonly List<ITarget> _targets = new();
    private static readonly HashSet<string> _manufacturers = new();

    /// <summary>
    /// Gets all registered target platforms.
    /// </summary>
    public static IReadOnlyList<ITarget> GetAllTargets() => _targets;

    /// <summary>
    /// Gets a target by its ID.
    /// </summary>
    public static ITarget? GetTargetById(string targetId)
    {
        return _targets.FirstOrDefault(t => t.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all unique manufacturers from registered targets.
    /// </summary>
    public static IReadOnlySet<string> GetManufacturers() => _manufacturers;

    /// <summary>
    /// Initializes the registry by discovering targets from Plugins/Targets/ folder.
    /// Called during application startup.
    /// </summary>
    public static void Initialize()
    {
        _targets.Clear();
        _manufacturers.Clear();

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var targetsPath = Path.Combine(basePath, TargetsFolder);

        if (!Directory.Exists(targetsPath))
        {
            Console.WriteLine($"WARN: Targets folder not found: {targetsPath}");
            return;
        }

        foreach (var dll in Directory.GetFiles(targetsPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var targetTypes = assembly.GetTypes()
                    .Where(t => typeof(ITarget).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in targetTypes)
                {
                    try
                    {
                        var target = (ITarget)Activator.CreateInstance(type)!;
                        _targets.Add(target);

                        if (!string.IsNullOrEmpty(target.Specs.Manufacturer))
                            _manufacturers.Add(target.Specs.Manufacturer);

                        Console.WriteLine($"TARGET_LOADED: {target.DisplayName} ({target.TargetId})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WARN: Failed to instantiate target {type.Name} — {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARN: Failed to load {Path.GetFileName(dll)} — {ex.Message}");
            }
        }

        Console.WriteLine($"TARGET_REGISTRY: {_targets.Count} targets loaded");
    }
}
