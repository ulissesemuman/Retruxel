using Retruxel.Core.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void LoadAssets()
    {
        CmbTilesetAsset.Items.Clear();

        var bgAssets = _project.Assets
            .Where(a => a.VramRegionId == "bg" || a.VramRegionId == "background")
            .ToList();

        if (bgAssets.Count == 0)
        {
            TxtVramRegionInfo.Text = "No background assets found. Click IMPORT ASSET to add one.";
            return;
        }

        foreach (var asset in bgAssets)
            CmbTilesetAsset.Items.Add(asset.Id);

        TxtTilesetInfo.Text = "Select a tileset asset to begin";
        TxtVramRegionInfo.Text = "No asset selected";
    }

    private void LoadTilesetImage(AssetEntry asset)
    {
        try
        {
            var absPath = Path.Combine(_projectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absPath))
            {
                MessageBox.Show($"Tileset image not found: {absPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tilesetRenderer.LoadTileset(absPath, _target.Specs.TileWidth);
            
            // Auto-calculate columns for import
            int imageWidth = asset.SourceWidth;
            int tileSize = _target.Specs.TileWidth;
            int calculatedColumns = imageWidth / tileSize;
            TxtImportColumns.Text = calculatedColumns.ToString();
            
            PopulateTilesetGrid();
            RenderCanvas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tileset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopulateTilesetGrid()
    {
        if (_tilesetRenderer.Image == null) return;

        TilesetGrid.Items.Clear();

        int tileSize = _target.Specs.TileWidth;
        
        // Use asset.TileCount instead of _tilesetRenderer.TotalTiles to avoid showing padding tiles
        string? assetId = CmbTilesetAsset.SelectedItem?.ToString();
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
        int totalTiles = asset?.TileCount ?? _tilesetRenderer.TotalTiles;
        
        int scaledSize = (int)(tileSize * _tileZoomLevel);

        for (int tileId = 0; tileId < totalTiles; tileId++)
        {
            var border = new Border
            {
                Width = scaledSize + 2,
                Height = scaledSize + 2,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = tileId
            };

            var tileImage = _tilesetRenderer.ExtractTile(tileId);

            var image = new Image
            {
                Width = scaledSize,
                Height = scaledSize,
                Source = tileImage,
                Stretch = Stretch.Fill
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            border.Child = image;
            border.MouseLeftButtonDown += TileButton_MouseDown;
            border.MouseMove += TileButton_MouseMove;
            border.MouseLeftButtonUp += TileButton_MouseUp;

            TilesetGrid.Items.Add(border);
        }

        UpdateTileSelectionVisual();
        UpdateSelectedTilePreview();
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null) return;

        string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);

        if (asset == null) return;

        if (_tilesetRenderer.Image != null && !_isInitializing)
        {
            int oldTileCount = _tilesetRenderer.TotalTiles;
            int newTileCount = asset.TileCount;

            if (newTileCount < oldTileCount)
            {
                var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
                int tilesAboveLimit = currentLayer.Count(t => t >= newTileCount);

                if (tilesAboveLimit > 0)
                {
                    var result = MessageBox.Show(
                        $"Warning: The new tileset '{asset.Id}' has only {newTileCount} tiles.\n\n" +
                        $"Your current tilemap uses {tilesAboveLimit} tile(s) with indices above {newTileCount - 1}.\n" +
                        $"These tiles will appear as black placeholders and will be lost if you save.\n\n" +
                        $"Do you want to continue?",
                        "Tileset Size Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        TxtTilesetInfo.Text = $"{asset.FileName} ({asset.TileCount} tiles)";

        var region = _target.Specs.VramRegions.FirstOrDefault(r => r.Id == asset.VramRegionId);
        if (region != null)
            TxtVramRegionInfo.Text = $"{region.Label}: {region.StartTile}-{region.EndTile} ({region.TileCount} tiles)";

        LoadTilesetImage(asset);
    }

    private void BtnTileZoom50_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 0.5;
        PopulateTilesetGrid();
    }

    private void BtnTileZoom100_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 1.0;
        PopulateTilesetGrid();
    }

    private void BtnTileZoom200_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 2.0;
        PopulateTilesetGrid();
    }
}
