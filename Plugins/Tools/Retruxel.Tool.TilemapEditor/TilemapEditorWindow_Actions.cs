using Retruxel.Core.Models;
using Retruxel.Lib.TilesetHelpers;
using Retruxel.Tool.TilemapEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
 => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
 => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
 => Close();

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null)
        {
            MessageBox.Show("Please select a tileset asset.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
        {
            MessageBox.Show("Please create a palette before saving.\n\nClick on the palette dropdown and select <New Palette> to create one.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            OpenPaletteEditor();
            return;
        }

        SavePaletteSlotSelection();

        // Snapshot the current layer directly — no Base64 round-trip needed.
        var layerSnapshot = _planeData.GetLayer(_currentLayerIndex);

        // Convert TileEntry[] to a typed array for clean JSON serialization.
        var tilesArray = layerSnapshot.Select(e => new
        {
            tileIndex = e.TileIndex,
            flipH     = e.FlipH,
            flipV     = e.FlipV,
            rotation  = e.Rotation
        }).ToArray();

        ModuleData = new Dictionary<string, object>
        {
            ["moduleId"]     = "plane",
            ["mapWidth"]     = _planeData.Width,
            ["mapHeight"]    = _planeData.Height,
            ["tilesAssetId"] = CmbTilesetAsset.SelectedItem.ToString()!,
            ["paletteSlot"]  = _selectedPaletteSlot,
            ["tiles"]      = tilesArray,
            ["mapAssetId"]   = "",
            ["startTile"]    = 0,
            ["mapX"]         = _mapOffsetX,
            ["mapY"]         = _mapOffsetY,
            ["solidTiles"]   = Array.Empty<int>()
        };

        DialogResult = true;
        Close();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Clear all tiles in the current layer?",
            "Clear Map",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _planeData.ClearLayer(_currentLayerIndex);
            RenderCanvas();
        }
    }

    private void BtnFill_Click(object sender, RoutedEventArgs e)
    {
        // Create entry with current flip flags
        var entry = new TileEntry
        {
            TileIndex = _selectedTileId,
            FlipH = _selectedFlipH,
            FlipV = _selectedFlipV
        };

        _planeData.FillLayer(_currentLayerIndex, entry);
        RenderCanvas();
    }

    private void BtnImportTilesetAsMap_Click(object sender, RoutedEventArgs e)
    {
        if (_tilesetRenderer.Image == null || CmbTilesetAsset.SelectedItem == null)
        {
            MessageBox.Show("Please select a tileset asset first.", "No Tileset", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtImportColumns.Text, out int columns) || columns <= 0)
        {
            MessageBox.Show("Please enter a valid number of columns (greater than 0).", "Invalid Columns", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var assetId = CmbTilesetAsset.SelectedItem.ToString()!;
            var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null) return;

            int tileCount = asset.GenerationParams.TileCount;
            int rows = (int)Math.Ceiling((double)tileCount / columns);

            // Resize plane to match tileset dimensions
            TxtWidth.Text = columns.ToString();
            TxtHeight.Text = rows.ToString();
            _planeData.Resize(columns, rows);

            // Fill plane with tiles in order (0, 1, 2, ...) - no flip
            var currentLayer = _planeData.GetLayer(_currentLayerIndex);
            for (int i = 0; i < currentLayer.Length && i < tileCount; i++)
            {
                currentLayer[i] = new TileEntry { TileIndex = i };
            }

            RenderCanvas();

            MessageBox.Show(
                $"Tileset imported as {columns}×{rows} plane.\n\n" +
                $"Total tiles: {tileCount}\n\n" +
                $"You can now edit the plane and save it.",
                "Import Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void LoadModuleData(Dictionary<string, object> moduleData)
    {
        if (moduleData.ContainsKey("mapWidth"))
            TxtWidth.Text = moduleData["mapWidth"].ToString()!;
        else if (moduleData.ContainsKey("width"))
            TxtWidth.Text = moduleData["width"].ToString()!;

        if (moduleData.ContainsKey("mapHeight"))
            TxtHeight.Text = moduleData["mapHeight"].ToString()!;
        else if (moduleData.ContainsKey("height"))
            TxtHeight.Text = moduleData["height"].ToString()!;

        // Load palette slot
        if (moduleData.ContainsKey("paletteSlot"))
        {
            var slotObj = moduleData["paletteSlot"];
            if (slotObj is System.Text.Json.JsonElement jsonSlot)
                _selectedPaletteSlot = jsonSlot.GetInt32();
            else
                _selectedPaletteSlot = Convert.ToInt32(slotObj);

            // Validate slot index
            if (_currentScene != null && _selectedPaletteSlot >= _currentScene.PaletteSlots.Count)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Loaded palette slot {_selectedPaletteSlot} is out of range, resetting to 0");
                _selectedPaletteSlot = 0;
            }

            // Update ComboBox selection
            if (_selectedPaletteSlot < CmbPalette.Items.Count)
                CmbPalette.SelectedIndex = _selectedPaletteSlot;
        }

        // Load map offset
        if (moduleData.ContainsKey("mapX"))
        {
            var mapXObj = moduleData["mapX"];
            if (mapXObj is System.Text.Json.JsonElement jsonX)
                _mapOffsetX = jsonX.GetInt32();
            else
                _mapOffsetX = Convert.ToInt32(mapXObj);
        }

        if (moduleData.ContainsKey("mapY"))
        {
            var mapYObj = moduleData["mapY"];
            if (mapYObj is System.Text.Json.JsonElement jsonY)
                _mapOffsetY = jsonY.GetInt32();
            else
                _mapOffsetY = Convert.ToInt32(mapYObj);
        }

        UpdateMapOffset();

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);

        _planeData.Resize(width, height);

        if (moduleData.ContainsKey("tilesAssetId"))
        {
            string assetId = moduleData["tilesAssetId"].ToString()!;
            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == assetId)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }
        }

        if (moduleData.ContainsKey("tiles"))
        {
            var tilesObj = moduleData["tiles"];

            if (tilesObj is System.Text.Json.JsonElement jsonEl)
            {
                if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var currentLayer = _planeData.GetLayer(_currentLayerIndex);
                    int index = 0;

                    foreach (var item in jsonEl.EnumerateArray())
                    {
                        if (index >= currentLayer.Length) break;

                        // New format: object with tileIndex, flipH, flipV, rotation
                        if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            currentLayer[index] = new TileEntry
                            {
                                TileIndex = item.TryGetProperty("tileIndex", out var ti) ? ti.GetInt32() : -1,
                                FlipH = item.TryGetProperty("flipH", out var fh) && fh.GetBoolean(),
                                FlipV = item.TryGetProperty("flipV", out var fv) && fv.GetBoolean(),
                                Rotation = item.TryGetProperty("rotation", out var rot) ? rot.GetInt32() : 0
                            };
                        }
                        // Old format: int (backward compat - treat as plain tile index)
                        else if (item.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            int tileIndex = item.GetInt32();
                            currentLayer[index] = new TileEntry { TileIndex = tileIndex };
                        }

                        index++;
                    }
                    RenderCanvas();
                }
                else if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var base64Data = jsonEl.GetString();
                    if (!string.IsNullOrEmpty(base64Data))
                        LoadFromBase64(base64Data);
                }
            }
            else if (tilesObj is object[] objArray)
            {
                var currentLayer = _planeData.GetLayer(_currentLayerIndex);
                for (int i = 0; i < Math.Min(objArray.Length, currentLayer.Length); i++)
                {
                    // Handle anonymous objects from BtnSave
                    var obj = objArray[i];
                    var type = obj.GetType();
                    var tiProp = type.GetProperty("tileIndex");
                    var fhProp = type.GetProperty("flipH");
                    var fvProp = type.GetProperty("flipV");
                    var rotProp = type.GetProperty("rotation");

                    currentLayer[i] = new TileEntry
                    {
                        TileIndex = tiProp != null ? (int)tiProp.GetValue(obj)! : -1,
                        FlipH = fhProp != null && (bool)fhProp.GetValue(obj)!,
                        FlipV = fvProp != null && (bool)fvProp.GetValue(obj)!,
                        Rotation = rotProp != null ? (int)rotProp.GetValue(obj)! : 0
                    };
                }
                RenderCanvas();
            }
        }
        else if (moduleData.ContainsKey("data"))
        {
            var dataObj = moduleData["data"];
            if (dataObj is string base64Data)
                LoadFromBase64(base64Data);
        }
    }

    private void LoadFromBase64(string base64Data)
    {
        var entries = PlaneSerializer.FromBase64(base64Data, _planeData.Width * _planeData.Height);
        var currentLayer = _planeData.GetLayer(_currentLayerIndex);
        Array.Copy(entries, currentLayer, Math.Min(entries.Length, currentLayer.Length));
        RenderCanvas();
    }
}
