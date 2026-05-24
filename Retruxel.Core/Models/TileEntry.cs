using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a single tile placement in a plane.
/// Stores tile identity and transformation intent — no hardware-specific encoding.
/// Target-agnostic: used by TilePacker, TilemapEditor, and code generation pipeline.
/// </summary>
public class TileEntry
{
    /// <summary>
    /// Index of the unique tile in the tileset (0-based, relative to startTile).
    /// -1 = empty cell.
    /// </summary>
    [JsonPropertyName("tileIndex")]
    public int TileIndex { get; set; } = -1;

    /// <summary>Flip the tile horizontally.</summary>
    [JsonPropertyName("flipH")]
    public bool FlipH { get; set; }

    /// <summary>Flip the tile vertically.</summary>
    [JsonPropertyName("flipV")]
    public bool FlipV { get; set; }

    /// <summary>
    /// Clockwise rotation in degrees. Valid values: 0, 90, 180, 270.
    /// Only used when the target hardware supports rotation.
    /// </summary>
    [JsonPropertyName("rotation")]
    public int Rotation { get; set; }

    /// <summary>X position in the original image (used by TilePacker).</summary>
    public int X { get; set; }

    /// <summary>Y position in the original image (used by TilePacker).</summary>
    public int Y { get; set; }

    /// <summary>
    /// Palette slot assigned to this tile (0-based).
    /// Relevant for targets that support per-tile palette selection (SMS BG, GBC, etc.).
    /// Defaults to 0.
    /// Overrides the plane's default palette slot for this tile only.
    /// </summary>
    [JsonPropertyName("paletteSlot")]
    public int PaletteSlot { get; set; } = -1;

    [JsonIgnore]
    public bool IsEmpty => TileIndex < 0;

    /// <summary>
    /// TileEntry representing an empty cell (transparent, no tile placed).
    /// </summary>
    public static TileEntry Empty => new() { TileIndex = -1 };

    public TileEntry Clone() => new()
    {
        TileIndex = TileIndex,
        FlipH = FlipH,
        FlipV = FlipV,
        Rotation = Rotation,
        X = X,
        Y = Y,
        PaletteSlot = PaletteSlot
    };
}
