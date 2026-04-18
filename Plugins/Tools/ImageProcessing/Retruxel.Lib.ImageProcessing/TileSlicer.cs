using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Retruxel.Lib.ImageProcessing;

public class TileSlicer
{
    public byte[][] SliceIntoTiles(SKBitmap image, SKColor[] palette, int tileWidth, int tileHeight)
    {
        int tilesX = image.Width / tileWidth;
        int tilesY = image.Height / tileHeight;
        var tiles = new List<byte[]>();

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                var tilePixels = new byte[tileWidth * tileHeight];
                int pixelIndex = 0;

                for (int py = 0; py < tileHeight; py++)
                {
                    for (int px = 0; px < tileWidth; px++)
                    {
                        int x = tx * tileWidth + px;
                        int y = ty * tileHeight + py;
                        SKColor pixel = image.GetPixel(x, y);
                        tilePixels[pixelIndex++] = (byte)Array.IndexOf(palette, pixel);
                    }
                }

                tiles.Add(tilePixels);
            }
        }

        return tiles.ToArray();
    }
}
