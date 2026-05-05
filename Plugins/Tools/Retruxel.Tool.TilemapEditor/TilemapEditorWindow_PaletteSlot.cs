using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private int _selectedPaletteSlot = 0;

    private void InitializePaletteSlotSelector()
    {
        if (_target == null)
        {
            System.Diagnostics.Debug.WriteLine("[TilemapEditor] InitializePaletteSlotSelector: _target is null!");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[TilemapEditor] InitializePaletteSlotSelector: Target = {_target.DisplayName}");

        // Clear existing palette UI
        CmbPalette.Visibility = Visibility.Collapsed;
        BtnEditPalette.Visibility = Visibility.Collapsed;
        CmbPalette.Items.Clear();

        // Create slot selector
        var slotCount = _target.GetPaletteSlotCount();
        System.Diagnostics.Debug.WriteLine($"[TilemapEditor] Palette slot count: {slotCount}");

        for (int i = 0; i < slotCount; i++)
        {
            var slotType = _target.GetPaletteSlotType(i);
            var itemText = $"Slot {i} \u2014 {slotType}";
            CmbPalette.Items.Add(itemText);
            System.Diagnostics.Debug.WriteLine($"[TilemapEditor] Added palette slot: {itemText}");
        }

        if (CmbPalette.Items.Count > 0)
        {
            CmbPalette.SelectedIndex = _selectedPaletteSlot;
            CmbPalette.SelectionChanged += CmbPaletteSlot_SelectionChanged;
            CmbPalette.Visibility = Visibility.Visible;
            BtnEditPalette.Visibility = Visibility.Visible;
            BtnEditPalette.Click += BtnEditPalette_Click;
            System.Diagnostics.Debug.WriteLine($"[TilemapEditor] Palette selector initialized with {CmbPalette.Items.Count} slots");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[TilemapEditor] WARNING: No palette slots added!");
        }
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
