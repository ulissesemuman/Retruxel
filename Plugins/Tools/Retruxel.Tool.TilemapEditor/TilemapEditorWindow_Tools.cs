using Retruxel.Core.Models;
using Retruxel.Lib.WPFImageProcessing;
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

        if (_toolRegistry == null)
        {
            MessageBox.Show("Tool registry not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var tilePackerTool = _toolRegistry.GetTool("retruxel.tool.tilepacker");
        if (tilePackerTool == null)
        {
            MessageBox.Show("TilePacker tool not found.", "Tool Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var input = new Dictionary<string, object>
            {
                ["indexMap"] = _currentAsset!.GenerationParams!.MapIndex,
                ["imageWidth"] = _currentAsset.SourceWidth,
                ["imageHeight"] = _currentAsset.SourceHeight,
                ["tileWidth"] = _target.Specs.TileWidth,
                ["tileHeight"] = _target.Specs.TileHeight,
                ["enableFlipH"] = true,
                ["enableFlipV"] = true,
                ["enableRotation"]= false
            };

            var result = tilePackerTool.Execute(input);

            TilePackResult tilePackResult = result["result"] as TilePackResult;

            var originalCount = tilePackResult.OriginalTileCount;
            var optimizedCount = tilePackResult.OptimizedTileCount;
            var compressionRatio = tilePackResult.CompressionRatio;
            var savedTiles = originalCount - optimizedCount;
            var savedPercent = (1.0 - compressionRatio) * 100;

            var message = $"Optimization complete!\n\n" +
                         $"Original tiles: {originalCount}\n" +
                         $"Optimized tiles: {optimizedCount}\n" +
                         $"Saved: {savedTiles} tiles ({savedPercent:F1}%)\n\n" +
                         $"Apply optimization to current plane?";

            var dialogResult = MessageBox.Show(message, "Tile Optimization", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dialogResult == MessageBoxResult.Yes)
                ApplyOptimization(result, asset);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Optimization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

            // Apply optimization to current layer
            var currentLayer = _planeData.GetLayer(_currentLayerIndex);
            int remappedCount = 0;

            for (int i = 0; i < currentLayer.Length && i < plane.Count; i++)
            {
                var entry = currentLayer[i];
                if (!entry.IsEmpty && indexMapping.ContainsKey(entry.TileIndex))
                {
                    entry.TileIndex = indexMapping[entry.TileIndex];
                    entry.FlipH = plane[i].FlipH;
                    entry.FlipV = plane[i].FlipV;
                    entry.Rotation = plane[i].Rotation;
                    currentLayer[i] = entry;
                    remappedCount++;
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
                int width = int.Parse(TxtWidth.Text);
                int height = int.Parse(TxtHeight.Text);
                int tileSize = _target.Specs.TileWidth;

                var bitmap = new SKBitmap(width * tileSize, height * tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    var currentLayer = _planeData.GetLayer(_currentLayerIndex);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            if (index < currentLayer.Length)
                            {
                                var entry = currentLayer[index];
                                if (!entry.IsEmpty)
                                {
                                    using var tileImage = _tilesetRenderer.ExtractSkTile(entry.TileIndex);
                                    if (tileImage != null)
                                        canvas.DrawBitmap(tileImage, x * tileSize, y * tileSize);
                                }
                            }
                        }
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
