namespace Retruxel.Core.Models;

/// <summary>
/// Allows targets to override module behavior without modifying the module itself.
/// Used to adapt universal modules to target-specific hardware constraints.
/// </summary>
public class ModuleOverride
{
    /// <summary>
    /// Module ID to override (e.g., "palette", "tilemap", "scroll").
    /// </summary>
    public string ModuleId { get; init; } = string.Empty;

    /// <summary>
    /// Override singleton behavior.
    /// - true: Only one instance allowed per project (e.g., SMS palette with 32 fixed colors)
    /// - false: Multiple instances allowed (e.g., multiple tilemaps for layers)
    /// - null: Use module's default IsSingleton value
    /// </summary>
    public bool? IsSingleton { get; init; }

    /// <summary>
    /// Maximum number of instances allowed (null = unlimited).
    /// Useful for hardware limits (e.g., SMS has exactly 2 palettes: BG + Sprite).
    /// </summary>
    public int? MaxInstances { get; init; }

    /// <summary>
    /// Additional validation rules specific to this target.
    /// Returns error messages if validation fails, empty if valid.
    /// </summary>
    public Func<string, IEnumerable<string>>? CustomValidator { get; init; }
}
