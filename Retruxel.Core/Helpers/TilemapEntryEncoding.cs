namespace Retruxel.Core.Helpers;

/// <summary>
/// Encodes tilemap entries with flip flags in a 32-bit integer.
/// 
/// Bit layout:
/// - Bits 31-16: Reserved for future use
/// - Bit 10: Vertical flip
/// - Bit 9: Horizontal flip
/// - Bits 8-0: Tile index (0-511)
/// </summary>
public static class TilemapEntryEncoding
{
    private const int FlipHBit = 9;
    private const int FlipVBit = 10;
    private const int TileIndexMask = 0x1FF;  // bits 0-8 (0-511)

    /// <summary>
    /// Encodes a tile index with flip flags into a single integer.
    /// </summary>
    public static int Encode(int tileIndex, bool flipH, bool flipV)
        => (tileIndex & TileIndexMask)
           | (flipH ? (1 << FlipHBit) : 0)
           | (flipV ? (1 << FlipVBit) : 0);

    /// <summary>
    /// Extracts the tile index from an encoded value.
    /// </summary>
    public static int DecodeTileIndex(int encoded) => encoded & TileIndexMask;

    /// <summary>
    /// Checks if horizontal flip is enabled in an encoded value.
    /// </summary>
    public static bool DecodeFlipH(int encoded) => (encoded & (1 << FlipHBit)) != 0;

    /// <summary>
    /// Checks if vertical flip is enabled in an encoded value.
    /// </summary>
    public static bool DecodeFlipV(int encoded) => (encoded & (1 << FlipVBit)) != 0;
}
