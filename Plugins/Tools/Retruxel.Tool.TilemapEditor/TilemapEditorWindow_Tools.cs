using Retruxel.Core.Models;
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

            var imagePath = Path.Combine(_projectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"Image file not found: {imagePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var input = new Dictionary<string, object>
            {
                ["imagePath"] = imagePath,
                ["tileWidth"] = _target.Specs.TileWidth,
                ["tileHeight"] = _target.Specs.TileHeight,
                ["enableFlipH"] = true,
                ["enableFlipV"] = true,
                ["enableRotation"] = false
            };

            var result = tilePackerTool.Execute(input);

            var originalCount = Convert.ToInt32(result["originalTileCount"]);
            var optimizedCount = Convert.ToInt32(result["optimizedTileCount"]);
            var compressionRatio = Convert.ToDouble(result["compressionRatio"]);
            var savedTiles = originalCount - optimizedCount;
            var savedPercent = (1.0 - compressionRatio) * 100;

            var message = $"Optimization complete!\n\n" +
                         $"Original tiles: {originalCount}\n" +
                         $"Optimized tiles: {optimizedCount}\n" +
                         $"Saved: {savedTiles} tiles ({savedPercent:F1}%)\n\n" +
                         $"Apply optimization to current tilemap?";

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
            var tilemapObj = optimizationResult["tilemap"];
            var uniqueTilesObj = optimizationResult["uniqueTiles"];

            if (tilemapObj == null || uniqueTilesObj == null)
            {
                MessageBox.Show("Optimization result is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // TilePacker now returns List<TileEntry> directly
            if (tilemapObj is not List<TileEntry> tilemap)
            {
                MessageBox.Show("Invalid tilemap format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Build index mapping from old tileset to optimized tileset
            var indexMapping = new Dictionary<int, int>();
            int tilesPerRow = originalAsset.SourceWidth / _target.Specs.TileWidth;

            foreach (var entry in tilemap)
            {
                int oldTileIndex = entry.Y * tilesPerRow + entry.X;
                indexMapping[oldTileIndex] = entry.TileIndex;
            }

            // Apply optimization to current layer
            var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
            int remappedCount = 0;

            for (int i = 0; i < currentLayer.Length && i < tilemap.Count; i++)
            {
                var entry = currentLayer[i];
                if (!entry.IsEmpty && indexMapping.ContainsKey(entry.TileIndex))
                {
                    // Remap tile index and apply flip flags from TilePacker
                    entry.TileIndex = indexMapping[entry.TileIndex];
                    entry.FlipH = tilemap[i].FlipH;
                    entry.FlipV = tilemap[i].FlipV;
                    entry.Rotation = tilemap[i].Rotation;
                    currentLayer[i] = entry;
                    remappedCount++;
                }
            }

            var optimizedAssetId = await CreateOptimizedTileset(uniqueTilesObj, originalAsset);
            if (optimizedAssetId == null)
            {
                MessageBox.Show("Failed to create optimized tileset.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadAssets();

            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == optimizedAssetId)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }

            RenderCanvas();

            MessageBox.Show($"Optimization applied successfully!\n\n" +
                          $"Tiles remapped: {remappedCount}\n" +
                          $"New tileset: {optimizedAssetId}\n\n" +
                          $"Remember to SAVE the tilemap to persist changes.",
                          "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply optimization: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string?> CreateOptimizedTileset(object uniqueTilesObj, AssetEntry originalAsset)
    {
        try
        {
            if (uniqueTilesObj is not System.Collections.IEnumerable enumerable)
                return null;

            var uniqueTilesList = new List<byte[]>();
            foreach (var tile in enumerable)
            {
                if (tile is byte[] tileData)
                    uniqueTilesList.Add(tileData);
            }

            if (uniqueTilesList.Count == 0) return null;

            int tileSize = _target.Specs.TileWidth;
            int tilesPerRow = 16;
            int rows = (int)Math.Ceiling(uniqueTilesList.Count / (double)tilesPerRow);
            int imageWidth = tilesPerRow * tileSize;
            int imageHeight = rows * tileSize;

            var bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgra32, null);
            bitmap.Lock();

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bitmap.BackBuffer;
                    int stride = bitmap.BackBufferStride;

                    for (int tileIdx = 0; tileIdx < uniqueTilesList.Count; tileIdx++)
                    {
                        var tileData = uniqueTilesList[tileIdx];
                        int tileX = (tileIdx % tilesPerRow) * tileSize;
                        int tileY = (tileIdx / tilesPerRow) * tileSize;

                        for (int py = 0; py < tileSize; py++)
                        {
                            for (int px = 0; px < tileSize; px++)
                            {
                                int srcIdx = (py * tileSize + px) * 4;
                                int dstX = tileX + px;
                                int dstY = tileY + py;
                                int dstIdx = dstY * stride + dstX * 4;

                                ptr[dstIdx] = tileData[srcIdx + 2];
                                ptr[dstIdx + 1] = tileData[srcIdx + 1];
                                ptr[dstIdx + 2] = tileData[srcIdx];
                                ptr[dstIdx + 3] = tileData[srcIdx + 3];
                            }
                        }
                    }
                }

                bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, imageWidth, imageHeight));
            }
            finally
            {
                bitmap.Unlock();
            }

            var assetsDir = Path.Combine(_projectPath, "assets");
            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);

            var optimizedFileName = $"{originalAsset.Id}_optimized.png";
            var optimizedPath = Path.Combine(assetsDir, optimizedFileName);

            using (var fileStream = new FileStream(optimizedPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }

            var optimizedAssetId = $"{originalAsset.Id}_optimized";
            var newAsset = new AssetEntry
            {
                Id = optimizedAssetId,
                FileName = optimizedFileName,
                RelativePath = $"assets/{optimizedFileName}",
                VramRegionId = originalAsset.VramRegionId,
                SourceWidth = imageWidth,
                SourceHeight = imageHeight,
                TileCount = uniqueTilesList.Count
            };

            if (!_project.Assets.Any(a => a.Id == optimizedAssetId))
            {
                _project.Assets.Add(newAsset);
                if (_saveProjectCallback != null)
                    await _saveProjectCallback.Invoke();
            }

            LoadAssets();

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
            FileName = "tilemap.png"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                int width = int.Parse(TxtWidth.Text);
                int height = int.Parse(TxtHeight.Text);
                int tileSize = _target.Specs.TileWidth;

                var bitmap = new RenderTargetBitmap(
                    width * tileSize,
                    height * tileSize,
                    96, 96,
                    PixelFormats.Pbgra32);

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
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
                                    var tileImage = _tilesetRenderer.ExtractTile(entry);
                                    if (tileImage != null)
                                        context.DrawImage(tileImage, new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
                                }
                            }
                        }
                    }
                }

                bitmap.Render(visual);

                using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);

                MessageBox.Show($"Tilemap exported to {Path.GetFileName(dialog.FileName)}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
