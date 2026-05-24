namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void InitializeUI()
    {
        var specs = _planeSpecs;

        int mapWidth = specs.DefaultWidth * 2;
        int mapHeight = specs.DefaultHeight * 2;
        TxtWidth.Text = mapWidth.ToString();
        TxtHeight.Text = mapHeight.ToString();

        // Always start with 1 layer — the user can add more freely.
        // The VramAllocator determines whether there is budget for additional layers.
        _planeData.Initialize(mapWidth, mapHeight, 1);

        PanelLayers.Visibility = System.Windows.Visibility.Visible;
        CmbLayers.Items.Clear();
        CmbLayers.Items.Add("Layer 1");
        CmbLayers.SelectedIndex = 0;
        TxtLayerInfo.Text = $"Layer 1 of {_planeData.LayerCount}";

        InitializePaletteSlotSelector();
        InitializeFlipHotkeys();
        RenderCanvas();

        _isInitializing = false;
    }
}
