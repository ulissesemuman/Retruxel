namespace Retruxel.Core.Models.Wizard;

/// <summary>
/// Data collected from Target Wizard.
/// </summary>
public class TargetWizardData
{
    // Step 1: Basic Information
    public string TargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReleaseYear { get; set; } = 1980;
    public string Status { get; set; } = "Scaffolding"; // Active, Scaffolding, Planned

    // Step 2: Technical Specifications
    public string CPU { get; set; } = string.Empty;
    public long CpuClockHz { get; set; }
    public int ScreenWidth { get; set; } = 256;
    public int ScreenHeight { get; set; } = 192;
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;
    public int RamBytes { get; set; } = 8192;
    public int VramBytes { get; set; } = 16384;
    public int TotalColors { get; set; } = 64;
    public int ColorDepthBitsPerChannel { get; set; } = 2;
    public int ColorsPerPalette { get; set; } = 16;
    public int MaxSpritesOnScreen { get; set; } = 64;
    public int SpritesPerScanline { get; set; } = 8;
    public string SoundChip { get; set; } = string.Empty;
    public int SoundToneChannels { get; set; } = 3;
    public int SoundNoiseChannels { get; set; } = 1;

    // Step 3: Toolchain
    public string Compiler { get; set; } = "sdcc"; // sdcc, cc65, custom
    public string RomExtension { get; set; } = ".rom";

    // Step 4: Main file
    public string MainFileSource { get; set; } = "template"; // template, type, attach
    public string MainFileContent { get; set; } = string.Empty;

    // Step 5: Variable mappings
    public Dictionary<string, string> VariableMappings { get; set; } = new();

    // Output
    public string OutputPath { get; set; } = string.Empty;
}
