using SkiaSharp;
using System;
using System.IO;

namespace Retruxel.Lib.ImageProcessing;

public static class PngReader
{
    /// <summary>
    /// Loads a PNG file and returns pixel data as RGBA uint array.
    /// </summary>
    /// <param name="path">Path to PNG file</param>
    /// <param name="width">Output: image width in pixels</param>
    /// <param name="height">Output: image height in pixels</param>
    /// <returns>Pixel array in RGBA format (0xAARRGGBB)</returns>
    public static uint[] Load(string path, out int width, out int height)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image file not found: {path}");

        using var bitmap = SKBitmap.Decode(path);
        if (bitmap == null)
            throw new InvalidOperationException($"Failed to decode image: {path}");

        width = bitmap.Width;
        height = bitmap.Height;

        var pixels = new uint[width * height];
        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                // Store as 0xAARRGGBB
                pixels[index++] = (uint)((color.Alpha << 24) | (color.Red << 16) | (color.Green << 8) | color.Blue);
            }
        }

        return pixels;
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// </summary>
    public static SKBitmap LoadBitmap(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image file not found: {path}");

        return SKBitmap.Decode(path);
    }
}
