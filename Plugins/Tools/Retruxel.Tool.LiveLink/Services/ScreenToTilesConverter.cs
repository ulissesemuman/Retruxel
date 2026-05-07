using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.AssetProcessor;
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

    private class OptimizedPalette
    {
        public uint[][] Palettes { get; set; } = Array.Empty<uint[]>();
        public byte[] TilePaletteAssignments { get; set; } = Array.Empty<byte>();
        public int TotalColors { get; set; }
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

        // Optimize palette using AssetProcessorTool
        var clusters = AssetProcessorTool.OptimizePalette(
            allColors.Select(c => (
                R: (byte)((c >> 16) & 0xFF),
                G: (byte)((c >> 8) & 0xFF),
                B: (byte)(c & 0xFF)
            )).ToList(),
            totalSlots,
            1.25 // Default diversity
        ).Select(c => 0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

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

        var optimized = new OptimizedPalette
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
        OptimizedPalette optimized)
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
            byte r1 = (byte)((color >> 16) & 0xFF);
            byte g1 = (byte)((color >> 8) & 0xFF);
            byte b1 = (byte)(color & 0xFF);
            byte r2 = (byte)((palette[i] >> 16) & 0xFF);
            byte g2 = (byte)((palette[i] >> 8) & 0xFF);
            byte b2 = (byte)(palette[i] & 0xFF);
            double dist = ColorMatching.ColorDistance(r1, g1, b1, r2, g2, b2);
            if (dist < minDist)
            {
                minDist = dist;
                bestIdx = i;
            }
        }

        return (byte)bestIdx;
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
                        byte r1 = (byte)((color >> 16) & 0xFF);
                        byte g1 = (byte)((color >> 8) & 0xFF);
                        byte b1 = (byte)(color & 0xFF);
                        byte r2 = (byte)((palettes[palIdx][i] >> 16) & 0xFF);
                        byte g2 = (byte)((palettes[palIdx][i] >> 8) & 0xFF);
                        byte b2 = (byte)((palettes[palIdx][i] & 0xFF));
                        double dist = ColorMatching.ColorDistance(r1, g1, b1, r2, g2, b2);
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
