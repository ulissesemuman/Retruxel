namespace Retruxel.Core.Models;

/// <summary>
/// Defines the rendering strategy for HUD (Heads-Up Display) modules.
/// Different retro hardware requires different approaches to render fixed UI elements.
/// </summary>
public enum HudStrategy
{
    /// <summary>
    /// Hardware window plane that does not scroll with the background.
    /// Supported by: SMS, GG, SNES, Mega Drive, Game Boy, Game Boy Color.
    /// </summary>
    WindowPlane,

    /// <summary>
    /// Mid-frame scroll reset via scanline IRQ.
    /// The HUD region appears fixed by zeroing scroll at a specific scanline.
    /// Supported by: NES.
    /// </summary>
    MidFrameScroll,

    /// <summary>
    /// HUD rendered entirely with sprites.
    /// Used on hardware without window plane or scroll control.
    /// Supported by: SG-1000, ColecoVision.
    /// </summary>
    SpriteOnly,

    /// <summary>
    /// Target has no HUD support. HUD module will warn the user.
    /// </summary>
    None
}
