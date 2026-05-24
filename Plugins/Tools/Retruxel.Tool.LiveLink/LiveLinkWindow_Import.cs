using Retruxel.Core.Services;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.AssetProcessor;
using Retruxel.Tool.LiveLink.Pipelines;
using Retruxel.Tool.LiveLink.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Import and conversion: bitmap to capture, palette optimization, asset export.
/// </summary>
public partial class LiveLinkWindow
{
    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCapture == null)
        {
            LogError("No capture data available");
            return;
        }

        try
        {
            // Open palette optimization preview window
            LogInfo("Opening palette optimization preview...");

            // Use the SAME bitmap that's being displayed in ImgPreview
            var previewBitmap = ImgPreview.Source as BitmapSource;
            if (previewBitmap == null)
            {
                LogError("No preview image available");
                return;
            }

            // Determine target color count based on destination target
            Retruxel.Core.Interfaces.ITarget? destinationTarget = null;
            int targetColorCount = 16; // Default

            if (_input?.TryGetValue("targetId", out var targetObj) == true)
            {
                var targetId = targetObj?.ToString();
                destinationTarget = TargetRegistry.GetTargetById(targetId ?? "sms");

                if (destinationTarget != null)
                {
                    int paletteSlotCount = destinationTarget.GetPaletteSlotCount();
                    int colorsPerSlot = destinationTarget.GetColorsPerSlot();
                    targetColorCount = paletteSlotCount * colorsPerSlot;

                    LogInfo($"Target: {destinationTarget.DisplayName} - {paletteSlotCount} slots × {colorsPerSlot} colors = {targetColorCount} total");
                }
            }

            // Convert WPF bitmap to SKBitmap
            var skiaBitmap = ImageProcessing.ConvertBitmapSourceToSkiaBitmap(previewBitmap);

            var previewWindow = new PaletteOptimizationWindow(
                skiaBitmap,
                targetColorCount,
                DistanceMode.RGB,
                destinationTarget);

            previewWindow.Owner = this;

            if (previewWindow.ShowDialog() != true)
            {
                LogInfo("Import cancelled by user");
                return;
            }

            // User confirmed - get the optimized bitmap and palette from preview
            var optimizedBitmap = previewWindow.OptimizedBitmap;
            var optimizedPalette = previewWindow.OptimizedPalette;
            double selectedDiversity = previewWindow.SelectedDiversity;

            LogInfo($"User selected diversity: {selectedDiversity:F2}");
            LogInfo($"Optimized palette: {optimizedPalette.Count} colors");

            // Check if capture has nametable (plane) or is tileset-only
            bool hasNametable = _lastCapture.Nametable != null &&
                               _lastCapture.NametableWidth > 0 &&
                               _lastCapture.NametableHeight > 0;

            LogInfo($"Capture type: {(hasNametable ? "Plane (with nametable)" : "Tileset only (no nametable)")}");

            CaptureResult optimizedCapture;

            if (hasNametable)
            {
                // Convert optimized bitmap back to tiles + nametable
                LogInfo("Converting optimized image to tiles...");

                // Extract pixels from optimized bitmap
                var optimizedPixels = IndexedBitmapRenderer.ExtractPixels(optimizedBitmap);

                // Convert to CaptureResult format
                optimizedCapture = ConvertBitmapToCapture(optimizedBitmap, optimizedPalette, _lastCapture);
            }
            else
            {
                // Tileset-only mode: keep original tiles, just update palette
                LogInfo("Tileset-only mode: using original tiles with optimized palette");

                var newPalette = optimizedPalette.Select(c =>
                    0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

                optimizedCapture = new CaptureResult
                {
                    Tiles = _lastCapture.Tiles,
                    Palette = newPalette,
                    Nametable = Array.Empty<ushort>(),
                    NametableWidth = 0,
                    NametableHeight = 0,
                    TileWidth = _lastCapture.TileWidth,
                    TileHeight = _lastCapture.TileHeight,
                    TargetId = _lastCapture.TargetId,
                    Metadata = new Dictionary<string, object>(_lastCapture.Metadata)
                };
            }

            LogInfo("Converting capture to standardized format...");

            // Use pipeline to convert CaptureResult → ImportedAssetData
            var pipeline = new CaptureToImportedAssetPipeline();
            var options = new Dictionary<string, object>
            {
                ["sourceEmulator"] = _connection?.EmulatorId ?? "unknown",
                ["destinationTarget"] = _input?.TryGetValue("targetId", out var target) == true ? target : null!
            };

            var importedData = pipeline.ProcessTyped(optimizedCapture, options);

            // If no nametable, pass the optimized bitmap to be saved directly
            if (!hasNametable)
            {
                options["optimizedBitmap"] = optimizedBitmap;
                options["originalPalette"] = _lastCapture.Palette; // Original RGB palette from emulator
                LogInfo("Passing optimized bitmap for direct PNG save (tileset-only mode)");
            }

            LogSuccess($"Converted: {importedData.GetSummary()}");

            if (!importedData.IsValid(out var errorMessage))
            {
                LogError($"Validation failed: {errorMessage}");
                return;
            }

            // Return data based on caller
            if (_captureMode && !string.IsNullOrEmpty(_callerId))
            {
                LogSuccess($"Returning imported data to {_callerId}");

                // Store options in metadata so they can be passed to the pipeline
                if (!hasNametable)
                {
                    importedData.Metadata["optimizedBitmap"] = optimizedBitmap;
                    importedData.Metadata["originalPalette"] = _lastCapture.Palette;
                    LogInfo("Stored optimized bitmap in ImportedAssetData metadata");
                }

                DialogResult = true;
                ModuleData = new Dictionary<string, object>
                {
                    ["callerId"] = _callerId,
                    ["importedAssetData"] = importedData
                };
                Close();
                return;
            }

            LogInfo("Import to project not yet implemented");
        }
        catch (Exception ex)
        {
            LogError($"Import failed: {ex.Message}");
        }
    }

    private CaptureResult ConvertBitmapToCapture(SKBitmap bitmap, List<(byte R, byte G, byte B)> palette, CaptureResult originalCapture)
    {
        // Convert palette to uint[]
        var paletteUint = palette.Select(c =>
            0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

        // Create palette lookup for fast color-to-index conversion
        var paletteLookup = new Dictionary<uint, byte>();
        for (int i = 0; i < paletteUint.Length; i++)
        {
            paletteLookup[paletteUint[i]] = (byte)i;
        }

        // Extract pixels from bitmap
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Convert pixels to tiles using original nametable structure
        int tileSize = 8;
        var tiles = new byte[originalCapture.Tiles.Length][];

        for (int tileIdx = 0; tileIdx < tiles.Length; tileIdx++)
        {
            tiles[tileIdx] = new byte[tileSize * tileSize];
        }

        // Map pixels to tiles based on nametable
        for (int ty = 0; ty < originalCapture.NametableHeight; ty++)
        {
            for (int tx = 0; tx < originalCapture.NametableWidth; tx++)
            {
                int nametableIdx = ty * originalCapture.NametableWidth + tx;
                if (nametableIdx >= originalCapture.Nametable.Length)
                    continue;

                ushort tileIdx = originalCapture.Nametable[nametableIdx];
                if (tileIdx >= tiles.Length)
                    continue;

                for (int py = 0; py < tileSize; py++)
                {
                    for (int px = 0; px < tileSize; px++)
                    {
                        int x = tx * tileSize + px;
                        int y = ty * tileSize + py;

                        if (x >= width || y >= height)
                            continue;

                        var pixel = bitmap.GetPixel(x, y);
                        uint color = 0xFF000000u | ((uint)pixel.Red << 16) | ((uint)pixel.Green << 8) | pixel.Blue;

                        byte colorIdx = paletteLookup.TryGetValue(color, out var idx) ? idx : (byte)0;

                        int tilePixelIdx = py * tileSize + px;
                        tiles[tileIdx][tilePixelIdx] = colorIdx;
                    }
                }
            }
        }

        return new CaptureResult
        {
            Tiles = tiles,
            Nametable = originalCapture.Nametable,
            NametableWidth = originalCapture.NametableWidth,
            NametableHeight = originalCapture.NametableHeight,
            Palette = paletteUint
        };
    }

    private CaptureResult ApplyOptimizedPaletteToCapture(CaptureResult originalCapture, List<(byte R, byte G, byte B)> optimizedPalette)
    {
        // Convert optimized palette to uint[]
        var newPalette = optimizedPalette.Select(c =>
            0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

        // Remap tile color indices to new palette
        var newTiles = new byte[originalCapture.Tiles.Length][];

        for (int i = 0; i < originalCapture.Tiles.Length; i++)
        {
            var oldTile = originalCapture.Tiles[i];
            var newTile = new byte[oldTile.Length];

            for (int j = 0; j < oldTile.Length; j++)
            {
                byte oldColorIdx = oldTile[j];
                if (oldColorIdx < originalCapture.Palette.Length)
                {
                    uint oldColor = originalCapture.Palette[oldColorIdx];
                    byte oldR = (byte)((oldColor >> 16) & 0xFF);
                    byte oldG = (byte)((oldColor >> 8) & 0xFF);
                    byte oldB = (byte)(oldColor & 0xFF);

                    // Find closest color in new palette
                    int closestIdx = 0;
                    double minDist = double.MaxValue;

                    for (int k = 0; k < optimizedPalette.Count; k++)
                    {
                        var newColor = optimizedPalette[k];
                        double dist = Math.Sqrt(
                            Math.Pow(oldR - newColor.R, 2) +
                            Math.Pow(oldG - newColor.G, 2) +
                            Math.Pow(oldB - newColor.B, 2));

                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestIdx = k;
                        }
                    }

                    newTile[j] = (byte)closestIdx;
                }
            }

            newTiles[i] = newTile;
        }

        // Create new capture with optimized palette
        return new CaptureResult
        {
            Tiles = newTiles,
            Palette = newPalette,
            Nametable = originalCapture.Nametable,
            NametableWidth = originalCapture.NametableWidth,
            NametableHeight = originalCapture.NametableHeight,
            TileWidth = originalCapture.TileWidth,
            TileHeight = originalCapture.TileHeight,
            TargetId = originalCapture.TargetId,
            Metadata = new Dictionary<string, object>(originalCapture.Metadata)
        };
    }
}
