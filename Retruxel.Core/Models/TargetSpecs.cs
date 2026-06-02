using System;
using System.Linq;

namespace Retruxel.Core.Models;

/// <summary>
/// Hardware specifications of a target console.
///
/// Divided into two concerns:
///   - Build specs: used by the toolchain and code generators
///   - Editor specs: used by the visual editor, tools and UI
/// </summary>
public class TargetSpecs
{
    // ── Screen ────────────────────────────────────────────────────────────────

    /// <summary>Screen resolution in pixels. Ex: 256×192 (SMS), 256×240 (NES)</summary>
    public int ScreenWidth  { get; set; }
    public int ScreenHeight { get; set; }

    // ── Tiles ─────────────────────────────────────────────────────────────────

    /// <summary>Tile size in pixels. Almost universally 8×8 on third-gen consoles.</summary>
    public int TileWidth  { get; set; } = 8;
    public int TileHeight { get; set; } = 8;

    // ── Colors & Palettes ─────────────────────────────────────────────────────

    /// <summary>
    /// Total distinct colors the hardware can produce.
    /// Ex: 64 (SMS), 54 (NES), 32768 (SNES)
    /// </summary>
    public int TotalColors { get; set; }

    /// <summary>
    /// Bits per color channel used by the hardware DAC.
    /// Ex: 2 (SMS), 3 (NES approximation), 5 (SNES)
    /// Actual colors come from ITarget.GetHardwarePalette().
    /// </summary>
    public int ColorDepthBitsPerChannel { get; set; }

    /// <summary>Maximum colors per tile (from a single palette slot). Ex: 16 (SMS), 4 (NES)</summary>
    public int ColorsPerTile { get; set; }

    /// <summary>Maximum colors visible per palette slot. Ex: 16 (SMS), 4 (NES BG)</summary>
    public int ColorsPerPalette { get; set; }

    /// <summary>
    /// Total palette slots available simultaneously on screen.
    /// Ex: 2 (SMS), 8 (NES — 4 BG + 4 sprite)
    /// </summary>
    public int SimultaneousPalettes { get; set; }

    /// <summary>How many simultaneous palettes are available for background tiles.</summary>
    public int BgPalettes { get; set; }

    /// <summary>How many simultaneous palettes are available for sprites.</summary>
    public int SpritePalettes { get; set; }

    // ── Planes ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hardware BG planes available on this target.
    /// Does NOT include the sprite layer — sprites are implicit on all targets
    /// and are controlled via MaxSpritesOnScreen / SpritesPerScanline.
    ///
    /// Ex: 1 entry (SMS/NES), 3 entries (Mega Drive: Plane A, Plane B, Window),
    ///     4 entries (SNES Mode 0: BG1–BG4)
    /// </summary>
    public PlaneSpecs[] Planes { get; set; } = Array.Empty<PlaneSpecs>();

    // ── VRAM ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Total bytes of VRAM available for tile pattern data.
    /// Used by VramAllocator to validate that all assets fit before building.
    ///
    /// This is the usable tile area after subtracting Name Table(s), SAT, and
    /// other fixed VRAM structures — not the raw VRAM size.
    ///
    /// Ex: 14336 (SMS: 448 tiles × 32 bytes),
    ///     8192  (NES CHR-RAM: 512 tiles × 16 bytes),
    ///     131072 (SNES: 128 KB shared VRAM)
    /// </summary>
    public int VramBytesForTiles { get; set; }

    // ── Sprites ───────────────────────────────────────────────────────────────

    /// <summary>Maximum sprites rendered per scanline before flickering.</summary>
    public int SpritesPerScanline { get; set; }

    /// <summary>Maximum sprites on screen simultaneously.</summary>
    public int MaxSpritesOnScreen { get; set; }

    /// <summary>Base sprite size in pixels. Ex: 8×8 (SMS default), 8×8 (NES)</summary>
    public int SpriteWidth  { get; set; } = 8;
    public int SpriteHeight { get; set; } = 8;

    /// <summary>
    /// Whether the hardware supports a double-height sprite mode.
    /// Ex: true (SMS supports 8×16 via VDP register), false (NES handles via OAM layout)
    /// </summary>
    public bool SupportsDoubleHeightSprites { get; set; }

    // ── Memory ────────────────────────────────────────────────────────────────

    /// <summary>Available RAM in bytes. Ex: 8192 (SMS), 2048 (NES)</summary>
    public int RamBytes { get; set; }

    /// <summary>
    /// ROM banks available on this target.
    /// Single-bank targets define one entry; multi-bank targets define one entry per bank.
    /// </summary>
    public RomBank[] Banks { get; set; } = Array.Empty<RomBank>();

    /// <summary>
    /// Total maximum ROM size in bytes across all banks.
    /// Computed from Banks — do not set directly.
    /// </summary>
    public int RomMaxBytes => Banks.Sum(b => b.MaxBytes);

    // ── CPU ───────────────────────────────────────────────────────────────────

    /// <summary>CPU name. Ex: "Zilog Z80", "MOS 6502", "Motorola 68000"</summary>
    public string CPU { get; set; } = string.Empty;

