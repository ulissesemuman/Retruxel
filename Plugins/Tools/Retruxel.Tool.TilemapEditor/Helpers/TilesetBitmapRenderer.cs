using System;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Renders tiles to WriteableBitmap with palette mapping and color table support.
/// </summary>
public static class TilesetBitmapRenderer
{
    /// <summary>
    /// Draws a single tile to a locked bitmap using unsafe pointer operations.
    /// </summary>
    public static unsafe void DrawTileUnsafe(
        byte* backBuffer,
        int stride,
        byte[] tilePixels,
        uint[] palette,
        int offsetX,
        int offsetY,
        int tileWidth,
        int tileHeight,
        int tileIndex,
        byte[]? colorTable)
    {
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                int pixelIndex = y * tileWidth + x;
                if (pixelIndex >= tilePixels.Length) continue;

                byte colorIndex = tilePixels[pixelIndex];
                uint color = ResolveColor(colorIndex, palette, tileIndex, colorTable);

                byte a = (byte)((color >> 24) & 0xFF);
                byte r = (byte)((color >> 16) & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)(color & 0xFF);

                int bitmapX = offsetX + x;
                int bitmapY = offsetY + y;
                int offset = bitmapY * stride + bitmapX * 4;

                backBuffer[offset] = b;
                backBuffer[offset + 1] = g;
                backBuffer[offset + 2] = r;
                backBuffer[offset + 3] = a;
            }
        }
    }

    /// <summary>
    /// Resolves a color index to an ARGB color, handling color tables and special cases.
    /// </summary>
    private static uint ResolveColor(byte colorIndex, uint[] palette, int tileIndex, byte[]? colorTable)
    {
        // SG-1000 with Color Table: Apply FG/BG colors from Color Table
        if (colorTable != null && tileIndex >= 0 && palette.Length == 16)
        {
            int colorTableIndex = tileIndex / 8;
            if (colorTableIndex < colorTable.Length)
            {
                byte colorByte = colorTable[colorTableIndex];
                byte fgColor = (byte)((colorByte >> 4) & 0x0F);
                byte bgColor = (byte)(colorByte & 0x0F);
                byte paletteIndex = colorIndex == 0 ? bgColor : fgColor;
                return palette[paletteIndex];
            }
            else
            {
                return colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
            }
        }
        // Special handling for 1bpp without Color Table
        else if (palette.Length == 16 && colorIndex <= 1)
        {
            return colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
        }
        // Normal palette lookup
        else
        {
            if (colorIndex >= palette.Length)
            {
                // Out of range - use magenta for debugging
                return 0xFFFF00FF;
            }
            else
            {
                return palette[colorIndex];
            }
        }
    }
}
