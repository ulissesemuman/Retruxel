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
                VramRegions = new[]
                {
                    new VramRegion("pattern0", "Pattern Table 0", 0, 255),
                    new VramRegion("pattern1", "Pattern Table 1", 256, 511)
                },
                MaxSpritesOnScreen = 64
            },
            "snes" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp (most common mode)
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 2047)
                },
                MaxSpritesOnScreen = 128
            },
            "sms" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 447)
                },
                MaxSpritesOnScreen = 64
            },
            "sg1000" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 2, // 1bpp (TMS9918)
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 511)
                },
                MaxSpritesOnScreen = 32
            },
            "gg" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 447)
                },
                MaxSpritesOnScreen = 64
            },
            "gb" or "gbc" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 4, // 2bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 383)
                },
                MaxSpritesOnScreen = 40
            },
            "gba" => new TargetSpecs
            {
                ScreenWidth = 240,
                ScreenHeight = 160,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 256, // 8bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 1023)
                },
                MaxSpritesOnScreen = 128
            },
            "ws" or "wsc" => new TargetSpecs
            {
                ScreenWidth = 224,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 511)
                },
                MaxSpritesOnScreen = 128
            },
            "pce" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 2047)
                },
                MaxSpritesOnScreen = 64
            },
            _ => null
        };
    }
}
