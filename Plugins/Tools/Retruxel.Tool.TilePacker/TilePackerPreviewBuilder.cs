using Retruxel.Core.Models;
using System.Collections.Generic;

namespace Retruxel.Tool.TilePacker;

internal static class TilePackerPreviewBuilder
{
    /// <summary>
    /// Builds a single-column index map from unique tile pixel data (palette indices).
    /// </summary>
    public static byte[] BuildOptimizedMapIndex(
        IReadOnlyList<byte[]> uniqueTiles,
        int tileWidth,
        int tileHeight)
    {
        if (uniqueTiles.Count == 0) return [];

        int imageWidth  = tileWidth;
        int imageHeight = uniqueTiles.Count * tileHeight;
        var mapIndex    = new byte[imageWidth * imageHeight];

        for (int tileIdx = 0; tileIdx < uniqueTiles.Count; tileIdx++)
        {
            var tileData = uniqueTiles[tileIdx];
            int tileY    = tileIdx * tileHeight;

            for (int py = 0; py < tileHeight; py++)
            {
                for (int px = 0; px < tileWidth; px++)
                    mapIndex[(tileY + py) * imageWidth + px] = tileData[py * tileWidth + px];
            }
        }

        return mapIndex;
    }

    public static List<TileMappingRow> BuildMappingRows(
        TilePackResult result,
        int tilesPerRow)
    {
        var rows = new List<TileMappingRow>(result.Plane.Count);
        var firstSourceForUnique = new Dictionary<int, int>();

        foreach (var entry in result.Plane)
        {
            int sourceIndex = entry.Y * tilesPerRow + entry.X;

            if (!firstSourceForUnique.ContainsKey(entry.TileIndex))
                firstSourceForUnique[entry.TileIndex] = sourceIndex;

            bool hasTransform = entry.FlipH || entry.FlipV || entry.Rotation != 0;
            bool isRemapped   = firstSourceForUnique[entry.TileIndex] != sourceIndex || hasTransform;

            rows.Add(new TileMappingRow
            {
                TileX         = entry.X,
                TileY         = entry.Y,
                SourceIndex   = sourceIndex,
                MappedIndex   = entry.TileIndex,
                FlipH         = entry.FlipH,
                FlipV         = entry.FlipV,
                Rotation      = entry.Rotation,
                Transform     = FormatTransform(entry.FlipH, entry.FlipV, entry.Rotation),
                Status        = isRemapped ? "Remapped" : "Unique"
            });
        }

        return rows;
    }

    private static string FormatTransform(bool flipH, bool flipV, int rotation)
    {
        if (rotation != 0) return $"{rotation}°";
        if (flipH && flipV) return "FH+FV";
        if (flipH) return "FH";
        if (flipV) return "FV";
        return "—";
    }
}

public sealed class TileMappingRow
{
    public int TileX { get; init; }
    public int TileY { get; init; }
    public int SourceIndex { get; init; }
    public int MappedIndex { get; init; }
    public bool FlipH { get; init; }
    public bool FlipV { get; init; }
    public int Rotation { get; init; }
    public string Transform { get; init; } = "—";
    public string Status { get; init; } = "Unique";
    public string Position => $"({TileX}, {TileY})";
}
