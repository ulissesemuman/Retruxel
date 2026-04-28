using Retruxel.Core.Models;
using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void BtnImportAsset_Click(object sender, RoutedEventArgs e)
    {
        var assetImporter = new Tool.AssetImporter.AssetImporterWindow(_target, _projectPath)
        {
            Owner = this
        };

        assetImporter.PreSelectRegion("background");

        if (assetImporter.ShowDialog() == true && assetImporter.ImportedAsset is not null)
        {
            var asset = assetImporter.ImportedAsset;

            if (_project.Assets.Any(a => a.Id == asset.Id))
            {
                MessageBox.Show($"An asset named '{asset.Id}' already exists.", "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _project.Assets.Add(asset);

            var result = MessageBox.Show(
                $"Asset '{asset.Id}' imported successfully.\n\nDo you want to create a palette from this asset's colors?",
                "Create Palette",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                OpenPaletteEditorForAsset(asset);

            LoadAssets();

            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == asset.Id)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private async void BtnImportFromLiveLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show("Tool registry not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var liveLinkTool = _toolRegistry.GetVisualTool("livelink");
            if (liveLinkTool == null)
            {
                MessageBox.Show("LiveLink tool not found. Make sure the plugin is installed.", "Tool Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var liveLinkInput = new Dictionary<string, object>
            {
                ["mode"] = "capture",
                ["targetId"] = _target.TargetId,
                ["callerId"] = "tilemap_editor"
            };

            var liveLinkWindow = (Window)liveLinkTool.CreateWindow(liveLinkInput);
            liveLinkWindow.Owner = this;

            if (liveLinkWindow.ShowDialog() == true)
            {
                var moduleDataProp = liveLinkWindow.GetType().GetProperty("ModuleData");
                var moduleData = moduleDataProp?.GetValue(liveLinkWindow) as Dictionary<string, object>;

                if (moduleData == null || !moduleData.ContainsKey("importedAssetData"))
                {
                    MessageBox.Show("No data received from LiveLink.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var importedData = (ImportedAssetData)moduleData["importedAssetData"];

                var pipeline = new Pipelines.ImportedAssetToTilemapPipeline();
                var pipelineOptions = new Dictionary<string, object>
                {
                    ["project"] = _project,
                    ["projectPath"] = _projectPath,
                    ["target"] = _target
                };

                var tilemapData = pipeline.ProcessTyped(importedData, pipelineOptions);

                if (_saveProjectCallback != null)
                    await _saveProjectCallback.Invoke();

                LoadAssets();

                var assetId = tilemapData["tilesAssetId"].ToString()!;
                for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
                {
                    if (CmbTilesetAsset.Items[i].ToString() == assetId)
                    {
                        CmbTilesetAsset.SelectedIndex = i;
                        break;
                    }
                }

                int width = (int)tilemapData["mapWidth"];
                int height = (int)tilemapData["mapHeight"];

                if (width == 0 || height == 0)
                {
                    var specs = _target.Specs.Tilemap;
                    width = specs.DefaultWidth * 2;
                    height = specs.DefaultHeight * 2;
                }

                TxtWidth.Text = width.ToString();
                TxtHeight.Text = height.ToString();

                _tilemapData.Resize(width, height);

                var mapData = (int[])tilemapData["mapData"];
                if (mapData.Length > 0)
                {
                    var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
                    Array.Copy(mapData, currentLayer, Math.Min(mapData.Length, currentLayer.Length));
                }

                RenderCanvas();

                var palette = (uint[])tilemapData["palette"];
                if (palette.Length > 0)
                {
                    var result = MessageBox.Show(
                        $"LiveLink captured {palette.Length} colors.\n\nDo you want to create a palette from these colors?",
                        "Create Palette",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var asset = (AssetEntry)tilemapData["asset"];
                        OpenPaletteEditorForAsset(asset);
                    }
                }

                MessageBox.Show(
                    $"Successfully imported tilemap from LiveLink!\n\n" +
                    $"Tiles: {importedData.Tiles.Length}\n" +
                    $"Map: {width}x{height}\n" +
                    $"Palette: {palette.Length} colors",
                    "Import Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import from LiveLink: {ex.Message}\n\n{ex.StackTrace}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
