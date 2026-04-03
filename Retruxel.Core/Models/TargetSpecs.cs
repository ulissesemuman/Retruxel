namespace Retruxel.Core.Models;

/// <summary>
/// Hardware specifications of a target console.
/// Used by the shell to display system info and enforce hardware limits.
/// </summary>
public class TargetSpecs
{
    /// <summary>Screen resolution in pixels. Ex: 256x192 (SMS), 256x240 (NES)</summary>
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }

    /// <summary>Total colors available in the hardware palette.</summary>
    public int TotalColors { get; set; }

    /// <summary>Maximum colors per tile.</summary>
    public int ColorsPerTile { get; set; }

    /// <summary>Maximum sprites rendered per scanline before flickering.</summary>
    public int SpritesPerScanline { get; set; }

    /// <summary>Maximum sprites on screen simultaneously.</summary>
    public int MaxSpritesOnScreen { get; set; }

    /// <summary>Tile size in pixels. Almost universally 8x8 on third-gen consoles.</summary>
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;

    /// <summary>Available RAM in bytes. Ex: 8192 (SMS), 2048 (NES)</summary>
    public int RamBytes { get; set; }

    /// <summary>Maximum ROM size in bytes.</summary>
    public int RomMaxBytes { get; set; }

    /// <summary>CPU name. Ex: "Zilog Z80", "MOS 6502", "Motorola 68000"</summary>
    public string CPU { get; set; } = string.Empty;

    /// <summary>CPU clock speed in Hz. Ex: 3546893 (SMS PAL)</summary>
    public int CpuClockHz { get; set; }

    /// <summary>Sound chip name. Ex: "SN76489", "2A03", "SPC700"</summary>
    public string SoundChip { get; set; } = string.Empty;

    /// <summary>Number of tone channels on the sound chip.</summary>
    public int SoundToneChannels { get; set; }

    /// <summary>Number of noise channels on the sound chip.</summary>
    public int SoundNoiseChannels { get; set; }
}