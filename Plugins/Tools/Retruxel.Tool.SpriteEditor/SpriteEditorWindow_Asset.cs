using Retruxel.Core.Models;
using Retruxel.Lib.PaletteHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    // ── Asset selector ─────────────────────────────────────────────────────

    private void InitializeAssetSelector()
    {
        if (_project == null)
        {
            System.Diagnostics.Debug.WriteLine("[SpriteEditor] InitializeAssetSelector: _project is null!");
            return;
        }

        // Unsubscribe first to avoid double-registration on re-init
        CmbTilesetAsset.SelectionChanged -= CmbTilesetAsset_SelectionChanged;
        CmbTilesetAsset.Items.Clear();

        foreach (var asset in _project.Assets.OrderBy(a => a.Id))
            CmbTilesetAsset.Items.Add(asset.Id);

        CmbTilesetAsset.SelectionChanged += CmbTilesetAsset_SelectionChanged;

        // Restore previously selected asset.
        // Call LoadTilesetImage directly because _isInitializing is still true here.
        if (!string.IsNullOrEmpty(_tilesetAssetId))
        {
            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i]?.ToString() == _tilesetAssetId)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    var asset = _project.Assets.FirstOrDefault(a => a.Id == _tilesetAssetId);
                    if (asset != null) LoadTilesetImage(asset);
                    break;
                }
            }
        }
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null) return;

        string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
        var asset = _project?.Assets.FirstOrDefault(a => a.Id == assetId);
        if (asset == null) return;

        _tilesetAssetId = assetId;
        LoadTilesetImage(asset);
        SaveAssetSelection();
    }

    /// <summary>
    /// Loads a tileset asset — identical pipeline to TilemapEditor.LoadTilesetImage().
    /// </summary>
    private void LoadTilesetImage(AssetEntry asset)
    {
        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] LoadTilesetImage: {asset.Id}");

        var gp = asset.GenerationParams;

        if (gp?.MapIndex == null || gp.MapIndex.Length == 0)
        {
            MessageBox.Show(
                $"Asset '{asset.Id}' has no MapIndex.\nRe-import the asset to generate it.",
                "Missing MapIndex", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _currentAsset = asset;

        // Defer tileset rendering until after WPF layout has calculated actual sizes.
        // If called during initialization (ActualWidth == 0), the canvas has no size yet.
        if (TilesetCanvas.ActualWidth > 0)
        {
            RefreshTilesetFromAsset();
            RenderCanvas();
        }
        else
        {
            Dispatcher.InvokeAsync(() =>
            {
                RefreshTilesetFromAsset();
                RenderCanvas();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void SaveAssetSelection()
    {
        ModuleData ??= new Dictionary<string, object>();
        ModuleData["tilesetAssetId"] = _tilesetAssetId ?? "";
    }

    // ── Browse / import ────────────────────────────────────────────────────

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
                var newId = assetImporter.ImportedAsset.Id;
                InitializeAssetSelector();

                // Select the newly imported asset
                for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
                {
                    if (CmbTilesetAsset.Items[i]?.ToString() == newId)
                    {
                        CmbTilesetAsset.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import asset:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
