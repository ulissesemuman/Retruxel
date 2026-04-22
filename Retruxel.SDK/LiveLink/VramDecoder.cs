namespace Retruxel.SDK.LiveLink;

/// <summary>
/// Decodes VRAM data back to tiles and images (inverse of TileConverter).
/// </summary>
public static class VramDecoder
{
    public static byte[][] DecodePlanarTiles(byte[] vramData, int tileCount, int bpp, int tileWidth = 8, int tileHeight = 8, bool interleaved = false)
    {
        var tiles = new byte[tileCount][];
        int bytesPerPlane = tileWidth * tileHeight / 8;
        int bytesPerTile = interleaved ? bytesPerPlane * bpp : bytesPerPlane * bpp;
        
        for (int t = 0; t < tileCount; t++)
        {
            tiles[t] = DecodeSinglePlanarTile(vramData, t * bytesPerTile, bpp, tileWidth, tileHeight, interleaved);
        }
        
        return tiles;
    }
    
    private static byte[] DecodeSinglePlanarTile(byte[] vramData, int offset, int bpp, int tileWidth, int tileHeight, bool interleaved)
    {
        var pixels = new byte[tileWidth * tileHeight];
        int bytesPerPlane = tileWidth * tileHeight / 8;
        
        if (!interleaved)
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
        else
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
    
    public static ushort[] DecodeNametable(byte[] vramData, int width, int height)
    {
        int entryCount = width * height;
        var nametable = new ushort[entryCount];
        
        for (int i = 0; i < entryCount; i++)
        {
            nametable[i] = (ushort)(vramData[i * 2] | (vramData[i * 2 + 1] << 8));
        }
        
        return nametable;
    }
}
