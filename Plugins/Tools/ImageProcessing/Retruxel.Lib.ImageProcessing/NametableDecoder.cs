namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Decodes nametable/tilemap data from VRAM.
/// </summary>
public static class NametableDecoder
{
    /// <summary>
    /// Decodes nametable data from raw VRAM bytes.
    /// </summary>
    /// <param name="vramData">Raw nametable bytes</param>
    /// <param name="width">Nametable width in tiles</param>
    /// <param name="height">Nametable height in tiles</param>
    /// <param name="bytesPerEntry">Bytes per nametable entry (1 for NES, 2 for SMS)</param>
    /// <returns>Array of nametable entries</returns>
    public static ushort[] Decode(byte[] vramData, int width, int height, int bytesPerEntry = 2)
    {
        int entryCount = width * height;
        var nametable = new ushort[entryCount];

        if (bytesPerEntry == 2)
        {
            // SMS format: 2 bytes per entry (tile index + attributes)
            for (int i = 0; i < entryCount && i * 2 + 1 < vramData.Length; i++)
            {
                nametable[i] = (ushort)(vramData[i * 2] | (vramData[i * 2 + 1] << 8));
            }
        }
        else if (bytesPerEntry == 1)
        {
            // NES format: 1 byte per entry (tile index only)
            for (int i = 0; i < entryCount && i < vramData.Length; i++)
            {
                nametable[i] = vramData[i];
            }
        }

        return nametable;
    }
}
