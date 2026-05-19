using Retruxel.Lib.PaletteHelpers;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private int _selectedPaletteSlot = 1; // Sprites default to slot 1

    private void InitializePaletteSlotSelector()
    {
        if (_target == null)
        {
            System.Diagnostics.Debug.WriteLine("[SpriteEditor] InitializePaletteSlotSelector: _target is null!");
            return;
        }

        PaletteHelpers.InitializePaletteSlotComboBox(
            comboBox:           CmbPaletteSlot,
            editButton:         BtnEditPalette,
            target:             _target,
            defaultSlot:        _selectedPaletteSlot,
            onSelectionChanged: CmbPaletteSlot_SelectionChanged,
            onEditClick:        BtnEditPalette_Click,
            debugTag:           "SpriteEditor");
    }

    private void CmbPaletteSlot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPaletteSlot.SelectedIndex < 0 || _isInitializing) return;

        _selectedPaletteSlot = CmbPaletteSlot.SelectedIndex;
        _activePaletteSlot   = CmbPaletteSlot.SelectedIndex;
        SavePaletteSlotSelection();

        RefreshTilesetWithPalette();
        RenderCanvas();
    }

    private void SavePaletteSlotSelection()
    {
        ModuleData = PaletteHelpers.SavePaletteSlotToModuleData(ModuleData, _selectedPaletteSlot);
    }

    private void BtnEditPalette_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScene == null || CmbPaletteSlot.SelectedIndex < 0)
        {
            MessageBox.Show("No palette slot selected.", "Palette Editor",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!PaletteHelpers.ParseSlotIndexFromComboBoxItem(
                CmbPaletteSlot.SelectedItem?.ToString() ?? "", out int slotIndex))
        {
            MessageBox.Show("Invalid palette slot format.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (PaletteHelpers.OpenPaletteEditorForSlot(_target, _currentScene, slotIndex, this))
        {
            RefreshTilesetWithPalette();
            RenderCanvas();
        }
    }
}
