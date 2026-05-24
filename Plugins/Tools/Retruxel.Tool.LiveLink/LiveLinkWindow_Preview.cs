using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.LiveLink.Services;
using SkiaSharp;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Preview rendering: plane with nametable, tileset grid fallback.
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

        // If we have nametable AND palette, render the full scene (plane)
        if (capture.Nametable != null && capture.NametableWidth > 0 && capture.NametableHeight > 0 && capture.Palette != null)
        {
            var sceneBitmap = CreatePreviewBitmap(capture);
            if (sceneBitmap != null)
            {
                ImgPreview.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(sceneBitmap);
                LogInfo("Preview: Rendered plane (nametable + tiles + palette)");
                return;
            }
        }

        // Otherwise render tiles in a grid (fallback when no nametable or no palette)
        LogInfo("Preview: Rendering tiles in grid (no nametable or no palette)");
        RenderTilesInGrid(capture);
    }


    private SKBitmap CreatePreviewBitmap(CaptureResult capture)
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

        // Create SKBitmap with Bgra8888 format
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        unsafe
        {
            byte* ptr = (byte*)bitmap.GetPixels();
            int stride = bitmap.RowBytes;

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
                        int attrX = tx / 4;
                        int attrY = ty / 4;
                        int attrIdx = attrY * 8 + attrX;

                        if (attrIdx < nesAttributeTable.Length)
                        {
                            byte attrByte = nesAttributeTable[attrIdx];
                            int quadX = (tx % 4) / 2;
                            int quadY = (ty % 4) / 2;
                            int quadrant = quadY * 2 + quadX;
                            paletteIdx = (byte)((attrByte >> (quadrant * 2)) & 0x03);
                        }
                    }
                    else
                    {
                        // SMS/GG: Extract tile attributes from nametable entry
                        tileIdx = (ushort)(nametableEntry & 0x1FF);
                        hFlip = (nametableEntry & 0x200) != 0;
                        vFlip = (nametableEntry & 0x400) != 0;
                        paletteIdx = (byte)((nametableEntry >> 11) & 0x01);
                    }

                    if (tileIdx >= capture.Tiles.Length)
                        continue;

                    var tile = capture.Tiles[tileIdx];

                    for (int py = 0; py < tileSize; py++)
                    {
                        for (int px = 0; px < tileSize; px++)
                        {
                            int srcPx = hFlip ? (tileSize - 1 - px) : px;
                            int srcPy = vFlip ? (tileSize - 1 - py) : py;
                            int pixelIdx = srcPy * tileSize + srcPx;

                            if (pixelIdx >= tile.Length)
                                continue;

                            byte colorIdx = tile[pixelIdx];

                            int finalColorIdx = colorIdx;
                            if (isNes)
                            {
                                finalColorIdx = (paletteIdx * 4) + colorIdx;
                            }
                            else if (hasMultiplePalettes && paletteIdx > 0)
                            {
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

        return bitmap;
    }

    private void RenderTilesInGrid(CaptureResult capture)
    {
        int tilesPerRow = 16;
        int tileSize = 8;
        int rows = (capture.Tiles.Length + tilesPerRow - 1) / tilesPerRow;

        int width = tilesPerRow * tileSize;
        int height = rows * tileSize;

        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // For NES sprite tiles without nametable, cycle through sprite palettes (4-7)
        bool isNesSprites = _sourceConsole == "nes" && capture.Palette != null && capture.Palette.Length == 32;
        int nesPaletteOffset = isNesSprites ? 16 : 0; // Sprite palettes start at index 16

        unsafe
        {
            byte* ptr = (byte*)bitmap.GetPixels();
            int stride = bitmap.RowBytes;

            // Process tiles in parallel
            Parallel.For(0, capture.Tiles.Length, tileIdx =>
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
            });
        }

        ImgPreview.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(bitmap);
    }
}
