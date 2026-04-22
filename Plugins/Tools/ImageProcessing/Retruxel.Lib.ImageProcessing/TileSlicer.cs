using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

public static class TileSlicer
{
    /// <summary>
    /// Slices pixel array into tiles and maps to palette indices.
    /// </summary>
    /// <param name="pixels">Pixel array in RGBA format</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <param name="tileWidth">Tile width (usually 8)</param>
    /// <param name="tileHeight">Tile height (usually 8)</param>
    /// <param name="palette">Color palette for mapping</param>
    /// <returns>Array of tiles, each tile is an array of palette indices</returns>
    public static byte[][] Slice(uint[] pixels, int width, int height, int tileWidth, int tileHeight, uint[] palette)
    {
        int tilesX = width / tileWidth;
        int tilesY = height / tileHeight;
        var tiles = new List<byte[]>();

        // Build palette lookup for fast color-to-index mapping
        var paletteLookup = new Dictionary<uint, byte>();
        for (int i = 0; i < palette.Length; i++)
            paletteLookup[palette[i]] = (byte)i;

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
                        int srcIndex = y * width + x;
                        
                        // Get RGB (strip alpha)
                        uint rgb = pixels[srcIndex] & 0x00FFFFFF;
                        
                        // Map to palette index
                        if (paletteLookup.TryGetValue(rgb, out var paletteIndex))
                            tilePixels[pixelIndex++] = paletteIndex;
                        else
                            tilePixels[pixelIndex++] = 0; // Default to color 0 if not found
                    }
                }

                tiles.Add(tilePixels);
            }
        }

        return tiles.ToArray();
    }
}
