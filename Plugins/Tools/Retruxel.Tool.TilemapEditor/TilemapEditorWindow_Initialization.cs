using Retruxel.Core.Models;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void InitializeUI()
    {
        var specs = _target.Specs.Tilemap;

        int mapWidth = specs.DefaultWidth * 2;
        int mapHeight = specs.DefaultHeight * 2;
        TxtWidth.Text = mapWidth.ToString();
        TxtHeight.Text = mapHeight.ToString();

        _tilemapData.Initialize(mapWidth, mapHeight, specs.MaxLayers);

        if (specs.MaxLayers > 1)
        {
            PanelLayers.Visibility = System.Windows.Visibility.Visible;
            CmbLayers.Items.Clear();
            for (int i = 0; i < specs.MaxLayers; i++)
                CmbLayers.Items.Add($"Layer {i + 1}");
            CmbLayers.SelectedIndex = 0;

            TxtLayerInfo.Text = $"Layer 1 of {specs.MaxLayers}";
        }
        else
        {
            PanelLayers.Visibility = System.Windows.Visibility.Collapsed;
            TxtLayerInfo.Text = "";
        }

        ConfigurePaletteUI(specs.PaletteMode, specs.PaletteBitsPerTile);
        InitializePaletteSlotSelector();
        RenderCanvas();

        _isInitializing = false;
    }

    private void ConfigurePaletteUI(PaletteMode mode, int paletteBits)
    {
        // Palette UI is now handled by InitializePaletteSlotSelector()
        // This method is kept for compatibility but does nothing
        // The old palette module system is obsolete
    }
}
