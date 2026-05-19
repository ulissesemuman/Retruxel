using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.PaletteHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private AssetEntry? _currentAsset;

    private void InitializeAssetSelector()
    {
        if (_project == null)
        {
            System.Diagnostics.Debug.WriteLine("[SpriteEditor] InitializeAssetSelector: _project is null!");
            return;
        }

        CmbTilesetAsset.Items.Clear();

        foreach (var asset in _project.Assets.OrderBy(a => a.Id))
        {
            CmbTilesetAsset.Items.Add(new ComboBoxItem
            {
                Content = asset.Id,
                Tag     = asset
            });
        }

        // Restore previously selected asset
        if (!string.IsNullOrEmpty(_tilesetAssetId))
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
        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] LoadTilesetFromAsset: {asset.Id}");

        var gp = asset.GenerationParams;

        if (gp?.MapIndex == null || gp.MapIndex.Length == 0)
        {
            MessageBox.Show(
                $"Asset '{asset.Id}' has no MapIndex.\nRe-import the asset to generate it.",
                "Missing MapIndex", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _currentAsset    = asset;
        _tilesetColumns  = gp.OptimizedWidth  / _target!.Specs.TileWidth;
        _tilesetRows     = gp.OptimizedHeight / _target.Specs.TileHeight;
        _totalTiles      = gp.TileCount;

        // Build a lightweight IndexedPngData from the stored MapIndex + Palette
        var colors = gp.Palette?.Count > 0
            ? gp.Palette
            : asset.GenerationParams.Palette ?? new List<string>();

        _indexedData = new IndexedPngData
        {
            Width   = gp.OptimizedWidth,
            Height  = gp.OptimizedHeight,
            Indices = gp.MapIndex,
            Colors  = colors
        };

        RefreshTilesetWithPalette();
        UpdateVramInfo();
    }

    private void SaveAssetSelection()
    {
        ModuleData ??= new Dictionary<string, object>();
        ModuleData["tilesetAssetId"] = _tilesetAssetId ?? "";
    }

    private void BtnBrowseTileset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var assetImporter = new Tool.AssetImporter.AssetImporterWindow(
                _target!, _projectPath!, _currentScene)
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
            MessageBox.Show($"Failed to import asset:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
