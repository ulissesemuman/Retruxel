namespace Retruxel.Core.Models;

public class ConsoleSpec
{
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string CPU { get; set; } = string.Empty;
    public int CpuClockHz { get; set; }
    public int RamBytes { get; set; }
    public int VramBytes { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public int TotalColors { get; set; }
    public int ColorDepthBitsPerChannel { get; set; }
    public int ColorsPerPalette { get; set; }
    public int MaxSpritesOnScreen { get; set; }
    public int SpritesPerScanline { get; set; }
    public string SoundChip { get; set; } = string.Empty;
    public int SoundToneChannels { get; set; }
    public int SoundNoiseChannels { get; set; }
    public string Compiler { get; set; } = string.Empty;
    public string RomExtension { get; set; } = string.Empty;
}
