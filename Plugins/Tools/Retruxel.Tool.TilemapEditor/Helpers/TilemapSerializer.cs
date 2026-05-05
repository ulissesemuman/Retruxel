using System;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles tilemap data serialization to/from Base64 and int arrays.
/// </summary>
public static class TilemapSerializer
{
    public static string ToBase64(int[] layerData)
    {
        byte[] bytes = new byte[layerData.Length * 2];

        for (int i = 0; i < layerData.Length; i++)
        {
            int tileId = layerData[i];
            ushort value = tileId < 0 ? (ushort)0xFFFF : (ushort)Math.Min(65534, tileId);

            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return Convert.ToBase64String(bytes);
    }

    public static int[] FromBase64(string base64Data, int expectedSize)
    {
        byte[] bytes = Convert.FromBase64String(base64Data);
        int tileCount = bytes.Length / 2;
        var result = new int[expectedSize];
        Array.Fill(result, -1);

        for (int i = 0; i < Math.Min(tileCount, expectedSize); i++)
        {
            ushort value = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            result[i] = value == 0xFFFF ? -1 : value;
        }

        return result;
    }

    public static int[] ToIntArray(int[] layerData)
    {
        var result = new int[layerData.Length];
        Array.Copy(layerData, result, layerData.Length);
        return result;
    }
}
