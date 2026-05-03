namespace Retruxel.Core.Models;

/// <summary>
/// Defines how many instances of a module can exist.
/// Replaces the boolean IsSingleton with more granular control.
/// </summary>
public enum SingletonPolicy
{
    /// <summary>
    /// One instance per project. Shared across all scenes.
    /// Adding a second instance is allowed but triggers a warning in the editor:
    /// "You already have X (Global). Adding another allows runtime switching."
    /// Example default: Input, Physics, Animation
    /// </summary>
    Global,

    /// <summary>
    /// One instance per scene. Different scenes can have their own instance.
    /// Adding a second instance in the same scene triggers a warning.
    /// Example default: Palette, Scroll
    /// </summary>
    PerScene,

    /// <summary>
    /// Multiple instances allowed anywhere.
    /// Example default: Tilemap, Sprite, Enemy, GameVar, TextDisplay
    /// </summary>
    Multiple
}
