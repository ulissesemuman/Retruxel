using Retruxel.Core.Models;
using Retruxel.Lib.PaletteHelpers;
using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.TilePacker;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void BtnOptimize_Click(object sender, RoutedEventArgs e)
    {
        if (_tilesetRenderer.Image == null || CmbTilesetAsset.SelectedItem == null)
        {
            MessageBox.Show("Please select a tileset asset first.", "No Tileset", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var assetId = CmbTilesetAsset.SelectedItem.ToString()!;
            var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null) return;

            if (asset.GenerationParams?.MapIndex == null || asset.GenerationParams.MapIndex.Length == 0)
            {
                MessageBox.Show($"Asset '{assetId}' has no MapIndex. Re-import the asset to generate it.",
                    "Missing MapIndex", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int imageWidth  = asset.GenerationParams.OptimizedWidth  > 0 ? asset.GenerationParams.OptimizedWidth  : asset.SourceWidth;
            int imageHeight = asset.GenerationParams.OptimizedHeight > 0 ? asset.GenerationParams.OptimizedHeight : asset.SourceHeight;

            var palette = ResolvePaletteForPreview();
            var dialog = new TilePackerWindow(
                asset.GenerationParams.MapIndex,
                imageWidth,
                imageHeight,
                _target.Specs.TileWidth,
                _target.Specs.TileHeight,
                palette,
                assetLabel: $"{asset.Id} — {_target.Specs.TileWidth}×{_target.Specs.TileHeight}px tiles")
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true || dialog.Result is not TilePackResult packResult)
                return;

            ApplyOptimization(new Dictionary<string, object> { ["result"] = packResult }, asset);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Optimization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private IReadOnlyList<HardwareColor> ResolvePaletteForPreview()
    {
        if (_currentScene != null && _currentScene.PaletteSlots.Count > 0)
        {
            var slotIndex = _selectedPaletteSlot;
            if (slotIndex >= _currentScene.PaletteSlots.Count)
                slotIndex = 0;

            return PaletteHelpers.ResolvePaletteColors(_currentScene.PaletteSlots[slotIndex], _target);
        }

        return _target.GetHardwarePalette();
    }

    private async void ApplyOptimization(Dictionary<string, object> optimizationResult, AssetEntry originalAsset)
    {
        try
        {
            if (optimizationResult["result"] is not TilePackResult packResult)
            {
                MessageBox.Show("Optimization result is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var plane = packResult.Plane;
            var uniqueTiles = packResult.UniqueTiles;

            // Build index mapping
            var indexMapping = new Dictionary<int, int>();
            int tilesPerRow = originalAsset.SourceWidth / _target.Specs.TileWidth;

            foreach (var entry in plane)
            {
                int oldTileIndex = entry.Y * tilesPerRow + entry.X;
                indexMapping[oldTileIndex] = entry.TileIndex;
            }

            // Apply optimization to current layer using sparse SetTile.
            int remappedCount = 0;
            var sparseLayer = _planeData.GetLayerSparse(_currentLayerIndex).ToList();

            foreach (var (x, y, existingEntry) in sparseLayer)
            {
                if (!existingEntry.IsEmpty && indexMapping.ContainsKey(existingEntry.TileIndex))
                {
                    // Find the corresponding plane entry by flat index.
                    int flatIndex = y * _planeData.Width + x;
                    if (flatIndex < plane.Count)
                    {
                        var remapped = existingEntry.Clone();
                        remapped.TileIndex = indexMapping[existingEntry.TileIndex];
                        remapped.FlipH     = plane[flatIndex].FlipH;
                        remapped.FlipV     = plane[flatIndex].FlipV;
                        remapped.Rotation  = plane[flatIndex].Rotation;
                        _planeData.SetTile(_currentLayerIndex, x, y, remapped);
                        remappedCount++;
                    }
                }
            }

            await UpdateAssetWithOptimization(uniqueTiles, originalAsset);

            LoadAssets();

            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == originalAsset.Id)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }

            RenderCanvas();

            MessageBox.Show($"Optimization applied successfully!\n\n" +
                          $"Tiles remapped: {remappedCount}\n\n" +
                          $"Remember to SAVE the plane to persist changes.",
                          "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply optimization: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task UpdateAssetWithOptimization(List<byte[]> uniqueTiles, AssetEntry asset)
    {
        int tileWidth  = _target.Specs.TileWidth;
        int tileHeight = _target.Specs.TileHeight;

        // Single-column layout: width = tileWidth, height = uniqueTiles.Count * tileHeight.
        // This guarantees TileCount == (OptimizedWidth/tileWidth)*(OptimizedHeight/tileHeight)
        // with no padding gaps at the end — every byte in MapIndex belongs to a valid tile.
        int imageWidth  = tileWidth;
        int imageHeight = uniqueTiles.Count * tileHeight;

        var mapIndex = new byte[imageWidth * imageHeight];

        for (int tileIdx = 0; tileIdx < uniqueTiles.Count; tileIdx++)
        {
            var tileData = uniqueTiles[tileIdx];
            int tileY = tileIdx * tileHeight;   // one tile per row

            for (int py = 0; py < tileHeight; py++)
                for (int px = 0; px < tileWidth; px++)
                    mapIndex[(tileY + py) * imageWidth + px] = tileData[py * tileWidth + px];
        }

        asset.GenerationParams!.MapIndex = mapIndex;
        asset.GenerationParams.TileCount = uniqueTiles.Count;
        asset.GenerationParams.OptimizedWidth  = imageWidth;
        asset.GenerationParams.OptimizedHeight = imageHeight;

        if (_saveProjectCallback != null)
            await _saveProjectCallback.Invoke();
    }

    private async Task<string?> CreateOptimizedTileset(List<byte[]> uniqueTiles, AssetEntry originalAsset)
    {
        try
        {
            if (uniqueTiles.Count == 0) return null;

            int tileWidth  = _target.Specs.TileWidth;
            int tileHeight = _target.Specs.TileHeight;

            // Single-column layout — no padding gaps.
            int imageWidth  = tileWidth;
            int imageHeight = uniqueTiles.Count * tileHeight;

            var mapIndex = new byte[imageWidth * imageHeight];

            for (int tileIdx = 0; tileIdx < uniqueTiles.Count; tileIdx++)
            {
                var tileData = uniqueTiles[tileIdx];
                int tileY = tileIdx * tileHeight;

                for (int py = 0; py < tileHeight; py++)
                    for (int px = 0; px < tileWidth; px++)
                        mapIndex[(tileY + py) * imageWidth + px] = tileData[py * tileWidth + px];
            }

            var optimizedAssetId = $"{originalAsset.Id}_optimized";

            var newAsset = new AssetEntry
            {
                Id = optimizedAssetId,
                FileName = $"{optimizedAssetId}.png",
                RelativePath = originalAsset.RelativePath,
                SourcePath = originalAsset.SourcePath,
                SourceWidth = imageWidth,
                SourceHeight = imageHeight,
                GenerationParams = originalAsset.GenerationParams
            };

            if (!_project.Assets.Any(a => a.Id == optimizedAssetId))
            {
                _project.Assets.Add(newAsset);
                if (_saveProjectCallback != null)
                    await _saveProjectCallback.Invoke();
            }

            return optimizedAssetId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateOptimizedTileset] Error: {ex.Message}");
            return null;
        }
    }

    private void BtnExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_tilesetRenderer.Image == null)
        {
            MessageBox.Show("No tileset loaded.", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            FileName = "plane.png"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                int width  = _planeData.Width;
                int height = _planeData.Height;

                // Fall back to viewport size when the plane is empty.
                if (width  == 0) width  = _planeSpecs.DefaultWidth;
                if (height == 0) height = _planeSpecs.DefaultHeight;

                int tileSize = _target.Specs.TileWidth;

                var bitmap = new SKBitmap(width * tileSize, height * tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    foreach (var (tx, ty, entry) in _planeData.GetLayerSparse(_currentLayerIndex))
                    {
                        if (tx >= width || ty >= height) continue;
                        if (entry.IsEmpty) continue;

                        using var tileImage = _tilesetRenderer.ExtractSkTile(entry.TileIndex);
                        if (tileImage != null)
                            canvas.DrawBitmap(tileImage, tx * tileSize, ty * tileSize);
                    }
                }

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                data.SaveTo(stream);

                MessageBox.Show($"Plane exported to {Path.GetFileName(dialog.FileName)}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
