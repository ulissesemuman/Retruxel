using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Lib.PaletteHelpers;

/// <summary>
/// Helper methods for palette slot management shared across editor tools.
/// </summary>
public static class PaletteHelpers
{
    /// <summary>
    /// Populates a ComboBox with palette slot items from the target and wires
    /// up the SelectionChanged and BtnEditPalette click handlers.
    /// Call this during editor initialization; it removes any previous handlers
    /// before re-adding them to prevent duplicates on re-init.
    /// </summary>
    public static void InitializePaletteSlotComboBox(
        ComboBox comboBox,
        Button editButton,
        ITarget target,
        int defaultSlot,
        SelectionChangedEventHandler onSelectionChanged,
        RoutedEventHandler onEditClick,
        string debugTag = "")
    {
        // Hide while rebuilding to avoid layout flicker
        comboBox.Visibility = Visibility.Collapsed;
        editButton.Visibility = Visibility.Collapsed;
        comboBox.Items.Clear();

        // Detach stale handlers
        comboBox.SelectionChanged -= onSelectionChanged;
        editButton.Click -= onEditClick;

        var slotCount = target.GetPaletteSlotCount();

        if (!string.IsNullOrEmpty(debugTag))
            System.Diagnostics.Debug.WriteLine(
                $"[{debugTag}] InitializePaletteSlotComboBox: {slotCount} slots");

        for (int i = 0; i < slotCount; i++)
        {
            var slotType = target.GetPaletteSlotType(i);
            comboBox.Items.Add($"Slot {i} \u2014 {slotType}");
        }

        if (comboBox.Items.Count > 0)
        {
            int safeIndex = Math.Clamp(defaultSlot, 0, comboBox.Items.Count - 1);
            comboBox.SelectedIndex = safeIndex;
            comboBox.SelectionChanged += onSelectionChanged;
            comboBox.Visibility = Visibility.Visible;
            editButton.Click += onEditClick;
            editButton.Visibility = Visibility.Visible;
        }
        else if (!string.IsNullOrEmpty(debugTag))
        {
            System.Diagnostics.Debug.WriteLine($"[{debugTag}] WARNING: No palette slots added!");
        }
    }

    /// <summary>
    /// Populates a ComboBox with palette slot items from the target (no button overload).
    /// </summary>
    public static void PopulatePaletteSlotComboBox(
        ComboBox comboBox,
        ITarget target,
        int selectedIndex = 0)
    {
        comboBox.Items.Clear();

        var slotCount = target.GetPaletteSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slotType = target.GetPaletteSlotType(i);
            comboBox.Items.Add($"Slot {i} \u2014 {slotType}");
        }

        if (comboBox.Items.Count > 0 && selectedIndex < comboBox.Items.Count)
            comboBox.SelectedIndex = selectedIndex;
    }

    /// <summary>
    /// Parses the slot index from a ComboBox item text like "Slot 0 — Background".
    /// </summary>
    public static bool ParseSlotIndexFromComboBoxItem(string itemText, out int slotIndex)
    {
        slotIndex = -1;

        if (string.IsNullOrEmpty(itemText) || !itemText.StartsWith("Slot "))
            return false;

        var spaceIndex = itemText.IndexOf(' ', 5);
        if (spaceIndex < 0)
            return false;

        return int.TryParse(itemText.Substring(5, spaceIndex - 5), out slotIndex);
    }

    /// <summary>
    /// Saves the selected palette slot index to a ModuleData dictionary.
    /// Creates the dictionary if null.
    /// </summary>
    public static Dictionary<string, object> SavePaletteSlotToModuleData(
        Dictionary<string, object>? moduleData,
        int slotIndex)
    {
        moduleData ??= new Dictionary<string, object>();
        moduleData["paletteSlot"] = slotIndex;
        return moduleData;
    }

    /// <summary>
    /// Opens the PaletteEditorWindow for a scene palette slot.
    /// Returns true if the user saved changes.
    /// </summary>
    public static bool OpenPaletteEditorForSlot(
        ITarget target,
        SceneData scene,
        int slotIndex,
        Window owner)
    {
        if (slotIndex < 0 || slotIndex >= scene.PaletteSlots.Count)
        {
            MessageBox.Show(
                $"Palette slot {slotIndex} does not exist in the current scene.",
                "Slot Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        try
        {
            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(
                target, scene.PaletteSlots[slotIndex])
            {
                Owner = owner
            };
            return paletteEditor.ShowDialog() == true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to edit palette: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Resolves a palette slot's hex colors to the nearest hardware colors.
    /// </summary>
    public static IReadOnlyList<HardwareColor> ResolvePaletteColors(
        PaletteSlotData slot,
        ITarget target)
    {
        var hardwarePalette = target.GetHardwarePalette();
        var result = new List<HardwareColor>();

        foreach (var hexColor in slot.Colors)
            result.Add(FindClosestHardwareColor(hexColor, hardwarePalette));

        if (result.Count == 0)
            result.Add(new HardwareColor(0, 0, 0));

        return result;
    }

    /// <summary>
    /// Finds the closest hardware color to a hex string using Manhattan distance in RGB.
    /// </summary>
    public static HardwareColor FindClosestHardwareColor(
        string hexColor,
        IReadOnlyList<HardwareColor> palette)
    {
        if (palette.Count == 0)
            return new HardwareColor(0, 0, 0);

        if (string.IsNullOrEmpty(hexColor) || hexColor.Length < 7 || hexColor[0] != '#')
            return palette[0];

        int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

        HardwareColor best = palette[0];
        int bestDist = int.MaxValue;

        foreach (var hw in palette)
        {
            int dist = Math.Abs(hw.R - r) + Math.Abs(hw.G - g) + Math.Abs(hw.B - b);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hw;
            }
            if (dist == 0) break;
        }

        return best;
    }
}
