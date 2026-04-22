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
    /// VRAM regions available on this target, grouped by intended use.
    /// Used by the AssetImporter to categorize assets and assign startTile automatically.
    /// Used by the editor to validate tile budget per region.
    ///
    /// Ex SMS:  [ VramRegion("bg",     "Background", 0,   255) ]
    ///          [ VramRegion("sprite", "Sprites",    256, 447) ]
    ///
    /// Ex NES:  [ VramRegion("pattern0", "Pattern Table 0", 0,   255) ]
    ///          [ VramRegion("pattern1", "Pattern Table 1", 256, 511) ]
    ///
    /// Ex Mega Drive: [ VramRegion("bg",     "Background", 0,    1535) ]
    ///                [ VramRegion("sprite", "Sprites",    1536, 2047) ]
    /// </summary>
    public VramRegion[] VramRegions { get; set; } = [];

    /// <summary>
    /// Convenience: total VRAM tile capacity across all regions.
    /// Computed from VramRegions — do not set directly.
    /// </summary>
    public int MaxTilesInVram => VramRegions.Length > 0
        ? VramRegions.Sum(r => r.TileCount)
        : 0;

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

    // Tilemap

    /// <summary>
    /// Tilemap specifications for this target.
    /// Defines layer support, flip/rotate capabilities, palette modes, and size limits.
    /// </summary>
    public TilemapSpecs Tilemap { get; set; } = new();

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

    /// <summary>
    /// ROM banks available on this target.
    /// Single-bank targets (SMS, GG, SG-1000, ColecoVision) define one entry.
    /// Multi-bank targets (NES) define one entry per named bank (PRG-ROM, CHR-ROM).
    /// The Build Console iterates this array to display per-bank usage.
    /// </summary>
    public RomBank[] Banks { get; set; } = [];

    /// <summary>
    /// Total maximum ROM size in bytes across all banks.
    /// Computed from Banks — do not set directly.
    /// </summary>
    public int RomMaxBytes => Banks.Sum(b => b.MaxBytes);

    // CPU

    /// <summary>CPU name. Ex: "Zilog Z80", "MOS 6502", "Motorola 68000"</summary>
    public string CPU { get; set; } = string.Empty;

    /// <summary>CPU clock speed in Hz. Ex: 3546893 (SMS PAL)</summary>
    public int CpuClockHz { get; set; }

    // Manufacturer

    /// <summary>Console manufacturer. Ex: "Sega", "Nintendo", "Coleco"</summary>
    public string Manufacturer { get; set; } = string.Empty;

    // Sound

    /// <summary>Sound chip name. Ex: "SN76489", "2A03", "SPC700"</summary>
    public string SoundChip { get; set; } = string.Empty;

    /// <summary>Number of tone channels on the sound chip.</summary>
    public int SoundToneChannels { get; set; }

    /// <summary>Number of noise channels on the sound chip.</summary>
    public int SoundNoiseChannels { get; set; }
}

/// <summary>
/// Represents a named ROM bank on a target console.
/// Single-bank targets use one entry labelled "ROM".
/// Multi-bank targets (NES) define one entry per bank (PRG-ROM, CHR-ROM).
/// </summary>
/// <param name="Id">Internal identifier. Ex: "rom", "prg", "chr"</param>
/// <param name="Label">Display name shown in the Build Console. Ex: "ROM", "PRG-ROM", "CHR-ROM"</param>
/// <param name="MaxBytes">Maximum capacity of this bank in bytes.</param>
public record RomBank(string Id, string Label, int MaxBytes);

/// <summary>
/// A named region of VRAM tile slots on a target console.
/// Defines where a category of assets lives in VRAM.
/// </summary>
/// <param name="Id">Internal identifier. Ex: "bg", "sprite", "pattern0"</param>
/// <param name="Label">Display name shown in the AssetImporter. Ex: "Background", "Sprites"</param>
/// <param name="StartTile">First tile slot in this region (inclusive).</param>
/// <param name="EndTile">Last tile slot in this region (inclusive).</param>
public record VramRegion(string Id, string Label, int StartTile, int EndTile)
{
    public int TileCount => EndTile - StartTile + 1;
}

