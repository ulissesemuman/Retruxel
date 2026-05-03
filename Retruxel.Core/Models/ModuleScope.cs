namespace Retruxel.Core.Models;

/// <summary>
/// Determines where a module is initialized in the generated code.
/// </summary>
public enum ModuleScope
{
    /// <summary>
    /// Module is initialized once in main() OnStart.
    /// Persists across all scene transitions.
    /// Example: Input, Physics, Sprite (player), Animation, GameVar
    /// </summary>
    Project,

    /// <summary>
    /// Module is initialized inside scene_X_init().
    /// Reloaded every time the scene is entered.
    /// Example: Palette, Tilemap, Scroll, Text.display (static), Enemy, Sprite (boss)
    /// </summary>
    Scene
}
