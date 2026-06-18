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

        // Get the bounding box so width/height reflect actual placed content.
        int mapWidth  = _planeData.Width;
        int mapHeight = _planeData.Height;

        // Snapshot the current layer as a flat row-major array (empty = TileEntry.Empty).
        var layerSnapshot = _planeData.GetLayer(_currentLayerIndex);

        // Serialize as typed objects for clean JSON round-trip.
        var tilesArray = layerSnapshot.Select(entry => new
        {
            tileIndex = entry.TileIndex,
            flipH     = entry.FlipH,
            flipV     = entry.FlipV,
            rotation  = entry.Rotation
        }).ToArray();

        ModuleData = new Dictionary<string, object>
        {
            ["moduleId"]     = "plane",
            ["mapWidth"]     = mapWidth,
            ["mapHeight"]    = mapHeight,
            ["tilesAssetId"] = CmbTilesetAsset.SelectedItem.ToString()!,
            ["paletteSlot"]  = _selectedPaletteSlot,
            ["tiles"]        = tilesArray,
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
        // Fill uses the current viewport dimensions as the fill area.
        int fillW = _planeSpecs.DefaultWidth;
        int fillH = _planeSpecs.DefaultHeight;

        var entry = new TileEntry
        {
            TileIndex = _selectedTileId,
            FlipH     = _selectedFlipH,
            FlipV     = _selectedFlipV
        };

        _planeData.FillLayer(_currentLayerIndex, entry, fillW, fillH);
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

            // Clear existing content and place tiles sequentially starting at (0,0).
            _planeData.ClearLayer(_currentLayerIndex);

            for (int i = 0; i < tileCount; i++)
            {
                int x = i % columns;
                int y = i / columns;
                _planeData.SetTile(_currentLayerIndex, x, y, new TileEntry { TileIndex = i });
            }

            // Update dimension display to reflect the imported layout.
            TxtWidth.Text  = columns.ToString();
            TxtHeight.Text = rows.ToString();

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
        // Read saved width/height — used to interpret the flat tiles array.
        int savedWidth  = _planeSpecs.DefaultWidth;
        int savedHeight = _planeSpecs.DefaultHeight;

        if (moduleData.ContainsKey("mapWidth"))
        {
            var w = moduleData["mapWidth"];
            savedWidth = w is System.Text.Json.JsonElement jw ? jw.GetInt32() : Convert.ToInt32(w);
            TxtWidth.Text = savedWidth.ToString();
        }
        else if (moduleData.ContainsKey("width"))
        {
            var w = moduleData["width"];
            savedWidth = w is System.Text.Json.JsonElement jw ? jw.GetInt32() : Convert.ToInt32(w);
            TxtWidth.Text = savedWidth.ToString();
        }

        if (moduleData.ContainsKey("mapHeight"))
        {
            var h = moduleData["mapHeight"];
            savedHeight = h is System.Text.Json.JsonElement jh ? jh.GetInt32() : Convert.ToInt32(h);
            TxtHeight.Text = savedHeight.ToString();
        }
        else if (moduleData.ContainsKey("height"))
        {
            var h = moduleData["height"];
            savedHeight = h is System.Text.Json.JsonElement jh ? jh.GetInt32() : Convert.ToInt32(h);
            TxtHeight.Text = savedHeight.ToString();
        }

        // Load palette slot.
        if (moduleData.ContainsKey("paletteSlot"))
        {
            var slotObj = moduleData["paletteSlot"];
            _selectedPaletteSlot = slotObj is System.Text.Json.JsonElement jsonSlot
                ? jsonSlot.GetInt32()
                : Convert.ToInt32(slotObj);

            if (_currentScene != null && _selectedPaletteSlot >= _currentScene.PaletteSlots.Count)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Loaded palette slot {_selectedPaletteSlot} is out of range, resetting to 0");
                _selectedPaletteSlot = 0;
            }

            if (_selectedPaletteSlot < CmbPalette.Items.Count)
                CmbPalette.SelectedIndex = _selectedPaletteSlot;
        }

        // Load map offset.
        if (moduleData.ContainsKey("mapX"))
        {
            var mapXObj = moduleData["mapX"];
            _mapOffsetX = mapXObj is System.Text.Json.JsonElement jsonX
                ? jsonX.GetInt32()
                : Convert.ToInt32(mapXObj);
        }

        if (moduleData.ContainsKey("mapY"))
        {
            var mapYObj = moduleData["mapY"];
            _mapOffsetY = mapYObj is System.Text.Json.JsonElement jsonY
                ? jsonY.GetInt32()
                : Convert.ToInt32(mapYObj);
        }

        UpdateMapOffset();

        // Initialize sparse plane (no fixed allocation needed).
        _planeData.Initialize(1);

        // Load tileset asset.
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

        // Load tile data.
        if (moduleData.ContainsKey("tiles"))
        {
            var tilesObj = moduleData["tiles"];

            if (tilesObj is System.Text.Json.JsonElement jsonEl)
            {
                if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    LoadTilesFromJsonArray(jsonEl, savedWidth, savedHeight);
                }
                else if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var base64Data = jsonEl.GetString();
                    if (!string.IsNullOrEmpty(base64Data))
                        LoadFromBase64(base64Data, savedWidth, savedHeight);
                }
            }
            else if (tilesObj is object[] objArray)
            {
                LoadTilesFromObjectArray(objArray, savedWidth);
            }
        }
        else if (moduleData.ContainsKey("data"))
        {
            var dataObj = moduleData["data"];
            if (dataObj is string base64Data)
                LoadFromBase64(base64Data, savedWidth, savedHeight);
        }

        RenderCanvas();
    }

    // ── Tile deserialization helpers ──────────────────────────────────────────

    private void LoadTilesFromJsonArray(System.Text.Json.JsonElement jsonArray, int width, int height)
    {
        int index = 0;
        int maxIndex = width * height;

        foreach (var item in jsonArray.EnumerateArray())
        {
            if (index >= maxIndex) break;

            int x = index % width;
            int y = index / width;

            TileEntry entry;

            if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                entry = new TileEntry
                {
                    TileIndex = item.TryGetProperty("tileIndex", out var ti) ? ti.GetInt32() : -1,
                    FlipH     = item.TryGetProperty("flipH",     out var fh) && fh.GetBoolean(),
                    FlipV     = item.TryGetProperty("flipV",     out var fv) && fv.GetBoolean(),
                    Rotation  = item.TryGetProperty("rotation",  out var rot) ? rot.GetInt32() : 0
                };
            }
            else if (item.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                // Legacy format: bare int tile index.
                entry = new TileEntry { TileIndex = item.GetInt32() };
            }
            else
            {
                index++;
                continue;
            }

            if (!entry.IsEmpty)
                _planeData.SetTile(_currentLayerIndex, x, y, entry);

            index++;
        }
    }

    private void LoadTilesFromObjectArray(object[] objArray, int width)
    {
        for (int i = 0; i < objArray.Length; i++)
        {
            int x = i % width;
            int y = i / width;

            var obj   = objArray[i];
            var type  = obj.GetType();
            var tiProp  = type.GetProperty("tileIndex");
            var fhProp  = type.GetProperty("flipH");
            var fvProp  = type.GetProperty("flipV");
            var rotProp = type.GetProperty("rotation");

            var entry = new TileEntry
            {
                TileIndex = tiProp  != null ? (int) tiProp.GetValue(obj)!  : -1,
                FlipH     = fhProp  != null && (bool)fhProp.GetValue(obj)!,
                FlipV     = fvProp  != null && (bool)fvProp.GetValue(obj)!,
                Rotation  = rotProp != null ? (int) rotProp.GetValue(obj)! : 0
            };

            if (!entry.IsEmpty)
                _planeData.SetTile(_currentLayerIndex, x, y, entry);
        }
    }

    private void LoadFromBase64(string base64Data, int width, int height)
    {
        var entries = PlaneSerializer.FromBase64(base64Data, width * height);
        _planeData.LoadLayer(_currentLayerIndex, entries, width, height);
    }
}
