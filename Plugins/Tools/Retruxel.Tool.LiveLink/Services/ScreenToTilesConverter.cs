using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Tool.LiveLink.Services;

/// <summary>
/// Converts screen buffer (RGB pixels) to tiles + palette + nametable.
/// </summary>
public class ScreenToTilesConverter
{
    public class ConversionResult
    {
        public byte[][] Tiles { get; set; } = Array.Empty<byte[]>();
        public uint[] Palette { get; set; } = Array.Empty<uint>();
        public ushort[] Nametable { get; set; } = Array.Empty<ushort>();
        public int NametableWidth { get; set; }
        public int NametableHeight { get; set; }
        public byte[] TilePaletteAssignments { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Converts RGBA screen buffer to tiles (8×8), optimized palette, and nametable.
    /// Nametable is 1:1 (no deduplication) - use tile optimizer tool to reduce tile count.
    /// </summary>
    public static ConversionResult Convert(
        byte[] screenBuffer,
        int screenWidth,
        int screenHeight,
        Retruxel.Core.Interfaces.ITarget target,
        int tileWidth = 8,
        int tileHeight = 8)
    {
        // Extract tiles from screen buffer
        int tilesX = screenWidth / tileWidth;
        int tilesY = screenHeight / tileHeight;
        int totalTiles = tilesX * tilesY;

        var tiles = new List<byte[]>();
        var tileColors = new List<uint[]>();
        var nametable = new ushort[totalTiles];

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                var (tile, colors) = ExtractTile(screenBuffer, screenWidth, screenHeight, tx, ty, tileWidth, tileHeight);

                int tileIndex = tiles.Count;
                tiles.Add(tile);
                tileColors.Add(colors);
                nametable[ty * tilesX + tx] = (ushort)tileIndex;
            }
        }

        // Build color palette from all unique colors
        var allColors = tileColors.SelectMany(c => c).Distinct().ToArray();

        System.Diagnostics.Debug.WriteLine($"[ScreenToTilesConverter] Found {allColors.Length} unique colors in screen");

        // Get palette configuration from target
        int paletteSlotCount = target.GetPaletteSlotCount();
        int colorsPerSlot = target.GetColorsPerSlot();
        int totalSlots = paletteSlotCount * colorsPerSlot;

        System.Diagnostics.Debug.WriteLine($"[ScreenToTilesConverter] Target: {paletteSlotCount} slots × {colorsPerSlot} colors = {totalSlots} total");

        // Optimize palette using hierarchical clustering
        var clusters = HierarchicalClustering(allColors, totalSlots);

        // Build palette structure
        var palettes = new uint[paletteSlotCount][];
        for (int i = 0; i < paletteSlotCount; i++)
        {
            palettes[i] = new uint[colorsPerSlot];
            for (int j = 0; j < colorsPerSlot && i * colorsPerSlot + j < clusters.Length; j++)
            {
                palettes[i][j] = clusters[i * colorsPerSlot + j];
            }
        }

        var assignments = AssignTilesToPalettes(tiles.ToArray(), tileColors.ToArray(), palettes, colorsPerSlot);

        var optimized = new PaletteOptimizer.OptimizedPalette
        {
            Palettes = palettes,
            TilePaletteAssignments = assignments,
            TotalColors = Math.Min(clusters.Length, totalSlots)
        };

        // Remap tile pixels to palette indices
        var remappedTiles = RemapTilesToPalette(tiles.ToArray(), tileColors.ToArray(), optimized);

        return new ConversionResult
        {
            Tiles = remappedTiles,
            Palette = optimized.Palettes.SelectMany(p => p).ToArray(),
            Nametable = nametable,
            NametableWidth = tilesX,
            NametableHeight = tilesY,
            TilePaletteAssignments = optimized.TilePaletteAssignments
        };
    }

    private static (byte[] tile, uint[] colors) ExtractTile(
        byte[] screenBuffer,
        int screenWidth,
        int screenHeight,
        int tileX,
        int tileY,
        int tileWidth,
        int tileHeight)
    {
        var tile = new byte[tileWidth * tileHeight];
        var colorMap = new Dictionary<uint, byte>();
        var colorList = new List<uint>();

        // Debug: Log tiles around line 8 (ty=7)
        if (tileY >= 7 && tileY <= 8 && tileX == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ExtractTile] tileX={tileX}, tileY={tileY}, screenPos=({tileX * tileWidth}, {tileY * tileHeight})");
        }

