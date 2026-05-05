using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Minimal BDF (Bitmap Distribution Format) parser.
/// Extracts 8x8 glyph bitmaps from BDF font files.
/// Only processes glyphs where DWIDTH == 8 and bounding box fits 8x8.
/// </summary>
public static class BdfParser
{
    public static List<TileEntry> Parse(string bdfContent)
    {
        var result = new List<TileEntry>();
        var lines = bdfContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("STARTCHAR"))
            {
                var glyph = ParseGlyph(lines, ref i);
                if (glyph != null)
                    result.Add(glyph);
            }
        }

        return result;
    }

    private static TileEntry? ParseGlyph(string[] lines, ref int index)
    {
        int encoding = -1;
        int dwidth = 0;
        var bitmapLines = new List<string>();
        bool inBitmap = false;

        while (index < lines.Length)
        {
            var line = lines[index++].Trim();

            if (line.StartsWith("ENCODING"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                    int.TryParse(parts[1], out encoding);
            }
            else if (line.StartsWith("DWIDTH"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                    int.TryParse(parts[1], out dwidth);
            }
            else if (line.StartsWith("BITMAP"))
            {
                inBitmap = true;
            }
            else if (line.StartsWith("ENDCHAR"))
            {
                break;
            }
            else if (inBitmap && !string.IsNullOrWhiteSpace(line))
            {
                bitmapLines.Add(line);
            }
        }

        // Skip if not 8 pixels wide or invalid encoding
        if (dwidth != 8 || encoding < 0 || encoding > 0xFFFF)
            return null;

        // Convert hex bitmap to font8x8 format
        var bitmap = new byte[8];
        for (int i = 0; i < Math.Min(8, bitmapLines.Count); i++)
        {
            if (byte.TryParse(bitmapLines[i], NumberStyles.HexNumber, null, out byte b))
            {
                // BDF is MSB first, font8x8 is LSB first - reverse bits
                bitmap[i] = ReverseBits(b);
            }
        }

        return new TileEntry
        {
            Character = (char)encoding,
            Bitmap = bitmap,
            SourceLabel = "BDF font",
            SourceIndex = encoding
        };
    }

    private static byte ReverseBits(byte b)
    {
        byte result = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((b & (1 << (7 - i))) != 0)
                result |= (byte)(1 << i);
        }
        return result;
    }
}