    /// <summary>CPU clock speed in Hz. Ex: 3546893 (SMS PAL)</summary>
    public int CpuClockHz { get; set; }

    // ── Manufacturer ──────────────────────────────────────────────────────────

    /// <summary>Console manufacturer. Ex: "Sega", "Nintendo", "Coleco"</summary>
    public string Manufacturer { get; set; } = string.Empty;

    // ── Sound ─────────────────────────────────────────────────────────────────

    /// <summary>Sound chip name. Ex: "SN76489", "2A03", "SPC700"</summary>
    public string SoundChip { get; set; } = string.Empty;

    /// <summary>Number of tone channels on the sound chip.</summary>
    public int SoundToneChannels { get; set; }

    /// <summary>Number of noise channels on the sound chip.</summary>
    public int SoundNoiseChannels { get; set; }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Physical input ports available on this target hardware.
    /// Each port describes its type (controller, keyboard, light gun) and
    /// the buttons it exposes, including the DevkitConst used by CodeGen.
    ///
    /// Ex: SMS has two controller ports (port1/port2), each with D-pad + 2 buttons.
    /// </summary>
    public InputPort[] InputPorts { get; set; } = Array.Empty<InputPort>();
}

/// <summary>
/// Represents a named ROM bank on a target console.
/// </summary>
/// <param name="Id">Internal identifier. Ex: "rom", "prg", "chr"</param>
/// <param name="Label">Display name shown in the Build Console. Ex: "ROM", "PRG-ROM"</param>
/// <param name="MaxBytes">Maximum capacity of this bank in bytes.</param>
public record RomBank(string Id, string Label, int MaxBytes);

/// <summary>
/// Specifications for one hardware BG plane on a target console.
///
/// Each plane describes its own editing capabilities independently —
/// this allows targets like the SNES where BG planes have different
/// color depths (e.g. BG1=4bpp, BG3=2bpp in Mode 1).
/// </summary>
public class PlaneSpecs
{
    /// <summary>
    /// Internal identifier referenced by PlaneData.PlaneId.
    /// Ex: "bg" (SMS), "plane_a", "plane_b", "window" (Mega Drive)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the editor tree.
    /// Ex: "Background" (SMS), "Plane A", "Window" (Mega Drive)
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Whether tiles can be flipped horizontally via tile attributes.
    /// Ex: true (SMS, Mega Drive), false (NES — requires duplicate tiles)
    /// </summary>
    public bool SupportsHorizontalFlip { get; set; }

    /// <summary>
    /// Whether tiles can be flipped vertically via tile attributes.
    /// </summary>
    public bool SupportsVerticalFlip { get; set; }

    /// <summary>
    /// Whether tiles can be rotated 90° via tile attributes.
    /// Ex: true (GBA), false (SMS, NES, Mega Drive)
    /// </summary>
    public bool SupportsRotation { get; set; }

    /// <summary>
    /// How palettes are assigned to tiles in this plane.
    /// </summary>
    public PaletteMode PaletteMode { get; set; } = PaletteMode.PerPlane;

    /// <summary>
    /// Bits used to encode the color index per pixel in this plane.
    /// Determines tile byte size: bytesPerTile = (8 × 8 × BitsPerPixel) / 8
    ///
    /// Ex: 4 (SMS 16-color), 2 (NES/SNES BG3 4-color), 8 (SNES 256-color)
    ///
    /// Used by VramAllocator to calculate byte cost per tile for this plane.
    /// </summary>
    public int BitsPerPixel { get; set; } = 4;

    /// <summary>
    /// If PaletteMode == PerTile, how many bits encode the palette index per tile.
    /// Ex: 1 (SMS — 2 palettes: 0 or 1), 2 (Mega Drive — 4 palettes: 0–3)
    /// </summary>
    public int PaletteBitsPerTile { get; set; }

    /// <summary>Default plane width in tiles when creating a new layer.</summary>
    public int DefaultWidth { get; set; } = 32;

    /// <summary>Default plane height in tiles when creating a new layer.</summary>
    public int DefaultHeight { get; set; } = 28;

    /// <summary>Maximum plane width in tiles.</summary>
    public int MaxWidth { get; set; } = 64;

    /// <summary>Maximum plane height in tiles.</summary>
    public int MaxHeight { get; set; } = 64;

    /// <summary>
    /// Bytes consumed by one 8×8 tile in this plane.
    /// Derived from BitsPerPixel — not set directly.
    /// </summary>
    public int BytesPerTile => (TileWidth * TileHeight * BitsPerPixel) / 8;

    private const int TileWidth  = 8;
    private const int TileHeight = 8;
}

/// <summary>
/// Defines how palettes are assigned to tiles in a plane.
/// </summary>
public enum PaletteMode
{
    /// <summary>One palette for the entire plane.</summary>
    PerPlane,

    /// <summary>Each tile can have a different palette via tile attributes.</summary>
    PerTile,

    /// <summary>Tiles are grouped into blocks that share a palette.</summary>
    PerBlock
}
