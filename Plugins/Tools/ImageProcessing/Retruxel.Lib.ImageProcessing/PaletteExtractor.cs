using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

public class PaletteExtractor
{
    public SKColor[] Extract(SKBitmap image, int maxColors)
    {
        var colors = new HashSet<SKColor>();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                colors.Add(image.GetPixel(x, y));
            }
        }

        if (colors.Count > maxColors)
            throw new InvalidOperationException($"Image has {colors.Count} colors, but maximum is {maxColors}");

        return colors.ToArray();
    }
}
