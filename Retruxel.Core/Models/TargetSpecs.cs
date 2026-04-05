namespace Retruxel.Core.Models;

/// <summary>
/// Hardware specifications of a target console.
///
/// Divided into two concerns:
///   - Build specs: used by the toolchain and code generator
///   - Editor specs: used by the visual editor, tools and UI
/// </summary>
public class TargetSpecs
{
    /// <summary>Screen resolution in pixels. Ex: 256x192 (SMS), 256x240 (NES)</summary>
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }

    // Tiles 

    /// <summary>Tile size in pixels. Almost universally 8×8 on third-gen consoles.</summary>
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;

    /// <summary>
    /// Maximum number of unique tiles loadable in VRAM simultaneously.
    /// Ex: 448 (SMS), 256 (NES CHR-ROM page)
    /// </summary>
    public int MaxTilesInVram { get; set; }

    // Colors & Palettes 

    /// <summary>Total distinct colors the hardware can produce.
    /// Ex: 64 (SMS), 54 (NES), 32768 (SNES)</summary>
    public int TotalColors { get; set; }

    /// <summary>
    /// Bits per color channel used by the hardware DAC.
    /// Ex: 2 (SMS), 3 (NES approximation), 5 (SNES)
    /// Used to document color depth — actual colors come from ITarget.GetHardwarePalette().
    /// </summary>
    public int ColorDepthBitsPerChannel { get; set; }

    /// <summary>Maximum colors per tile (from a single palette slot).
    /// Ex: 16 (SMS), 4 (NES), 16 (SNES)</summary>
    public int ColorsPerTile { get; set; }

    /// <summary>
    /// Maximum colors visible per palette slot.
    /// Ex: 16 (SMS), 4 (NES BG), 16 (SNES)
    /// </summary>
    public int ColorsPerPalette { get; set; }

    /// <summary>
    /// Total palette slots available simultaneously on screen.
    /// Ex: 2 (SMS — Palette 0 and Palette 1), 8 (NES — 4 BG + 4 sprite)
    /// </summary>
    public int SimultaneousPalettes { get; set; }

    /// <summary>How many simultaneous palettes are available for background tiles.
    /// Ex: 2 (SMS), 4 (NES)</summary>
    public int BgPalettes { get; set; }

    /// <summary>How many simultaneous palettes are available for sprites.
    /// Ex: 2 (SMS), 4 (NES)</summary>
    public int SpritePalettes { get; set; }

    // Sprites 
    /// <summary>Maximum sprites rendered per scanline before flickering.</summary>
    public int SpritesPerScanline { get; set; }

    /// <summary>Maximum sprites on screen simultaneously.</summary>
    public int MaxSpritesOnScreen { get; set; }

    /// <summary>Base sprite size in pixels. Ex: 8×8 (SMS default), 8×8 (NES)</summary>
    public int SpriteWidth { get; set; } = 8;
    public int SpriteHeight { get; set; } = 8;

    /// <summary>
    /// Whether the hardware supports a double-height sprite mode.
    /// Ex: true (SMS supports 8×16), false (NES handles this via OAM layout)
    /// </summary>
    public bool SupportsDoubleHeightSprites { get; set; }

    // Memory

    /// <summary>Available RAM in bytes. Ex: 8192 (SMS), 2048 (NES)</summary>
    public int RamBytes { get; set; }

    /// <summary>Maximum ROM size in bytes.</summary>
    public int RomMaxBytes { get; set; }

    // CPU

    /// <summary>CPU name. Ex: "Zilog Z80", "MOS 6502", "Motorola 68000"</summary>
    public string CPU { get; set; } = string.Empty;

    /// <summary>CPU clock speed in Hz. Ex: 3546893 (SMS PAL)</summary>
    public int CpuClockHz { get; set; }

    // Sound

    /// <summary>Sound chip name. Ex: "SN76489", "2A03", "SPC700"</summary>
    public string SoundChip { get; set; } = string.Empty;

    /// <summary>Number of tone channels on the sound chip.</summary>
    public int SoundToneChannels { get; set; }

    /// <summary>Number of noise channels on the sound chip.</summary>
    public int SoundNoiseChannels { get; set; }
}