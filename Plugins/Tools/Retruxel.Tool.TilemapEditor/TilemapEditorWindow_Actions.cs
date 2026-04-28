using Retruxel.Tool.TilemapEditor.Helpers;
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

    private void BtnSave_Click(object sender, RoutedEventArgs e)
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

        var base64Data = TilemapSerializer.ToBase64(_tilemapData.GetLayer(_currentLayerIndex));
        var bytes = Convert.FromBase64String(base64Data);
        var mapDataArray = new int[bytes.Length / 2];

        for (int i = 0; i < mapDataArray.Length; i++)
        {
            ushort value = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            mapDataArray[i] = value == 0xFFFF ? -1 : value;
        }

        ModuleData = new Dictionary<string, object>
        {
            ["moduleId"] = "tilemap",
            ["mapWidth"] = int.Parse(TxtWidth.Text),
            ["mapHeight"] = int.Parse(TxtHeight.Text),
            ["tilesAssetId"] = CmbTilesetAsset.SelectedItem.ToString()!,
            ["paletteRef"] = CmbPalette.SelectedItem?.ToString() ?? "",
            ["mapData"] = mapDataArray,
            ["mapAssetId"] = "",
            ["startTile"] = 0,
            ["mapX"] = _mapOffsetX,
            ["mapY"] = _mapOffsetY,
            ["solidTiles"] = Array.Empty<int>()
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
            _tilemapData.ClearLayer(_currentLayerIndex);
            RenderCanvas();
        }
    }

    private void BtnFill_Click(object sender, RoutedEventArgs e)
    {
        _tilemapData.FillLayer(_currentLayerIndex, _selectedTileId);
        RenderCanvas();
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

        _tilemapData.Resize(width, height);

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

        if (moduleData.ContainsKey("paletteRef"))
        {
            string paletteRef = moduleData["paletteRef"].ToString()!;
            for (int i = 0; i < CmbPalette.Items.Count; i++)
            {
                if (CmbPalette.Items[i].ToString() == paletteRef)
                {
                    CmbPalette.SelectedIndex = i;
                    break;
                }
            }
        }

        if (moduleData.ContainsKey("mapData"))
        {
            var mapDataObj = moduleData["mapData"];

            if (mapDataObj is System.Text.Json.JsonElement jsonEl)
            {
                if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var intArray = jsonEl.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                    var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
                    Array.Copy(intArray, currentLayer, Math.Min(intArray.Length, currentLayer.Length));
                    RenderCanvas();
                }
                else if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var base64Data = jsonEl.GetString();
                    if (!string.IsNullOrEmpty(base64Data))
                        LoadFromBase64(base64Data);
                }
            }
            else if (mapDataObj is int[] intArray)
            {
                var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
                Array.Copy(intArray, currentLayer, Math.Min(intArray.Length, currentLayer.Length));
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
        var intArray = TilemapSerializer.FromBase64(base64Data, _tilemapData.Width * _tilemapData.Height);
        var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
        Array.Copy(intArray, currentLayer, Math.Min(intArray.Length, currentLayer.Length));
        RenderCanvas();
    }
}
