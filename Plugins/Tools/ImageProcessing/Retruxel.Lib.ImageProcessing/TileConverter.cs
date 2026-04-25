using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Decodes raw VRAM data back to tiles (inverse of Convert).
    /// </summary>
    /// <param name="vramData">Raw VRAM bytes</param>
    /// <param name="tileCount">Number of tiles to decode</param>
    /// <param name="bpp">Bits per pixel</param>
    /// <param name="tileWidth">Tile width in pixels</param>
    /// <param name="tileHeight">Tile height in pixels</param>
    /// <param name="format">Source tile format</param>
    /// <param name="interleave">Interleave mode</param>
    /// <returns>Array of decoded tiles (each tile is palette indices)</returns>
    public static byte[][] Decode(byte[] vramData, int tileCount, int bpp, int tileWidth, int tileHeight, TileFormat format, InterleaveMode interleave)
    {
        var tiles = new byte[tileCount][];
        int bytesPerTile = format switch
        {
            TileFormat.Planar => (tileWidth * tileHeight * bpp) / 8,
            TileFormat.Linear => (tileWidth * tileHeight * bpp) / 8,
            _ => throw new NotSupportedException($"Format {format} not supported for decoding")
        };

        for (int t = 0; t < tileCount; t++)
        {
            tiles[t] = DecodeSingleTile(vramData, t * bytesPerTile, bpp, tileWidth, tileHeight, format, interleave);
        }

        return tiles;
    }

    private static byte[] DecodeSingleTile(byte[] vramData, int offset, int bpp, int tileWidth, int tileHeight, TileFormat format, InterleaveMode interleave)
    {
        return format switch
        {
            TileFormat.Planar => DecodeFromPlanar(vramData, offset, bpp, tileWidth, tileHeight, interleave),
            TileFormat.Linear => DecodeFromLinear(vramData, offset, bpp, tileWidth, tileHeight),
            _ => throw new NotSupportedException($"Format {format} not supported for decoding")
        };
    }

    private static byte[] DecodeFromPlanar(byte[] vramData, int offset, int bpp, int tileWidth, int tileHeight, InterleaveMode interleave)
    {
        var pixels = new byte[tileWidth * tileHeight];
        int bytesPerPlane = tileWidth * tileHeight / 8;

        if (interleave == InterleaveMode.Tile)
        {
            // SMS/NES style: all plane 0, then all plane 1
            for (int row = 0; row < tileHeight; row++)
            {
                for (int col = 0; col < tileWidth; col++)
                {
                    int pixelIndex = row * tileWidth + col;
                    byte pixelValue = 0;

                    for (int plane = 0; plane < bpp; plane++)
                    {
                        int byteIndex = offset + plane * bytesPerPlane + row;
                        int bit = (vramData[byteIndex] >> (7 - col)) & 1;
                        pixelValue |= (byte)(bit << plane);
                    }

                    pixels[pixelIndex] = pixelValue;
                }
            }
        }
        else if (interleave == InterleaveMode.Line)
        {
            // Game Boy style: interleaved planes per line
            for (int row = 0; row < tileHeight; row++)
            {
                for (int col = 0; col < tileWidth; col++)
                {
                    int pixelIndex = row * tileWidth + col;
                    byte pixelValue = 0;

                    for (int plane = 0; plane < bpp; plane++)
                    {
                        int byteIndex = offset + row * bpp + plane;
                        int bit = (vramData[byteIndex] >> (7 - col)) & 1;
                        pixelValue |= (byte)(bit << plane);
                    }

                    pixels[pixelIndex] = pixelValue;
                }
            }
        }

        return pixels;
    }

    private static byte[] DecodeFromLinear(byte[] vramData, int offset, int bpp, int tileWidth, int tileHeight)
    {
        var pixels = new byte[tileWidth * tileHeight];
        int pixelsPerByte = 8 / bpp;

        for (int i = 0; i < pixels.Length; i++)
        {
            int byteIndex = offset + i / pixelsPerByte;
            int bitShift = (pixelsPerByte - 1 - (i % pixelsPerByte)) * bpp;
            int mask = (1 << bpp) - 1;
            pixels[i] = (byte)((vramData[byteIndex] >> bitShift) & mask);
        }

        return pixels;
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
