using Retruxel.Core.Interfaces;
using Retruxel.Target.ColecoVision;
using Retruxel.Target.GG;
using Retruxel.Target.NES;
using Retruxel.Target.SG1000;
using Retruxel.Target.SMS;

namespace Retruxel.Services;

/// <summary>
/// Centralized registry of all available target platforms.
/// Used by WelcomeView, TargetSelectionDialog, and other components that need target information.
/// </summary>
public static class TargetRegistry
{
    private static readonly List<ITarget> _targets =
    [
        new SmsTarget(),
        new NesTarget(),
        new GgTarget(),
        new SG1000Target(),
        new ColecoVisionTarget()
    ];

    private static HashSet<string> _manufacturers = new();

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
    /// Initializes the registry by discovering manufacturers.
    /// Called during application startup.
    /// </summary>
    public static void Initialize()
    {
        _manufacturers.Clear();
        foreach (var target in _targets)
        {
            if (!string.IsNullOrEmpty(target.Specs.Manufacturer))
            {
                _manufacturers.Add(target.Specs.Manufacturer);
            }
        }
    }
}
