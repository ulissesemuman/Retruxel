using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
            CmbPalette.SelectionChanged += CmbPalette_SelectionChanged;
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

    private void CmbPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPalette.SelectedIndex < 0 || _isInitializing) return;

        _selectedPaletteSlot = CmbPalette.SelectedIndex;
        SavePaletteSlotSelection();

        // Refresh preview in memory only
        RefreshTilesetPreview();
        PopulateTilesetGrid();
        RenderCanvas();
    }

    private void CmbPalette_DropDownClosed(object sender, EventArgs e)
    {
        if (_isInitializing) return;

        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
            OpenPaletteEditor();
    }

    private void SavePaletteSlotSelection()
    {
        if (ModuleData == null)
            ModuleData = new Dictionary<string, object>();

        ModuleData["paletteSlot"] = _selectedPaletteSlot;
    }

    private async void BtnEditPalette_Click(object sender, RoutedEventArgs e)
    {
        if (CmbPalette.SelectedItem == null)
        {
            MessageBox.Show("Please select a palette slot to edit.", "No Palette Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string selectedText = CmbPalette.SelectedItem.ToString()!;
        if (!selectedText.StartsWith("Slot "))
        {
            MessageBox.Show("Invalid palette slot format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string slotNumberStr = selectedText.Substring(5, selectedText.IndexOf(' ', 5) - 5);
        if (!int.TryParse(slotNumberStr, out int slotIndex))
        {
            MessageBox.Show("Could not parse slot index.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_currentScene == null)
        {
            MessageBox.Show("No scene is currently loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (slotIndex >= _currentScene.PaletteSlots.Count)
        {
            MessageBox.Show($"Palette slot {slotIndex} does not exist in the current scene.", "Slot Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var currentSlot = _currentScene.PaletteSlots[slotIndex];
            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(_target, currentSlot) { Owner = this };

            if (paletteEditor.ShowDialog() == true)
            {
                if (_saveProjectCallback != null)
                    await _saveProjectCallback.Invoke();

                // Refresh preview in memory only
                RefreshTilesetPreview();
                PopulateTilesetGrid();
                RenderCanvas();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Failed to edit palette: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenPaletteEditor(AssetEntry? sourceAsset = null)
    {
        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show("Tool registry not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paletteEditorTool = _toolRegistry.GetVisualTool("palette_editor");
            if (paletteEditorTool == null)
            {
                MessageBox.Show("Palette Editor tool not found. Make sure the plugin is installed.", "Tool Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extension = _toolRegistry.GetTool($"palette_editor_ext_{_target.TargetId}");
            if (extension == null)
            {
                MessageBox.Show($"Target '{_target.DisplayName}' does not support Palette Editor yet.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extensionResult = extension.Execute(new Dictionary<string, object>());
            if (!extensionResult.ContainsKey("paletteProvider"))
            {
                MessageBox.Show($"Target '{_target.DisplayName}' palette extension is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paletteProvider = (IPaletteProvider)extensionResult["paletteProvider"];

            // If no sourceAsset provided, try to get currently selected asset
            if (sourceAsset == null && CmbTilesetAsset.SelectedItem != null)
            {
                var assetId = CmbTilesetAsset.SelectedItem.ToString()!;
                sourceAsset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            }

            byte[]? initialColors = null;
            if (sourceAsset != null)
                initialColors = ExtractColorsFromAsset(sourceAsset, paletteProvider);

            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(paletteProvider, "tilemap_editor", initialColors, null, _project)
            {
                Owner = this
            };

            if (paletteEditor.ShowDialog() == true && paletteEditor.ModuleData != null)
            {
                var paletteName = paletteEditor.ModuleData.ContainsKey("name")
                    ? paletteEditor.ModuleData["name"].ToString()!
                    : "palette";

                var existingPalettes = _project.Scenes.SelectMany(s => s.Elements)
                    .Where(e => e.ModuleId == "palette")
                    .Select(e => e.ElementId)
                    .ToHashSet();

                var paletteId = paletteName;
                int suffix = 1;
                while (existingPalettes.Contains(paletteId))
                {
                    paletteId = $"{paletteName}_{suffix}";
                    suffix++;
                }

                var paletteElement = new SceneElementData
                {
                    ElementId = paletteId,
                    ModuleId = "palette",
                    ModuleState = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(paletteEditor.ModuleData)
                    ).RootElement.Clone(),
                    TileX = 0,
                    TileY = 0,
                    Trigger = "OnStart"
                };

                if (_project.Scenes.Count > 0)
                {
                    _project.Scenes[0].Elements.Add(paletteElement);

                    // Add visual element to scene editor
                    if (_sceneEditor != null)
                    {
                        try
                        {
                            var addMethod = _sceneEditor.GetType().GetMethod("AddElementFromData",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                            if (addMethod != null)
                            {
                                addMethod.Invoke(_sceneEditor, new object[] { paletteElement });
                                System.Diagnostics.Debug.WriteLine($"Successfully added palette element '{paletteId}' to scene");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("WARNING: AddElementFromData method not found on SceneEditor");
                                MessageBox.Show(
                                    $"Palette '{paletteId}' was created but may not appear in the scene editor.\n" +
                                    "Please close and reopen the project to see it.",
                                    "Warning",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR: Failed to invoke AddElementFromData: {ex.Message}\n{ex.StackTrace}");
                            MessageBox.Show(
                                $"Palette '{paletteId}' was created but may not appear in the scene editor.\n" +
                                "Please close and reopen the project to see it.",
                                "Warning",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }

                    if (_saveProjectCallback != null)
                        await _saveProjectCallback.Invoke();
                }

                for (int i = 0; i < CmbPalette.Items.Count; i++)
                {
                    if (CmbPalette.Items[i].ToString() == paletteId)
                    {
                        CmbPalette.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                var previousSelection = CmbPalette.Items.Cast<string>().FirstOrDefault(s => s != "<New Palette>");
                if (previousSelection != null)
                    CmbPalette.SelectedItem = previousSelection;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Palette Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private byte[]? ExtractColorsFromAsset(AssetEntry asset, IPaletteProvider paletteProvider)
    {
        try
        {
            var absPath = Path.Combine(_projectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absPath)) return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(absPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            var uniqueColors = new HashSet<(byte R, byte G, byte B)>();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var b = pixels[i];
                var g = pixels[i + 1];
                var r = pixels[i + 2];
                var a = pixels[i + 3];

                if (a > 0)
                    uniqueColors.Add((r, g, b));

                if (uniqueColors.Count >= paletteProvider.SlotCount)
                    break;
            }

            var paletteIndices = new byte[paletteProvider.SlotCount];
            var hardwareColors = paletteProvider.HardwareColors;
            int slotIndex = 0;

            foreach (var color in uniqueColors.Take(paletteProvider.SlotCount))
            {
                int closestIndex = 0;
                double minDistance = double.MaxValue;

                for (int i = 0; i < hardwareColors.Length; i++)
                {
                    if (hardwareColors[i] is HardwareColor hwColor)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(color.R - hwColor.R, 2) +
                            Math.Pow(color.G - hwColor.G, 2) +
                            Math.Pow(color.B - hwColor.B, 2));

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestIndex = i;
                        }
                    }
                }

                paletteIndices[slotIndex++] = (byte)closestIndex;
            }

            return paletteIndices;
        }
        catch
        {
            return null;
        }
    }
}
