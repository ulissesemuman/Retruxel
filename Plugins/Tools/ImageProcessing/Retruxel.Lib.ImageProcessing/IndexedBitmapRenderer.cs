using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Renders an indexed bitmap from a MapIndex byte array and a hardware palette.
///
/// MapIndex stores one byte per pixel — the index into the hardware palette.
/// Index 0 is typically the transparent/background color.
/// Valid range depends on the target (SMS: 0–15, NES: 0–3 per sub-palette, etc.).
///
/// This class is target-agnostic: it only maps indices to colors.
/// The caller is responsible for providing the correct palette for the target.
/// </summary>
public static class IndexedBitmapRenderer
{
    /// <summary>
    /// Renders a WriteableBitmap from a MapIndex array and palette.
    /// Uses Parallel.For for performance on large images.
    /// </summary>
    /// <param name="mapIndex">One byte per pixel — palette index. Length must equal width × height.</param>
    /// <param name="palette">Hardware palette. Index values in mapIndex must be valid indices into this list.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="existingBitmap">
    ///   Optional existing WriteableBitmap to write into (avoids allocation).
    ///   Must match width × height. If null, a new bitmap is allocated.
    /// </param>
    /// <returns>WriteableBitmap in Bgra32 format, ready for WPF display.</returns>
    public static WriteableBitmap Render(
        byte[] mapIndex,
        IReadOnlyList<HardwareColor> palette,
        int width,
        int height,
        WriteableBitmap? existingBitmap = null)
    {
        if (mapIndex.Length != width * height)
            throw new ArgumentException(
                $"MapIndex length {mapIndex.Length} does not match {width}×{height} = {width * height}.",
                nameof(mapIndex));

        if (palette.Count == 0)
            throw new ArgumentException("Palette must contain at least one color.", nameof(palette));

        int stride = width * 4;
        byte[] outputPixels = new byte[height * stride];

        // Pre-clamp palette count once — avoids per-pixel branch
        int paletteMax = palette.Count - 1;

        Parallel.For(0, height, y =>
        {
            int rowStart  = y * width;
            int byteStart = rowStart * 4;

            for (int x = 0; x < width; x++)
            {
                int idx   = Math.Min(mapIndex[rowStart + x], paletteMax);
                var color = palette[idx];

                int offset = byteStart + x * 4;
                outputPixels[offset]     = color.B;
                outputPixels[offset + 1] = color.G;
                outputPixels[offset + 2] = color.R;
                outputPixels[offset + 3] = 255;
            }
        });

        if (existingBitmap != null &&
            existingBitmap.PixelWidth == width &&
            existingBitmap.PixelHeight == height)
        {
            existingBitmap.WritePixels(
                new Int32Rect(0, 0, width, height),
                outputPixels, stride, 0);
            return existingBitmap;
        }

        var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        wb.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);
        return wb;
    }

    /// <summary>
    /// Renders a single tile from a MapIndex array.
    /// Extracts the tile region and renders it as a standalone bitmap.
    /// </summary>
    /// <param name="mapIndex">Full image MapIndex (one byte per pixel).</param>
    /// <param name="palette">Hardware palette.</param>
    /// <param name="imageWidthInPixels">Full image width — needed to compute row offsets.</param>
    /// <param name="tileX">Tile column in the tileset grid.</param>
    /// <param name="tileY">Tile row in the tileset grid.</param>
    /// <param name="tileSize">Tile size in pixels (width and height, assumed square).</param>
    /// <returns>Frozen BitmapSource for the requested tile.</returns>
    public static BitmapSource RenderTile(
        byte[] mapIndex,
        IReadOnlyList<HardwareColor> palette,
        int imageWidthInPixels,
        int tileX,
        int tileY,
        int tileSize)
    {
        int paletteMax = palette.Count - 1;
        int stride     = tileSize * 4;
        byte[] pixels  = new byte[tileSize * tileSize * 4];

        int srcOriginX = tileX * tileSize;
        int srcOriginY = tileY * tileSize;

        for (int py = 0; py < tileSize; py++)
        {
            int srcRow = (srcOriginY + py) * imageWidthInPixels + srcOriginX;
            int dstRow = py * tileSize;

            for (int px = 0; px < tileSize; px++)
            {
                int srcIdx = srcRow + px;
                if (srcIdx >= mapIndex.Length) break;

                int idx   = Math.Min(mapIndex[srcIdx], paletteMax);
                var color = palette[idx];

                int offset = (dstRow + px) * 4;
                pixels[offset]     = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = 255;
            }
        }

        var bmp = BitmapSource.Create(
            tileSize, tileSize, 96, 96,
            PixelFormats.Bgra32, null, pixels, stride);
        bmp.Freeze();
        return bmp;
    }
}