        for (int py = 0; py < tileHeight; py++)
        {
            for (int px = 0; px < tileWidth; px++)
            {
                int screenX = tileX * tileWidth + px;
                int screenY = tileY * tileHeight + py;

                if (screenX >= screenWidth || screenY >= screenHeight)
                {
                    tile[py * tileWidth + px] = 0;
                    continue;
                }

                int bufferIdx = (screenY * screenWidth + screenX) * 4;

                // Debug: Log first pixel of tiles around line 8
                if (tileY >= 7 && tileY <= 8 && tileX == 0 && px == 0 && py == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExtractTile] Tile ({tileX},{tileY}): first pixel at screenX={screenX}, screenY={screenY}, bufferIdx={bufferIdx}");
                }

                if (bufferIdx + 3 >= screenBuffer.Length)
                {
                    tile[py * tileWidth + px] = 0;
                    continue;
                }

                uint color = (uint)(
                    (screenBuffer[bufferIdx + 3] << 24) | // A
                    (screenBuffer[bufferIdx + 0] << 16) | // R
                    (screenBuffer[bufferIdx + 1] << 8) |  // G
                    screenBuffer[bufferIdx + 2]);         // B

                if (!colorMap.ContainsKey(color))
                {
                    colorMap[color] = (byte)colorList.Count;
                    colorList.Add(color);
                }

                tile[py * tileWidth + px] = colorMap[color];
            }
        }

        return (tile, colorList.ToArray());
    }

    private static byte[][] RemapTilesToPalette(
        byte[][] tiles,
        uint[][] tileColors,
        PaletteOptimizer.OptimizedPalette optimized)
    {
        var remappedTiles = new byte[tiles.Length][];
        var flatPalette = optimized.Palettes.SelectMany(p => p).ToArray();

        for (int tileIdx = 0; tileIdx < tiles.Length; tileIdx++)
        {
            var tile = tiles[tileIdx];
            var colors = tileColors[tileIdx];
            var remapped = new byte[tile.Length];

            for (int i = 0; i < tile.Length; i++)
            {
                byte localColorIdx = tile[i];
                if (localColorIdx < colors.Length)
                {
                    uint color = colors[localColorIdx];
                    remapped[i] = FindNearestColorIndex(color, flatPalette);
                }
            }

            remappedTiles[tileIdx] = remapped;
        }

        return remappedTiles;
    }

    private static byte FindNearestColorIndex(uint color, uint[] palette)
    {
        int bestIdx = 0;
        double minDist = double.MaxValue;

        for (int i = 0; i < palette.Length; i++)
        {
            double dist = ColorDistance(color, palette[i]);
            if (dist < minDist)
            {
                minDist = dist;
                bestIdx = i;
            }
        }

        return (byte)bestIdx;
    }

    private static double ColorDistance(uint c1, uint c2)
    {
        int r1 = (int)((c1 >> 16) & 0xFF);
        int g1 = (int)((c1 >> 8) & 0xFF);
        int b1 = (int)(c1 & 0xFF);

        int r2 = (int)((c2 >> 16) & 0xFF);
        int g2 = (int)((c2 >> 8) & 0xFF);
        int b2 = (int)(c2 & 0xFF);

        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;

        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static uint[] HierarchicalClustering(uint[] colors, int targetSlots)
    {
        if (colors.Length <= targetSlots)
            return colors;

        var colorSet = new HashSet<uint>(colors);
        var colorList = colorSet.ToList();
        var centroids = new uint[targetSlots];

        // K-means++ initialization
        var random = new Random();
        centroids[0] = colorList[random.Next(colorList.Count)];

        for (int i = 1; i < targetSlots; i++)
        {
            var distances = new double[colorList.Count];
            double totalDistance = 0;

            for (int j = 0; j < colorList.Count; j++)
            {
                double minDist = double.MaxValue;
                for (int k = 0; k < i; k++)
                {
                    double dist = ColorDistance(colorList[j], centroids[k]);
                    if (dist < minDist)
                        minDist = dist;
                }
                distances[j] = minDist * minDist;
                totalDistance += distances[j];
            }

            double threshold = random.NextDouble() * totalDistance;
            double sum = 0;

            for (int j = 0; j < colorList.Count; j++)
            {
                sum += distances[j];
                if (sum >= threshold)
                {
                    centroids[i] = colorList[j];
                    break;
                }
            }
        }

        // K-means clustering
        for (int iter = 0; iter < 20; iter++)
        {
            var clusters = new List<uint>[targetSlots];
            for (int i = 0; i < targetSlots; i++)
                clusters[i] = new List<uint>();

            foreach (var color in colorList)
            {
                int nearest = 0;
                double minDist = ColorDistance(color, centroids[0]);

                for (int i = 1; i < targetSlots; i++)
                {
                    double dist = ColorDistance(color, centroids[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = i;
                    }
                }

                clusters[nearest].Add(color);
            }

            bool changed = false;
            for (int i = 0; i < targetSlots; i++)
            {
                if (clusters[i].Count > 0)
                {
                    long r = 0, g = 0, b = 0;
                    foreach (var color in clusters[i])
                    {
                        r += (color >> 16) & 0xFF;
                        g += (color >> 8) & 0xFF;
                        b += color & 0xFF;
                    }

                    int count = clusters[i].Count;
                    uint newCentroid = 0xFF000000u | ((uint)(r / count) << 16) | ((uint)(g / count) << 8) | (uint)(b / count);

                    if (newCentroid != centroids[i])
                    {
                        centroids[i] = newCentroid;
                        changed = true;
                    }
                }
            }

            if (!changed)
                break;
        }

        return centroids;
    }

    private static byte[] AssignTilesToPalettes(byte[][] tiles, uint[][] tileColors, uint[][] palettes, int colorsPerPalette)
    {
        var assignments = new byte[tiles.Length];

        for (int tileIdx = 0; tileIdx < tiles.Length; tileIdx++)
        {
            var tile = tiles[tileIdx];
            var colors = tileColors[tileIdx].Distinct().ToArray();

            int bestPalette = 0;
            double minError = double.MaxValue;

            for (int palIdx = 0; palIdx < palettes.Length; palIdx++)
            {
                double error = 0;
                foreach (var color in colors)
                {
                    double minDist = double.MaxValue;
                    for (int i = 0; i < colorsPerPalette && i < palettes[palIdx].Length; i++)
                    {
                        double dist = ColorDistance(color, palettes[palIdx][i]);
                        if (dist < minDist)
                            minDist = dist;
                    }
                    error += minDist;
                }

                if (error < minError)
                {
                    minError = error;
                    bestPalette = palIdx;
                }
            }

            assignments[tileIdx] = (byte)bestPalette;
        }

        return assignments;
    }
}
