namespace Retruxel.Tool.TilemapPreprocessor;

/// <summary>
/// Represents a single tile placement in the tilemap.
/// Stores tile identity and transformation intent — no hardware-specific encoding.
/// </summary>
public class TileEntry
{
    /// <summary>
    /// Index of the unique tile in the tileset (0-based, relative to startTile).
    /// -1 = empty cell.
    /// </summary>
    public int TileIndex { get; set; } = -1;

    /// <summary>Flip the tile horizontally.</summary>
    public bool FlipH { get; set; }

    /// <summary>Flip the tile vertically.</summary>
    public bool FlipV { get; set; }

    /// <summary>
    /// Clockwise rotation in degrees. Valid values: 0, 90, 180, 270.
    /// Only used when the target hardware supports rotation.
    /// </summary>
    public int Rotation { get; set; }

    public bool IsEmpty => TileIndex < 0;

    public static TileEntry Empty => new() { TileIndex = -1 };
}
