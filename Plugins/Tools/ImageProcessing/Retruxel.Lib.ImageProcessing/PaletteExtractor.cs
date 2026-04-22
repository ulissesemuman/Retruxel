using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

public static class PaletteExtractor
{
    /// <summary>
    /// Extracts unique colors from pixel array.
    /// </summary>
    /// <param name="pixels">Pixel array in RGBA format (0xAARRGGBB)</param>
    /// <param name="maxColors">Maximum number of colors allowed</param>
    /// <returns>Array of unique RGB colors (0x00RRGGBB, alpha stripped)</returns>
    public static uint[] Extract(uint[] pixels, int maxColors)
    {
        var colors = new HashSet<uint>();

        foreach (var pixel in pixels)
        {
            // Strip alpha channel, keep only RGB
            var rgb = pixel & 0x00FFFFFF;
            
            // Skip fully transparent pixels
            if ((pixel >> 24) > 0)
                colors.Add(rgb);
        }

        if (colors.Count > maxColors)
            throw new InvalidOperationException($"Image has {colors.Count} colors, but maximum is {maxColors}");

        return colors.ToArray();
    }

    /// <summary>
    /// Ensures black (0x000000) is at index 0 in the palette.
    /// If black doesn't exist, it's added. If it exists elsewhere, it's moved to index 0.
    /// </summary>
    public static uint[] EnsureBlackAtZero(uint[] palette)
    {
        const uint black = 0x000000;

        // Check if black is already at index 0
        if (palette.Length > 0 && palette[0] == black)
            return palette;

        // Find black in palette
        var blackIndex = Array.IndexOf(palette, black);

        if (blackIndex > 0)
        {
            // Black exists but not at index 0 - swap
            var result = palette.ToArray();
            result[blackIndex] = result[0];
            result[0] = black;
            return result;
        }
        else if (blackIndex < 0)
        {
            // Black doesn't exist - prepend it
            var result = new uint[palette.Length + 1];
            result[0] = black;
            Array.Copy(palette, 0, result, 1, palette.Length);
            return result;
        }

        return palette;
    }
}
