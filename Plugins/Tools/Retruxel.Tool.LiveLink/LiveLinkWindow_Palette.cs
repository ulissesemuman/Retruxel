using Retruxel.Tool.LiveLink.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Palette decoding and validation for all supported consoles.
/// </summary>
public partial class LiveLinkWindow
{
    private uint[] DecodeSmsPalette(byte[] paletteData)
    {
        // SMS: 6-bit RGB (2 bits per channel) - format: 00BBGGRR
        var palette = new uint[paletteData.Length];
        for (int i = 0; i < paletteData.Length; i++)
        {
            byte smsColor = paletteData[i];
            byte r = (byte)(((smsColor >> 0) & 0x03) * 85);
            byte g = (byte)(((smsColor >> 2) & 0x03) * 85);
            byte b = (byte)(((smsColor >> 4) & 0x03) * 85);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeGameGearPalette(byte[] paletteData)
    {
        // Game Gear: 12-bit RGB (4 bits per channel) - 2 bytes per color, little-endian
        // Format: 0000BBBBGGGGRRRR
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort ggColor = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((ggColor >> 0) & 0x0F) * 17);
            byte g = (byte)(((ggColor >> 4) & 0x0F) * 17);
            byte b = (byte)(((ggColor >> 8) & 0x0F) * 17);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeGameBoyPalette(byte[] paletteData)
    {
        // GB/GBC: 15-bit RGB (5-5-5), 2 bytes per color, little-endian
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort color15 = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((color15 >> 0) & 0x1F) * 8);
            byte g = (byte)(((color15 >> 5) & 0x1F) * 8);
            byte b = (byte)(((color15 >> 10) & 0x1F) * 8);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeSnesPalette(byte[] paletteData)
    {
        // SNES: 15-bit RGB (5-5-5), 2 bytes per color, little-endian
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort color15 = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((color15 >> 0) & 0x1F) * 8);
            byte g = (byte)(((color15 >> 5) & 0x1F) * 8);
            byte b = (byte)(((color15 >> 10) & 0x1F) * 8);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeNesPalette(byte[] paletteData)
    {
        // NES master palette (64 colors)
        uint[] nesMasterPalette = new uint[64]
        {
            0xFF666666, 0xFF002A88, 0xFF1412A7, 0xFF3B00A4, 0xFF5C007E, 0xFF6E0040, 0xFF6C0600, 0xFF561D00,
            0xFF333500, 0xFF0B4800, 0xFF005200, 0xFF004F08, 0xFF00404D, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFADADAD, 0xFF155FD9, 0xFF4240FF, 0xFF7527FE, 0xFFA01ACC, 0xFFB71E7B, 0xFFB53120, 0xFF994E00,
            0xFF6B6D00, 0xFF388700, 0xFF0C9300, 0xFF008F32, 0xFF007C8D, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFFFFEFF, 0xFF64B0FF, 0xFF9290FF, 0xFFC676FF, 0xFFF36AFF, 0xFFFE6ECC, 0xFFFE8170, 0xFFEA9E22,
            0xFFBCBE00, 0xFF88D800, 0xFF5CE430, 0xFF45E082, 0xFF48CDDE, 0xFF4F4F4F, 0xFF000000, 0xFF000000,
            0xFFFFFEFF, 0xFFC0DFFF, 0xFFD3D2FF, 0xFFE8C8FF, 0xFFFBC2FF, 0xFFFEC4EA, 0xFFFECCC5, 0xFFF7D8A5,
            0xFFE4E594, 0xFFCFEF96, 0xFFBDF4AB, 0xFFB3F3CC, 0xFFB5EBF2, 0xFFB8B8B8, 0xFF000000, 0xFF000000
        };

        var palette = new uint[paletteData.Length];

        // Debug: Log raw palette data
        var rawIndices = string.Join(" ", paletteData.Select(b => $"{b:X2}"));
        LogInfo($"NES palette raw bytes: {rawIndices}");

        for (int i = 0; i < paletteData.Length; i++)
        {
            byte colorIndex = (byte)(paletteData[i] & 0x3F); // NES palette is 6-bit
            palette[i] = nesMasterPalette[colorIndex];
        }

        // Debug: Count unique colors
        var uniqueColors = palette.Distinct().ToList();
        LogInfo($"NES palette: {palette.Length} total entries, {uniqueColors.Count} unique colors");

        // Debug: Show which palette slots use which colors
        for (int pal = 0; pal < 8; pal++)
        {
            var palColors = new System.Collections.Generic.List<string>();
            for (int col = 0; col < 4; col++)
            {
                int idx = pal * 4 + col;
                if (idx < paletteData.Length)
                {
                    byte rawIdx = paletteData[idx];
                    palColors.Add($"{rawIdx:X2}");
                }
            }
            LogInfo($"  Palette {pal}: [{string.Join(", ", palColors)}]");
        }

        return palette;
    }
}
