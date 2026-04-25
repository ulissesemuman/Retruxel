namespace Retruxel.Core.Models;

/// <summary>
/// Standardized format for asset data exchange between tools (LiveLink, Import Assets, Tilemap Editor, etc).
/// Acts as an intermediate format that can be converted to/from different console formats.
/// </summary>
public class ImportedAssetData
{
    /// <summary>
    /// Decoded tile data. Each tile is a byte array of pixel indices (0-15 for 4bpp, 0-3 for 2bpp, etc).
    /// Format: tiles[tileIndex][pixelIndex] where pixelIndex = y * TileWidth + x
    /// </summary>
    public byte[][] Tiles { get; set; } = Array.Empty<byte[]>();

    /// <summary>
    /// Tilemap/nametable data. Array of tile indices arranged in row-major order.
    /// Format: tilemapData[y * MapWidth + x] = tileIndex
    /// Value -1 indicates empty/transparent tile.
    /// </summary>
    public int[] TilemapData { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Palette data in ARGB format (0xAARRGGBB).
    /// </summary>
    public uint[] Palette { get; set; } = Array.Empty<uint>();

    /// <summary>
    /// Width of each tile in pixels (typically 8).
    /// </summary>
    public int TileWidth { get; set; } = 8;

    /// <summary>
    /// Height of each tile in pixels (typically 8).
    /// </summary>
    public int TileHeight { get; set; } = 8;

    /// <summary>
    /// Width of the tilemap in tiles.
    /// </summary>
    public int MapWidth { get; set; }

    /// <summary>
    /// Height of the tilemap in tiles.
    /// </summary>
    public int MapHeight { get; set; }

    /// <summary>
    /// Bits per pixel (2 for NES/GB, 4 for SMS/GG, 4-8 for SNES/GBA).
    /// </summary>
    public int BitsPerPixel { get; set; } = 4;

    /// <summary>
    /// Source emulator that captured this data (e.g., "emulicious", "mesen").
    /// </summary>
    public string SourceEmulator { get; set; } = string.Empty;

    /// <summary>
    /// Source target/console ID (e.g., "sms", "nes", "gg").
    /// </summary>
    public string SourceTargetId { get; set; } = string.Empty;

    /// <summary>
    /// Destination target ID for conversion (if different from source).
    /// </summary>
    public string? DestinationTargetId { get; set; }

    /// <summary>
    /// Timestamp when the data was captured/imported.
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Optional metadata for additional information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Validates that the asset data is consistent and complete.
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        if (Tiles.Length == 0)
        {
            errorMessage = "No tiles data";
            return false;
        }

        if (TileWidth <= 0 || TileHeight <= 0)
        {
            errorMessage = "Invalid tile dimensions";
            return false;
        }

        if (TilemapData.Length > 0 && (MapWidth <= 0 || MapHeight <= 0))
        {
            errorMessage = "Invalid map dimensions";
            return false;
        }

        if (TilemapData.Length > 0 && TilemapData.Length != MapWidth * MapHeight)
        {
            errorMessage = $"Tilemap data size mismatch: expected {MapWidth * MapHeight}, got {TilemapData.Length}";
            return false;
        }

        var expectedPixelsPerTile = TileWidth * TileHeight;
        foreach (var tile in Tiles)
        {
            if (tile.Length != expectedPixelsPerTile)
            {
                errorMessage = $"Tile pixel count mismatch: expected {expectedPixelsPerTile}, got {tile.Length}";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Gets a summary string for debugging/logging.
    /// </summary>
    public string GetSummary()
    {
        return $"ImportedAssetData: {Tiles.Length} tiles ({TileWidth}x{TileHeight}), " +
               $"Map: {MapWidth}x{MapHeight}, Palette: {Palette.Length} colors, " +
               $"BPP: {BitsPerPixel}, Source: {SourceTargetId}";
    }
}
