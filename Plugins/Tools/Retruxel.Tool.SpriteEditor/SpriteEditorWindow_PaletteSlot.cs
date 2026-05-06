using System.Collections.Generic;
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

        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] InitializePaletteSlotSelector: Target = {_target.DisplayName}");

        // Clear existing palette UI
        CmbPaletteSlot.Visibility = Visibility.Collapsed;
        BtnEditPalette.Visibility = Visibility.Collapsed;
        CmbPaletteSlot.Items.Clear();

        // Create slot selector
        var slotCount = _target.GetPaletteSlotCount();
        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Palette slot count: {slotCount}");

        for (int i = 0; i < slotCount; i++)
        {
            var slotType = _target.GetPaletteSlotType(i);
            var itemText = $"Slot {i} — {slotType}";
            CmbPaletteSlot.Items.Add(itemText);
            System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Added palette slot: {itemText}");
        }

        if (CmbPaletteSlot.Items.Count > 0)
        {
            CmbPaletteSlot.SelectedIndex = _selectedPaletteSlot;
            CmbPaletteSlot.SelectionChanged += CmbPaletteSlot_SelectionChanged;
            CmbPaletteSlot.Visibility = Visibility.Visible;
            BtnEditPalette.Visibility = Visibility.Visible;
            BtnEditPalette.Click += BtnEditPalette_Click;
            System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Palette selector initialized with {CmbPaletteSlot.Items.Count} slots");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[SpriteEditor] WARNING: No palette slots added!");
        }
    }

    private void CmbPaletteSlot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPaletteSlot.SelectedIndex < 0 || _isInitializing) return;

        _selectedPaletteSlot = CmbPaletteSlot.SelectedIndex;
        _activePaletteSlot = CmbPaletteSlot.SelectedIndex;
        SavePaletteSlotSelection();

        RefreshTilesetWithPalette();
        RenderCanvas();
    }

    private void SavePaletteSlotSelection()
    {
        if (ModuleData == null)
            ModuleData = new Dictionary<string, object>();

        ModuleData["paletteSlot"] = _selectedPaletteSlot;
    }

    private void BtnEditPalette_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScene == null || CmbPaletteSlot.SelectedIndex < 0)
        {
            MessageBox.Show("No palette slot selected.", "Palette Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var slotIndex = CmbPaletteSlot.SelectedIndex;
        var selectedText = CmbPaletteSlot.SelectedItem?.ToString() ?? "";

        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Opening PaletteEditor for slot {slotIndex}: {selectedText}");

        if (slotIndex >= _currentScene.PaletteSlots.Count)
        {
            MessageBox.Show($"Palette slot {slotIndex} not found in scene.", "Palette Editor", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var slot = _currentScene.PaletteSlots[slotIndex];
        System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Slot has {slot.Colors.Count} colors");

        try
        {
            // Use ITarget constructor for scene palette slot editing
            var paletteEditor = new Retruxel.Tool.PaletteEditor.PaletteEditorWindow(_target, slot)
            {
                Owner = this
            };

            if (paletteEditor.ShowDialog() == true)
            {
                // PaletteSlotData was updated directly by the editor
                System.Diagnostics.Debug.WriteLine($"[SpriteEditor] Palette updated with {slot.Colors.Count} colors");

                RefreshTilesetWithPalette();
                RenderCanvas();
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpriteEditor] ERROR opening PaletteEditor: {ex.Message}");
            MessageBox.Show($"Failed to open Palette Editor:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
