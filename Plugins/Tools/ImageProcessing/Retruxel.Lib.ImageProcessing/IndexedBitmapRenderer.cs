using Retruxel.Core.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Renders an indexed bitmap from a mapIndex byte array and a hardware palette.
///
/// mapIndex stores one byte per pixel — the index into the hardware palette.
/// Index 0 is typically the transparent/background color.
/// Valid range depends on the target (SMS: 0–15, NES: 0–3 per sub-palette, etc.).
///
/// This class is target-agnostic: it only maps indices to colors.
/// The caller is responsible for providing the correct palette for the target.
/// </summary>
public static class IndexedBitmapRenderer
{
    /// <summary>
    /// Renders an SKBitmap from a mapIndex array and palette.
    /// Uses Parallel.For for performance on large images.
    /// </summary>
    /// <param name="mapIndex">One byte per pixel — palette index. Length must equal width × height.</param>
    /// <param name="palette">Hardware palette. Index values in mapIndex must be valid indices into this list.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>SKBitmap in N32 format, ready for display.</returns>
    public static SKBitmap Render(
        byte[] mapIndex,
        IReadOnlyList<HardwareColor> palette,
        int width,
        int height)
    {
        if (mapIndex.Length != width * height)
            throw new ArgumentException(
                $"mapIndex length {mapIndex.Length} does not match {width}×{height} = {width * height}.",
                nameof(mapIndex));

        if (palette.Count == 0)
            throw new ArgumentException("Palette must contain at least one color.", nameof(palette));

        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // Pre-clamp palette count once — avoids per-pixel branch
        int paletteMax = palette.Count - 1;

        unsafe
        {
            var ptr = (uint*)bitmap.GetPixels().ToPointer();

            Parallel.For(0, height, y =>
            {
                int rowStart = y * width;

                for (int x = 0; x < width; x++)
                {
                    int idx = Math.Min(mapIndex[rowStart + x], paletteMax);
                    var color = palette[idx];

                    // RGBA format
                    ptr[rowStart + x] = (uint)((255 << 24) | (color.R << 16) | (color.G << 8) | color.B);
                }
            });
        }

        return bitmap;
    }

