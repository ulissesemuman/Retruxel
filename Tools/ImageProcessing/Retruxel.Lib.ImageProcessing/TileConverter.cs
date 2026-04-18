using System;

namespace Retruxel.Lib.ImageProcessing;

public class TileConverter
{
    public byte[] Convert(byte[] pixels, int bpp, int tileWidth, int tileHeight, TileFormat format, InterleaveMode interleave)
    {
        return format switch
        {
            TileFormat.Planar => ConvertToPlanar(pixels, bpp, tileWidth, tileHeight, interleave),
            TileFormat.Linear => ConvertToLinear(pixels, bpp),
            TileFormat.Chunky => ConvertToChunky(pixels, bpp),
            _ => throw new NotSupportedException($"Format {format} not supported")
        };
    }

    private byte[] ConvertToPlanar(byte[] pixels, int bpp, int tileWidth, int tileHeight, InterleaveMode interleave)
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

    private byte[] ConvertToLinear(byte[] pixels, int bpp)
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

    private byte[] ConvertToChunky(byte[] pixels, int bpp)
    {
        // TODO: Implement chunky format if needed
        throw new NotImplementedException("Chunky format not yet implemented");
    }
}
