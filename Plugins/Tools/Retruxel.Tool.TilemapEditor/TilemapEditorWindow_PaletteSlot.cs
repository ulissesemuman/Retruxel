using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.PaletteHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
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

        PaletteHelpers.InitializePaletteSlotComboBox(
            comboBox:           CmbPalette,
            editButton:         BtnEditPalette,
            target:             _target,
            defaultSlot:        _selectedPaletteSlot,
            onSelectionChanged: CmbPalette_SelectionChanged,
            onEditClick:        BtnEditPalette_Click,
            debugTag:           "TilemapEditor");
    }

    private void CmbPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPalette.SelectedIndex < 0 || _isInitializing) return;

        _selectedPaletteSlot = CmbPalette.SelectedIndex;
        SavePaletteSlotSelection();

        RefreshTilesetFromAsset();
        RenderCanvas();
    }

    private void SavePaletteSlotSelection()
    {
        ModuleData = PaletteHelpers.SavePaletteSlotToModuleData(ModuleData, _selectedPaletteSlot);
    }

    private async void BtnEditPalette_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScene == null)
        {
            MessageBox.Show("No scene is currently loaded.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!PaletteHelpers.ParseSlotIndexFromComboBoxItem(
                CmbPalette.SelectedItem?.ToString() ?? "", out int slotIndex))
        {
            MessageBox.Show("Invalid palette slot format.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (PaletteHelpers.OpenPaletteEditorForSlot(_target, _currentScene, slotIndex, this))
        {
            if (_saveProjectCallback != null)
                await _saveProjectCallback.Invoke();

            RefreshTilesetFromAsset();
            RenderCanvas();
        }
    }

    private async void OpenPaletteEditor(AssetEntry? sourceAsset = null)
    {
        // Full palette creation flow (new palette from asset) — kept here as it is
        // TilemapEditor-specific logic with SceneEditor integration.
        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show("Tool registry not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paletteEditorTool = _toolRegistry.GetVisualTool("palette_editor");
            if (paletteEditorTool == null)
            {
                MessageBox.Show("Palette Editor tool not found.", "Tool Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extension = _toolRegistry.GetTool($"palette_editor_ext_{_target.TargetId}");
            if (extension == null)
            {
                MessageBox.Show($"Target '{_target.DisplayName}' does not support Palette Editor yet.",
                    "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extensionResult = extension.Execute(new Dictionary<string, object>());
            if (!extensionResult.ContainsKey("paletteProvider"))
            {
                MessageBox.Show($"Target '{_target.DisplayName}' palette extension is invalid.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paletteProvider = (Core.Interfaces.IPaletteProvider)extensionResult["paletteProvider"];

            if (sourceAsset == null && CmbTilesetAsset.SelectedItem != null)
            {
                var assetId = CmbTilesetAsset.SelectedItem.ToString()!;
                sourceAsset = _project.Assets.Find(a => a.Id == assetId);
            }

            byte[]? initialColors = null;
            if (sourceAsset != null)
            {
                if (sourceAsset?.GenerationParams?.Palette != null)
                {
                    var hardwarePalette = _target.GetHardwarePalette();
                    var fastPalette = ColorMatching.PrepareFastPalette(hardwarePalette);

                    initialColors = sourceAsset.GenerationParams.Palette
                        .Select(hex =>
                        {
                            var hw = HardwareColor.FromHex(hex);
                            return ColorMatching.FindNearestColorIndex(
                                (hw.R, hw.G, hw.B), fastPalette);
                        })
                        .ToArray();
                }
            }

            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(
                paletteProvider, "plane_editor", initialColors, null, _project)
            {
                Owner = this
            };

            if (paletteEditor.ShowDialog() == true && paletteEditor.ModuleData != null)
            {
                if (_saveProjectCallback != null)
                    await _saveProjectCallback.Invoke();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Failed to open Palette Editor: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbPalette_DropDownClosed(object sender, EventArgs e)
    {
        if (_isInitializing) return;

        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
            OpenPaletteEditor();
    }
}