    /// <summary>
    /// Renders a single tile from a mapIndex array.
    /// Extracts the tile region and renders it as a standalone bitmap.
    /// </summary>
    /// <param name="mapIndex">Full image mapIndex (one byte per pixel).</param>
    /// <param name="palette">Hardware palette.</param>
    /// <param name="imageWidthInPixels">Full image width — needed to compute row offsets.</param>
    /// <param name="tileX">Tile column in the tileset grid.</param>
    /// <param name="tileY">Tile row in the tileset grid.</param>
    /// <param name="tileSize">Tile size in pixels (width and height, assumed square).</param>
    /// <returns>SKBitmap for the requested tile.</returns>
    public static SKBitmap RenderTile(
        byte[] mapIndex,
        IReadOnlyList<HardwareColor> palette,
        int imageWidthInPixels,
        int tileX,
        int tileY,
        int tileSize)
    {
        int paletteMax = palette.Count - 1;
        var bitmap = new SKBitmap(tileSize, tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        int srcOriginX = tileX * tileSize;
        int srcOriginY = tileY * tileSize;

        unsafe
        {
            var ptr = (uint*)bitmap.GetPixels().ToPointer();

            for (int py = 0; py < tileSize; py++)
            {
                int srcRow = (srcOriginY + py) * imageWidthInPixels + srcOriginX;
                int dstRow = py * tileSize;

                for (int px = 0; px < tileSize; px++)
                {
                    int srcIdx = srcRow + px;
                    if (srcIdx >= mapIndex.Length)
                    {
                        ptr[dstRow + px] = 0; // Transparent
                        continue;
                    }

                    int idx = Math.Min(mapIndex[srcIdx], paletteMax);
                    var color = palette[idx];

                    // RGBA format
                    ptr[dstRow + px] = (uint)((255 << 24) | (color.B << 16) | (color.G << 8) | color.R);
                }
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Encodes a bitmap into a mapIndex array using the specified palette and distance mode.
    /// </summary>
    /// <param name="bitmap">Source bitmap to encode.</param>
    /// <param name="palette">Palette to use for encoding.</param>
    /// <param name="distanceMode">Distance mode for color matching.</param>
    /// <returns>Encoded mapIndex array.</returns>
    public static byte[] Encode(
        SKBitmap bitmap,
        IReadOnlyList<HardwareColor> palette,
        DistanceMode distanceMode = DistanceMode.RGB
        )
    {
        var converted = bitmap.ColorType == SKColorType.Bgra8888
            ? bitmap
            : bitmap.Copy(SKColorType.Bgra8888);

        int width = converted.Width;
        int height = converted.Height;
        int rowBytes = converted.RowBytes;
        int paletteMax = palette.Count - 1;
        var result = new byte[width * height];

        var fastPalette = ColorMatching.PrepareFastPalette(palette, DistanceMode.RGB);

        unsafe
        {
            var ptr = (byte*)converted.GetPixels().ToPointer();

            Parallel.For(0, height, y =>
            {
                byte* row = ptr + y * rowBytes;
                int rowStart = y * width;

                for (int x = 0; x < width; x++)
                {
                    byte* px = row + x * 4;
                    byte alpha = px[3];

                    // Bgra8888: px[0]=B, px[1]=G, px[2]=R
                    result[rowStart + x] = alpha == 0
                        ? (byte)0
                        : ColorMatching.FindNearestColorIndex((px[2], px[1], px[0]), fastPalette, distanceMode);
                }
            });
        }

        if (!ReferenceEquals(converted, bitmap))
            converted.Dispose();

        return result;
    }

    /// <summary>
    /// Encodes a mapIndex array by remapping indices from one palette to another.
    /// Useful for changing palettes without re-processing the original image.
    /// </summary>
    /// <param name="sourceMapIndex">Original mapIndex with indices into sourcePalette.</param>
    /// <param name="sourcePalette">Original palette that sourceMapIndex refers to.</param>
    /// <param name="targetPalette">New palette to remap colors to.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>New mapIndex with indices into targetPalette.</returns>
    public static byte[] EncodeFromMapIndex(
        byte[] sourceMapIndex,
        IReadOnlyList<HardwareColor> sourcePalette,
        IReadOnlyList<HardwareColor> targetPalette,
        int width,
        int height)
    {
        if (sourceMapIndex.Length != width * height)
            throw new ArgumentException(
                $"sourceMapIndex length {sourceMapIndex.Length} does not match {width}×{height}.",
                nameof(sourceMapIndex));

        var result = new byte[sourceMapIndex.Length];
        var fastPalette = ColorMatching.PrepareFastPalette(targetPalette, DistanceMode.RGB);
        int sourcePaletteMax = sourcePalette.Count - 1;

        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                int idx = rowStart + x;
                int sourceColorIdx = Math.Min(sourceMapIndex[idx], sourcePaletteMax);
                var sourceColor = sourcePalette[sourceColorIdx];

                // Find nearest color in target palette
                result[idx] = ColorMatching.FindNearestColorIndex(
                    (sourceColor.R, sourceColor.G, sourceColor.B),
                    fastPalette,
                    DistanceMode.RGB);
            }
        });

        return result;
    }

    public static List<(byte R, byte G, byte B)> ExtractPixels(SKBitmap bitmap)
    {
        // Garante formato conhecido — Bgra8888 para consistência com o resto do pipeline
        var converted = bitmap.ColorType == SKColorType.Bgra8888
            ? bitmap
            : bitmap.Copy(SKColorType.Bgra8888);

        int width = converted.Width;
        int height = converted.Height;

        var result = new List<(byte R, byte G, byte B)>(width * height);

        unsafe
        {
            var ptr = (byte*)converted.GetPixels().ToPointer();
            int rowBytes = converted.RowBytes;

            for (int y = 0; y < height; y++)
            {
                byte* row = ptr + y * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    byte* px = row + x * 4;
                    // Bgra8888: px[0]=B, px[1]=G, px[2]=R, px[3]=A
                    result.Add((px[2], px[1], px[0]));
                }
            }
        }

        if (!ReferenceEquals(converted, bitmap))
            converted.Dispose();

        return result;
    }
}
