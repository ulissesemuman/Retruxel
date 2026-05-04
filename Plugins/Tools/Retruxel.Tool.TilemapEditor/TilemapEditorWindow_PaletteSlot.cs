using System.Windows;
using System.Windows.Controls;
using Retruxel.Tool.PaletteEditor;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private int _selectedPaletteSlot = 0;

    private void InitializePaletteSlotSelector()
    {
        if (_target == null) return;

        // Clear existing palette UI
        CmbPalette.Visibility = Visibility.Collapsed;
        BtnEditPalette.Visibility = Visibility.Collapsed;

        // Create slot selector
        var slotCount = _target.GetPaletteSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slotType = _target.GetPaletteSlotType(i);
            CmbPalette.Items.Add($"Slot {i} \u2014 {slotType}");
        }

        CmbPalette.SelectedIndex = _selectedPaletteSlot;
        CmbPalette.SelectionChanged += CmbPaletteSlot_SelectionChanged;
        CmbPalette.Visibility = Visibility.Visible;
        BtnEditPalette.Visibility = Visibility.Visible;
        BtnEditPalette.Click += BtnEditPalette_Click;
    }

    private void CmbPaletteSlot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPalette.SelectedIndex < 0 || _isInitializing) return;

        _selectedPaletteSlot = CmbPalette.SelectedIndex;
        SavePaletteSlotSelection();

        // Refresh preview with new palette slot
        RefreshTilesetPreview();
        RenderCanvas();
    }

    private void SavePaletteSlotSelection()
    {
        if (ModuleData == null)
            ModuleData = new Dictionary<string, object>();

        ModuleData["paletteSlot"] = _selectedPaletteSlot;
    }
}
