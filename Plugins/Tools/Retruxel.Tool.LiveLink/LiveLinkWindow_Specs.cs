using Retruxel.Core.Models;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Console hardware specifications (independent of Retruxel targets).
/// </summary>
public partial class LiveLinkWindow
{
    /// <summary>
    /// Returns hardware specs for a given console based on real hardware specifications.
    /// These specs are independent of Retruxel target implementations.
    /// LiveLink always uses these specs to ensure accurate capture from emulator.
    /// </summary>
    private TargetSpecs? GetConsoleSpecs(string console)
    {
        return console switch
        {
            "nes" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 240,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 4, // 2bpp
                MaxSpritesOnScreen = 64
            },
            "snes" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp (most common mode)
                MaxSpritesOnScreen = 128
            },
            "sms" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                MaxSpritesOnScreen = 64
            },
            "sg1000" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 2, // 1bpp (TMS9918)
                MaxSpritesOnScreen = 32
            },
            "gg" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                MaxSpritesOnScreen = 64
            },
            "gb" or "gbc" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 4, // 2bpp
                MaxSpritesOnScreen = 40
            },
            "gba" => new TargetSpecs
            {
                ScreenWidth = 240,
                ScreenHeight = 160,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 256, // 8bpp
                MaxSpritesOnScreen = 128
            },
            "ws" or "wsc" => new TargetSpecs
            {
                ScreenWidth = 224,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                MaxSpritesOnScreen = 128
            },
            "pce" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                MaxSpritesOnScreen = 64
            },
            _ => null
        };
    }
}
