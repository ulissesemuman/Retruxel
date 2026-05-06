using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void InitializeAssetSelector()
    {
        if (_project == null)
        {
            System.Diagnostics.Debug.WriteLine("[SpriteEditor] InitializeAssetSelector: _project is null!");
            return;
        }

        CmbTilesetAsset.Items.Clear();

        // Add all image assets from project
        var imageAssets = _project.Assets
            .OrderBy(a => a.Id);

        foreach (var asset in imageAssets)
        {
            var item = new ComboBoxItem
            {
                Content = asset.Id,
                Tag = asset
            };
            CmbTilesetAsset.Items.Add(item);
        }

        // Select current asset if available
        if (!string.IsNullOrEmpty(_tilesetAssetId))
        {
            var currentAsset = _project.Assets.FirstOrDefault(a => a.Id == _tilesetAssetId);
            if (currentAsset != null)
            {
                foreach (ComboBoxItem item in CmbTilesetAsset.Items)
                {
                    if (item.Tag is AssetEntry asset && asset.Id == _tilesetAssetId)
                    {
                        CmbTilesetAsset.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        CmbTilesetAsset.SelectionChanged += CmbTilesetAsset_SelectionChanged;
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem is not ComboBoxItem item) return;
        if (item.Tag is not AssetEntry asset) return;
        if (_isInitializing) return;

        _tilesetAssetId = asset.Id;
        LoadTilesetFromAsset(asset);
        SaveAssetSelection();
    }

    private void LoadTilesetFromAsset(AssetEntry asset)
    {
        var imagePath = System.IO.Path.Combine(_projectPath, "Assets", asset.RelativePath);
        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Loading tileset from: {imagePath}");

        if (!System.IO.File.Exists(imagePath))
        {
            MessageBox.Show($"Tileset image not found:\n{imagePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (asset.IsIndexed && _currentScene != null)
        {
            _indexedData = _indexedPngService.Read(imagePath);
            _tilesetColumns = _indexedData.Width / 8;
            _tilesetRows = _indexedData.Height / 8;
            _totalTiles = _tilesetColumns * _tilesetRows;
            RefreshTilesetWithPalette();
        }
        else
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(imagePath));
            _tilesetImage = bitmap;
            _tilesetColumns = bitmap.PixelWidth / 8;
            _tilesetRows = bitmap.PixelHeight / 8;
            _totalTiles = _tilesetColumns * _tilesetRows;
            RenderTileset();
        }

        UpdateVramInfo();
    }

    private void SaveAssetSelection()
    {
        if (ModuleData == null)
            ModuleData = new Dictionary<string, object>();

        ModuleData["tilesetAssetId"] = _tilesetAssetId ?? "";
    }

    private void BtnBrowseTileset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var assetImporter = new Retruxel.Tool.AssetImporter.AssetImporterWindow(_target!, _projectPath!, _currentScene)
            {
                Owner = this
            };
            assetImporter.PreSelectRegion("sprites");

            if (assetImporter.ShowDialog() == true && assetImporter.ImportedAsset != null)
            {
                _project!.Assets.Add(assetImporter.ImportedAsset);
                InitializeAssetSelector();

                var newAsset = assetImporter.ImportedAsset;
                foreach (ComboBoxItem item in CmbTilesetAsset.Items)
                {
                    if (item.Tag is AssetEntry asset && asset.Id == newAsset.Id)
                    {
                        CmbTilesetAsset.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Failed to import asset:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
