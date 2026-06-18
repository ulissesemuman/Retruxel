namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void InitializeUI()
    {
        // The plane is sparse — no fixed width/height allocation needed.
        // TxtWidth and TxtHeight now show the computed bounding box and are read-only
        // display fields; the canvas expands automatically as the user paints.
        _planeData.Initialize(1);

        // Seed the display fields with the viewport size as a reference point.
        var specs = _planeSpecs;
        TxtWidth.Text  = specs.DefaultWidth.ToString();
        TxtHeight.Text = specs.DefaultHeight.ToString();

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
