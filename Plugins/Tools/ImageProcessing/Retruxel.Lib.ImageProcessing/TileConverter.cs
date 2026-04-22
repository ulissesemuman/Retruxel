using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

public static class TileConverter
{
    /// <summary>
    /// Converts tiles to target format.
    /// </summary>
    /// <param name="tiles">Array of tiles (each tile is palette indices)</param>
    /// <param name="palette">Color palette</param>
    /// <param name="format">Target tile format</param>
    /// <param name="interleave">Interleave mode</param>
    /// <param name="bpp">Bits per pixel</param>
    /// <returns>Converted tile data as byte array</returns>
    public static byte[] Convert(byte[][] tiles, uint[] palette, TileFormat format, InterleaveMode interleave, int bpp)
    {
        var result = new List<byte>();

        foreach (var tile in tiles)
        {
            var converted = ConvertSingleTile(tile, palette, format, interleave, bpp);
            result.AddRange(converted);
        }

        return result.ToArray();
    }

    private static byte[] ConvertSingleTile(byte[] pixels, uint[] palette, TileFormat format, InterleaveMode interleave, int bpp)
    {
        return format switch
        {
            TileFormat.Planar => ConvertToPlanar(pixels, bpp, 8, 8, interleave),
            TileFormat.Linear => ConvertToLinear(pixels, bpp),
            TileFormat.Chunky => ConvertToChunky(pixels, bpp),
            _ => throw new NotSupportedException($"Format {format} not supported")
        };
    }

    private static byte[] ConvertToPlanar(byte[] pixels, int bpp, int tileWidth, int tileHeight, InterleaveMode interleave)
    {
        int pixelsPerTile = tileWidth * tileHeight;
        int bytesPerPlane = pixelsPerTile / 8;
        int totalBytes = bytesPerPlane * bpp;
        var output = new byte[totalBytes];

        if (interleave == InterleaveMode.Tile)
        {
            // SMS/NES style: all plane 0, then all plane 1, etc.
            for (int plane = 0; plane < bpp; plane++)
            {
                for (int row = 0; row < tileHeight; row++)
                {
                    byte rowByte = 0;
                    for (int col = 0; col < tileWidth; col++)
                    {
                        int pixelIndex = row * tileWidth + col;
                        int bit = (pixels[pixelIndex] >> plane) & 1;
                        rowByte |= (byte)(bit << (7 - col));
                    }
                    output[plane * bytesPerPlane + row] = rowByte;
                }
            }
        }
        else if (interleave == InterleaveMode.Line)
        {
            // Game Boy style: interleave planes per line
            for (int row = 0; row < tileHeight; row++)
            {
                for (int plane = 0; plane < bpp; plane++)
                {
                    byte rowByte = 0;
                    for (int col = 0; col < tileWidth; col++)
                    {
                        int pixelIndex = row * tileWidth + col;
                        int bit = (pixels[pixelIndex] >> plane) & 1;
                        rowByte |= (byte)(bit << (7 - col));
                    }
                    output[row * bpp + plane] = rowByte;
                }
            }
        }

        return output;
    }

    private static byte[] ConvertToLinear(byte[] pixels, int bpp)
    {
        // CGA style: pixels packed sequentially
        int pixelsPerByte = 8 / bpp;
        int totalBytes = (pixels.Length + pixelsPerByte - 1) / pixelsPerByte;
        var output = new byte[totalBytes];

        for (int i = 0; i < pixels.Length; i++)
        {
            int byteIndex = i / pixelsPerByte;
            int bitShift = (pixelsPerByte - 1 - (i % pixelsPerByte)) * bpp;
            output[byteIndex] |= (byte)(pixels[i] << bitShift);
        }

        return output;
    }

    private static byte[] ConvertToChunky(byte[] pixels, int bpp)
    {
        // TODO: Implement chunky format if needed
        throw new NotImplementedException("Chunky format not yet implemented");
    }
}
