using Retruxel.Tool.LiveLink.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Preview rendering: tilemap with nametable, tileset grid fallback.
/// </summary>
public partial class LiveLinkWindow
{
    private void RenderPreview(CaptureResult capture)
    {
        if (capture.Tiles == null || capture.Tiles.Length == 0)
        {
            ImgPreview.Source = null;
            return;
        }

        // If we have nametable AND palette, render the full scene (tilemap)
        if (capture.Nametable != null && capture.NametableWidth > 0 && capture.NametableHeight > 0 && capture.Palette != null)
        {
            var sceneBitmap = CreatePreviewBitmap(capture);
            if (sceneBitmap != null)
            {
                ImgPreview.Source = sceneBitmap;
                LogInfo("Preview: Rendered tilemap (nametable + tiles + palette)");
                return;
            }
        }

        // Otherwise render tiles in a grid (fallback when no nametable or no palette)
        LogInfo("Preview: Rendering tiles in grid (no nametable or no palette)");
        RenderTilesInGrid(capture);
    }

    private BitmapSource? CreatePreviewBitmap(CaptureResult capture)
    {
        // This creates a bitmap with ORIGINAL source console colors (not mapped to target hardware)
        if (capture.Tiles == null || capture.Tiles.Length == 0 || capture.Palette == null || capture.Palette.Length == 0)
            return null;

        if (capture.Nametable == null || capture.NametableWidth == 0 || capture.NametableHeight == 0)
            return null;

        int tileSize = 8;
        int width = capture.NametableWidth * tileSize;
        int height = capture.NametableHeight * tileSize;

        // Detect console type and palette format
        bool isNes = capture.Metadata.ContainsKey("attributeTable");
        byte[]? nesAttributeTable = isNes ? capture.Metadata["attributeTable"] as byte[] : null;

        bool hasMultiplePalettes = capture.Palette.Length > 16;
        int colorsPerPalette = hasMultiplePalettes ? 16 : capture.Palette.Length;

        LogInfo($"CreatePreviewBitmap: Console={(_sourceConsole ?? "unknown")}, Palette has {capture.Palette.Length} colors, hasMultiplePalettes={hasMultiplePalettes}, colorsPerPalette={colorsPerPalette}");

        if (isNes && nesAttributeTable != null)
        {
            LogInfo($"NES mode: Using attribute table ({nesAttributeTable.Length} bytes)");
        }

        // Debug: Log first few nametable entries
        var sampleEntries = capture.Nametable.Take(10).Select(e => $"0x{e:X4}");
        LogInfo($"Sample nametable entries: {string.Join(", ", sampleEntries)}");

        var bitmap = new WriteableBitmap(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        bitmap.Lock();

        unsafe
        {
            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            for (int ty = 0; ty < capture.NametableHeight; ty++)
            {
                for (int tx = 0; tx < capture.NametableWidth; tx++)
                {
                    int nametableIdx = ty * capture.NametableWidth + tx;
                    if (nametableIdx >= capture.Nametable.Length)
                        continue;

                    ushort nametableEntry = capture.Nametable[nametableIdx];

                    ushort tileIdx;
                    bool hFlip = false;
                    bool vFlip = false;
                    byte paletteIdx = 0;

                    if (isNes && nesAttributeTable != null)
                    {
                        // NES: Nametable entry is just tile index
                        tileIdx = nametableEntry;

                        // Get palette from attribute table
                        // Each attribute byte controls 4x4 tiles (2 bits per 2x2 tile group)
                        int attrX = tx / 4;
                        int attrY = ty / 4;
                        int attrIdx = attrY * 8 + attrX; // 8 attribute bytes per row (32 tiles / 4)

                        if (attrIdx < nesAttributeTable.Length)
                        {
                            byte attrByte = nesAttributeTable[attrIdx];

                            // Determine which 2x2 quadrant within the 4x4 block
                            int quadX = (tx % 4) / 2; // 0 or 1
                            int quadY = (ty % 4) / 2; // 0 or 1
                            int quadrant = quadY * 2 + quadX; // 0=TL, 1=TR, 2=BL, 3=BR

                            // Extract 2 bits for this quadrant
                            paletteIdx = (byte)((attrByte >> (quadrant * 2)) & 0x03);
                        }
                    }
                    else
                    {
                        // SMS/GG: Extract tile attributes from nametable entry
                        tileIdx = (ushort)(nametableEntry & 0x1FF); // Bits 0-8: tile index
                        hFlip = (nametableEntry & 0x200) != 0;        // Bit 9: horizontal flip
                        vFlip = (nametableEntry & 0x400) != 0;        // Bit 10: vertical flip
                        paletteIdx = (byte)((nametableEntry >> 11) & 0x01); // Bit 11: palette select
                    }

                    if (tileIdx >= capture.Tiles.Length)
                        continue;

                    var tile = capture.Tiles[tileIdx];

                    for (int py = 0; py < tileSize; py++)
                    {
                        for (int px = 0; px < tileSize; px++)
                        {
                            // Apply flip transformations
                            int srcPx = hFlip ? (tileSize - 1 - px) : px;
                            int srcPy = vFlip ? (tileSize - 1 - py) : py;
                            int pixelIdx = srcPy * tileSize + srcPx;

                            if (pixelIdx >= tile.Length)
                                continue;

                            byte colorIdx = tile[pixelIdx];

                            // Apply palette offset
                            int finalColorIdx = colorIdx;
                            if (isNes)
                            {
                                // NES: 4 palettes of 4 colors each (indices 0-3 per palette)
                                // Palette 0 at indices 0-3, Palette 1 at 4-7, etc.
                                finalColorIdx = (paletteIdx * 4) + colorIdx;
                            }
                            else if (hasMultiplePalettes && paletteIdx > 0)
                            {
                                // SMS/GG: 2 palettes of 16 colors each
                                finalColorIdx = colorIdx + (paletteIdx * colorsPerPalette);
                            }

                            if (finalColorIdx >= capture.Palette.Length)
                                continue;

                            uint color = capture.Palette[finalColorIdx];

                            int x = tx * tileSize + px;
                            int y = ty * tileSize + py;
                            int offset = y * stride + x * 4;

                            ptr[offset + 0] = (byte)((color >> 0) & 0xFF);  // B
                            ptr[offset + 1] = (byte)((color >> 8) & 0xFF);  // G
                            ptr[offset + 2] = (byte)((color >> 16) & 0xFF); // R
                            ptr[offset + 3] = (byte)((color >> 24) & 0xFF); // A
                        }
                    }
                }
            }
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        bitmap.Unlock();
        bitmap.Freeze();

        return bitmap;
    }

    private void RenderTilesInGrid(CaptureResult capture)
    {
        int tilesPerRow = 16;
        int tileSize = 8;
        int rows = (capture.Tiles.Length + tilesPerRow - 1) / tilesPerRow;

        int width = tilesPerRow * tileSize;
        int height = rows * tileSize;

        var bitmap = new WriteableBitmap(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        bitmap.Lock();

        unsafe
        {
            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            // For NES sprite tiles without nametable, cycle through sprite palettes (4-7)
            bool isNesSprites = _sourceConsole == "nes" && capture.Palette != null && capture.Palette.Length == 32;
            int nesPaletteOffset = isNesSprites ? 16 : 0; // Sprite palettes start at index 16

            for (int tileIdx = 0; tileIdx < capture.Tiles.Length; tileIdx++)
            {
                var tile = capture.Tiles[tileIdx];
                int tileX = (tileIdx % tilesPerRow) * tileSize;
                int tileY = (tileIdx / tilesPerRow) * tileSize;

                // Cycle through palettes for variety
                int paletteNum = (tileIdx / tilesPerRow) % 4; // Change palette every row

                for (int py = 0; py < tileSize; py++)
                {
                    for (int px = 0; px < tileSize; px++)
                    {
                        int pixelIdx = py * tileSize + px;
                        if (pixelIdx >= tile.Length)
                            continue;

                        byte colorIdx = tile[pixelIdx];

                        uint color;
                        if (capture.Palette != null)
                        {
                            int finalColorIdx = colorIdx;

                            if (isNesSprites)
                            {
                                // NES sprites: use sprite palettes (4-7)
                                finalColorIdx = nesPaletteOffset + (paletteNum * 4) + colorIdx;
                            }

                            if (finalColorIdx < capture.Palette.Length)
                            {
                                color = capture.Palette[finalColorIdx];
                            }
                            else
                            {
                                // Fallback to grayscale
                                byte gray = (byte)(colorIdx * 85);
                                color = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                            }
                        }
                        else
                        {
                            // Fallback to grayscale
                            byte gray = (byte)(colorIdx * 85);
                            color = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                        }

                        int x = tileX + px;
                        int y = tileY + py;
                        int offset = y * stride + x * 4;

                        ptr[offset + 0] = (byte)((color >> 0) & 0xFF);  // B
                        ptr[offset + 1] = (byte)((color >> 8) & 0xFF);  // G
                        ptr[offset + 2] = (byte)((color >> 16) & 0xFF); // R
                        ptr[offset + 3] = (byte)((color >> 24) & 0xFF); // A
                    }
                }
            }
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        bitmap.Unlock();
        bitmap.Freeze();

        ImgPreview.Source = bitmap;
    }
}
