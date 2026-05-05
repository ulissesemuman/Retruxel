using System;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a single 8x8 tile that can be used in text rendering or tilesets.
/// Can originate from DefaultFont, external PNG, TTF rasterization, or raw bytes.
/// </summary>
public class TileEntry
{
    /// <summary>
    /// Unicode character this tile represents, if applicable.
    /// Null for tiles from spritesheets that have no character mapping.
    /// </summary>
    public char? Character { get; init; }

    /// <summary>
    /// Raw 8x8 glyph bitmap in font8x8 format:
    /// 8 bytes, row-wise, LSB = leftmost pixel.
    /// </summary>
    public byte[] Bitmap { get; init; } = new byte[8];

    /// <summary>
    /// Human-readable source description shown in tooltips.
    /// Examples: "font8x8 Basic", "MyFont.ttf", "icons.png tile 3"
    /// </summary>
    public string SourceLabel { get; init; } = "";

    /// <summary>
    /// Zero-based index within the source (glyph index, tile index, etc.)
    /// </summary>
    public int SourceIndex { get; init; }
}

/// <summary>
/// Event args for tile selection events.
/// </summary>
public class TileSelectedEventArgs : EventArgs
{
    public TileEntry Tile { get; }
    public int Index { get; }

    public TileSelectedEventArgs(TileEntry tile, int index)
    {
        Tile = tile;
        Index = index;
    }
}

/// <summary>
/// Event args for tile drag events.
/// </summary>
public class TileDragEventArgs : EventArgs
{
    public TileEntry Tile { get; }
    public int Index { get; }

    public TileDragEventArgs(TileEntry tile, int index)
    {
        Tile = tile;
        Index = index;
    }
}
