using Retruxel.Core.Models;
using System;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles plane data serialization to/from Base64.
/// Format: 4 bytes per tile entry (TileIndex LE ushort + flags byte + rotation byte).
/// Internal to TilemapEditor — not shared with other tools.
/// </summary>
public static class PlaneSerializer
{
    public static string ToBase64(TileEntry[] layerData)
    {
        byte[] bytes = new byte[layerData.Length * 4];

        for (int i = 0; i < layerData.Length; i++)
        {
            var entry  = layerData[i];
            int offset = i * 4;

            ushort tileIndex = entry.TileIndex < 0 ? (ushort)0xFFFF : (ushort)entry.TileIndex;
            bytes[offset]     = (byte)(tileIndex & 0xFF);
            bytes[offset + 1] = (byte)((tileIndex >> 8) & 0xFF);

            byte flags = 0;
            if (entry.FlipH) flags |= 0x01;
            if (entry.FlipV) flags |= 0x02;
            bytes[offset + 2] = flags;

            bytes[offset + 3] = entry.Rotation switch
            {
                90  => 1,
                180 => 2,
                270 => 3,
                _   => 0
            };
        }

        return Convert.ToBase64String(bytes);
    }

    public static TileEntry[] FromBase64(string base64Data, int expectedSize)
    {
        byte[] bytes      = Convert.FromBase64String(base64Data);
        int    entryCount = bytes.Length / 4;
        var    result     = new TileEntry[expectedSize];

        for (int i = 0; i < expectedSize; i++)
            result[i] = TileEntry.Empty;

        for (int i = 0; i < Math.Min(entryCount, expectedSize); i++)
        {
            int    offset    = i * 4;
            ushort tileIndex = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
            byte   flags     = bytes[offset + 2];
            bool   flipH     = (flags & 0x01) != 0;
            bool   flipV     = (flags & 0x02) != 0;
            int    rotation  = bytes[offset + 3] switch { 1 => 90, 2 => 180, 3 => 270, _ => 0 };

            result[i] = tileIndex == 0xFFFF
                ? TileEntry.Empty
                : new TileEntry { TileIndex = tileIndex, FlipH = flipH, FlipV = flipV, Rotation = rotation };
        }

        return result;
    }
}
