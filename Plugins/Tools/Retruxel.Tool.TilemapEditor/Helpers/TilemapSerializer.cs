using System;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles tilemap data serialization to/from Base64 and int arrays.
/// Preserves flip flags (bits 9-10) during serialization.
/// </summary>
public static class TilemapSerializer
{
    /// <summary>
    /// Serializes tilemap data to Base64, preserving flip flags.
    /// Uses ushort (16-bit) encoding with sentinel value 0xFFFF for empty cells.
    /// </summary>
    public static string ToBase64(int[] layerData)
    {
        byte[] bytes = new byte[layerData.Length * 2];

        for (int i = 0; i < layerData.Length; i++)
        {
            int tileId = layerData[i];
            
            // ETAPA 4: Preserve bits 0-10 (tileIndex + flipH + flipV)
            // Sentinel: 0xFFFF for empty cells (-1)
            // Max value: 0x7FF (bits 0-10 = 2047)
            ushort value = tileId < 0 ? (ushort)0xFFFF : (ushort)(tileId & 0x7FF);

            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Deserializes tilemap data from Base64, preserving flip flags.
    /// </summary>
    public static int[] FromBase64(string base64Data, int expectedSize)
    {
        byte[] bytes = Convert.FromBase64String(base64Data);
        int tileCount = bytes.Length / 2;
        var result = new int[expectedSize];
        Array.Fill(result, -1);

        for (int i = 0; i < Math.Min(tileCount, expectedSize); i++)
        {
            ushort value = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            
            // ETAPA 4: Preserve all bits (including flip flags)
            // Sentinel 0xFFFF → -1, otherwise keep value as-is
            result[i] = value == 0xFFFF ? -1 : (int)value;
        }

        return result;
    }

    /// <summary>
    /// Creates a copy of tilemap data as int array.
    /// </summary>
    public static int[] ToIntArray(int[] layerData)
    {
        var result = new int[layerData.Length];
        Array.Copy(layerData, result, layerData.Length);
        return result;
    }
}
