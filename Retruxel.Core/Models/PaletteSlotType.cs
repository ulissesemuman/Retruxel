namespace Retruxel.Core.Models;

/// <summary>
/// Defines the usage type of a palette slot on the target hardware.
/// </summary>
public enum PaletteSlotType
{
    /// <summary>
    /// Used only by background tiles.
    /// </summary>
    Background,

    /// <summary>
    /// Used only by sprites.
    /// </summary>
    Sprite,

    /// <summary>
    /// Can be used by both background and sprites.
    /// </summary>
    Shared
}
