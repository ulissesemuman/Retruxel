using Retruxel.Core.Models;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Handles reading and writing indexed PNG files (palette mode).
/// Indexed PNG stores pixel values as color table indices (0–N),
/// separate from the actual RGB colors in the palette.
///
/// This separation is the foundation of Retruxel's dynamic palette system:
/// the same indexed PNG can be rendered with different color palettes
/// without modifying the asset file.
/// </summary>
public class MapIndexService
{
    /// <summary>
    /// Reads a PNG and returns pixel indices and palette colors.
    /// Builds a color map from unique colors in the image.
    /// Reuses Encode() for fast pixel processing.
    /// </summary>
    public IndexedPngData? Read(string pngPath)
    {
        if (string.IsNullOrWhiteSpace(pngPath) || !File.Exists(pngPath))
            return null;

        using var bitmap = SKBitmap.Decode(pngPath);
        if (bitmap == null)
            return null;

        var width = bitmap.Width;
        var height = bitmap.Height;

        // Build unique color palette
        var colorSet = new HashSet<SKColor>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorSet.Add(bitmap.GetPixel(x, y));
            }
        }

        var palette = colorSet
            .Select(c => new HardwareColor(c.Red, c.Green, c.Blue))
            .ToList();

        // Use Encode() for fast indexing
        var indices = IndexedBitmapRenderer.Encode(bitmap, palette);

        return new IndexedPngData
        {
            Width = width,
            Height = height,
            Indices = indices,
            Colors = palette.Select(c => $"#{c.R:X2}{c.G:X2}{c.B:X2}").ToList()
        };
    }

    /// <summary>
    /// Saves IndexedPngData as a PNG file.
    /// Note: Saves as RGBA but maintains indexed structure in memory.
    /// </summary>
    public void Write(IndexedPngData data, string outputPath)
    {
        var imageInfo = new SKImageInfo(data.Width, data.Height, SKColorType.Rgba8888);
        using var bitmap = new SKBitmap(imageInfo);

        // Parse palette colors
        var palette = data.Colors.Select(h => SKColor.Parse(h)).ToArray();

        // Write pixels using palette
        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var idx = data.Indices[y * data.Width + x];
                var color = idx < palette.Length ? palette[idx] : SKColors.Black;
                bitmap.SetPixel(x, y, color);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encData = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        encData.SaveTo(stream);
    }

    /// <summary>
    /// Renders an IndexedPngData to an SKBitmap
    /// using the provided palette colors for preview.
    /// Called whenever the user changes the palette slot in TilemapEditor.
    /// </summary>
    public SKBitmap RenderPreview(
        IndexedPngData indexedData,
        IReadOnlyList<string> paletteColors,
        int scale = 1)
    {
        var w = indexedData.Width * scale;
        var h = indexedData.Height * scale;

        var imageInfo = new SKImageInfo(w, h, SKColorType.Bgra8888);
        var bitmap = new SKBitmap(imageInfo);

        for (int y = 0; y < indexedData.Height; y++)
        {
            for (int x = 0; x < indexedData.Width; x++)
            {
                var idx = indexedData.Indices[y * indexedData.Width + x];
                var hex = idx < paletteColors.Count ? paletteColors[idx] : "#000000";
                var color = SKColor.Parse(hex);

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        bitmap.SetPixel(x * scale + sx, y * scale + sy, color);
                    }
                }
            }
        }

        return bitmap;
    }

    private static int FindNearestIndex(SKColor pixel, List<SKColor> palette)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < palette.Count; i++)
        {
            var dr = pixel.Red - palette[i].Red;
            var dg = pixel.Green - palette[i].Green;
            var db = pixel.Blue - palette[i].Blue;
            var d = dr * dr + dg * dg + db * db;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
}

/// <summary>
/// Indexed PNG data: pixel indices + suggested color palette.
/// </summary>
public class IndexedPngData
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Indices { get; init; } = [];
    public List<string> Colors { get; init; } = new(); // Hex strings for JSON serialization

    /// <summary>
    /// RGB colors as byte triplets (R, G, B) for direct hardware conversion.
    /// Cached on first access to avoid repeated parsing.
    /// </summary>
    public byte[] ColorsRgb
    {
        get
        {
            if (_colorsRgb == null)
            {
                _colorsRgb = new byte[Colors.Count * 3];
                for (int i = 0; i < Colors.Count; i++)
                {
                    var hex = Colors[i].TrimStart('#');
                    _colorsRgb[i * 3 + 0] = System.Convert.ToByte(hex.Substring(0, 2), 16); // R
                    _colorsRgb[i * 3 + 1] = System.Convert.ToByte(hex.Substring(2, 2), 16); // G
                    _colorsRgb[i * 3 + 2] = System.Convert.ToByte(hex.Substring(4, 2), 16); // B
                }
            }
            return _colorsRgb;
        }
    }
    private byte[]? _colorsRgb;
}