/// <summary>
/// Tilemap specifications for a target console.
/// Defines layer support, flip/rotate capabilities, palette assignment modes, and size limits.
/// Used by the Tilemap Editor to generate UI dynamically and validate user input.
/// </summary>
public class TilemapSpecs
{
    /// <summary>
    /// Maximum number of tilemap layers this target supports.
    /// Ex: 1 (SMS, NES), 2 (Game Gear), 4 (Mega Drive — Plane A, B, Window, Sprites)
    /// </summary>
    public int MaxLayers { get; set; } = 1;

    /// <summary>
    /// Whether tiles can be flipped horizontally via tile attributes.
    /// Ex: true (SMS, Mega Drive), false (NES — requires duplicate tiles)
    /// </summary>
    public bool SupportsHorizontalFlip { get; set; } = false;

    /// <summary>
    /// Whether tiles can be flipped vertically via tile attributes.
    /// Ex: true (SMS, Mega Drive), false (NES — requires duplicate tiles)
    /// </summary>
    public bool SupportsVerticalFlip { get; set; } = false;

    /// <summary>
    /// Whether tiles can be rotated 90° via tile attributes.
    /// Ex: true (GBA), false (SMS, NES, Mega Drive)
    /// </summary>
    public bool SupportsRotation { get; set; } = false;

    /// <summary>
    /// How palettes are assigned to tiles in a tilemap.
    /// Determines UI behavior in the Tilemap Editor.
    /// </summary>
    public PaletteMode PaletteMode { get; set; } = PaletteMode.PerTilemap;

    /// <summary>
    /// If PaletteMode == PerTile, how many bits are used to store the palette index per tile.
    /// Ex: 1 (SMS — 2 palettes: 0 or 1), 2 (Mega Drive — 4 palettes: 0-3)
    /// Ignored if PaletteMode != PerTile.
    /// </summary>
    public int PaletteBitsPerTile { get; set; } = 0;

    /// <summary>
    /// Default tilemap width in tiles when creating a new tilemap.
    /// Ex: 32 (SMS), 32 (NES), 64 (Mega Drive)
    /// </summary>
    public int DefaultWidth { get; set; } = 32;

    /// <summary>
    /// Default tilemap height in tiles when creating a new tilemap.
    /// Ex: 28 (SMS), 30 (NES), 32 (Mega Drive)
    /// </summary>
    public int DefaultHeight { get; set; } = 28;

    /// <summary>
    /// Maximum tilemap width in tiles.
    /// Ex: 64 (SMS), 64 (NES), 128 (Mega Drive)
    /// </summary>
    public int MaxWidth { get; set; } = 64;

    /// <summary>
    /// Maximum tilemap height in tiles.
    /// Ex: 64 (SMS), 64 (NES), 128 (Mega Drive)
    /// </summary>
    public int MaxHeight { get; set; } = 64;
}

/// <summary>
/// Defines how palettes are assigned to tiles in a tilemap.
/// Used by the Tilemap Editor to show/hide palette selection controls.
/// </summary>
public enum PaletteMode
{
    /// <summary>
    /// One palette for the entire tilemap. All tiles share the same palette.
    /// Ex: NES (each nametable uses one palette from the 4 available)
    /// UI: Shows single ComboBox "Palette" in Properties panel.
    /// </summary>
    PerTilemap,

    /// <summary>
    /// Each tile can have a different palette assigned via tile attributes.
    /// Ex: SMS (1 bit = 2 palettes), Mega Drive (2 bits = 4 palettes)
    /// UI: Shows ComboBox "Palette" in Properties panel + per-tile palette brush in toolbar.
    /// </summary>
    PerTile,

    /// <summary>
    /// Tiles are grouped into blocks (e.g., 2×2 or 4×4), and each block shares a palette.
    /// Ex: GBA (4×4 blocks share palette)
    /// UI: Shows block-level palette selection overlay on canvas.
    /// </summary>
    PerBlock
}