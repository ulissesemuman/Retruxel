using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Tool.TilemapEditor.Helpers;

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
        RenderCanvas();

        _isInitializing = false;
    }

    private void ConfigurePaletteUI(PaletteMode mode, int paletteBits)
    {
        CmbPalette.Items.Clear();

        var paletteModules = _project.Scenes
            .SelectMany(s => s.Elements)
            .Where(e => e.ModuleId == "palette")
            .ToList();

        switch (mode)
        {
            case PaletteMode.PerTilemap:
                LblPalette.Text = "PALETTE (TILEMAP)";
                TxtPaletteModeInfo.Text = "One palette for the entire tilemap. All tiles share the same palette.";

                foreach (var pal in paletteModules)
                    CmbPalette.Items.Add(pal.ElementId);

                CmbPalette.Items.Add("<New Palette>");
                CmbPalette.SelectedIndex = 0;
                break;

            case PaletteMode.PerTile:
                int paletteCount = 1 << paletteBits;
                LblPalette.Text = "PALETTE (DEFAULT)";
                TxtPaletteModeInfo.Text = $"Each tile can use one of {paletteCount} palettes. Select default palette here. Use palette brush to paint per-tile palettes.";

                int addedCount = 0;
                foreach (var pal in paletteModules.Take(paletteCount))
                {
                    CmbPalette.Items.Add(pal.ElementId);
                    addedCount++;
                }

                for (int i = addedCount; i < paletteCount; i++)
                    CmbPalette.Items.Add("<New Palette>");

                CmbPalette.SelectedIndex = 0;
                break;

            case PaletteMode.PerBlock:
                LblPalette.Text = "PALETTE (BLOCK)";
                TxtPaletteModeInfo.Text = "Tiles are grouped into blocks. Each block shares a palette. Use block overlay to assign palettes.";

                foreach (var pal in paletteModules)
                    CmbPalette.Items.Add(pal.ElementId);

                CmbPalette.Items.Add("<New Palette>");
                CmbPalette.SelectedIndex = 0;
                break;
        }
    }
}
