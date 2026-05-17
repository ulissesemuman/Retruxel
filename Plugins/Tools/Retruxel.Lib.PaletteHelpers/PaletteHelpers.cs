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
    /// Populates a ComboBox with palette slot items from the target.
    /// </summary>
    /// <param name="comboBox">ComboBox to populate</param>
    /// <param name="target">Target platform</param>
    /// <param name="selectedIndex">Index to select after population</param>
    public static void PopulatePaletteSlotComboBox(ComboBox comboBox, ITarget target, int selectedIndex = 0)
    {
        comboBox.Items.Clear();

        var slotCount = target.GetPaletteSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slotType = target.GetPaletteSlotType(i);
            var itemText = $"Slot {i} — {slotType}";
            comboBox.Items.Add(itemText);
        }

        if (comboBox.Items.Count > 0 && selectedIndex < comboBox.Items.Count)
        {
            comboBox.SelectedIndex = selectedIndex;
        }
    }

    /// <summary>
    /// Parses the slot index from a ComboBox item text like "Slot 0 — Background".
    /// </summary>
    /// <param name="itemText">ComboBox item text</param>
    /// <param name="slotIndex">Parsed slot index</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool ParseSlotIndexFromComboBoxItem(string itemText, out int slotIndex)
    {
        slotIndex = -1;

        if (string.IsNullOrEmpty(itemText) || !itemText.StartsWith("Slot "))
            return false;

        var spaceIndex = itemText.IndexOf(' ', 5);
        if (spaceIndex < 0)
            return false;

        var slotNumberStr = itemText.Substring(5, spaceIndex - 5);
        return int.TryParse(slotNumberStr, out slotIndex);
    }

    /// <summary>
    /// Saves the selected palette slot to ModuleData dictionary.
    /// </summary>
    /// <param name="moduleData">ModuleData dictionary (will be created if null)</param>
    /// <param name="slotIndex">Slot index to save</param>
    /// <returns>Updated ModuleData dictionary</returns>
    public static Dictionary<string, object> SavePaletteSlotToModuleData(Dictionary<string, object>? moduleData, int slotIndex)
    {
        moduleData ??= new Dictionary<string, object>();
        moduleData["paletteSlot"] = slotIndex;
        return moduleData;
    }

    /// <summary>
    /// Opens the PaletteEditorWindow for editing a scene palette slot.
    /// </summary>
    /// <param name="target">Target platform</param>
    /// <param name="scene">Current scene</param>
    /// <param name="slotIndex">Slot index to edit</param>
    /// <param name="owner">Owner window</param>
    /// <returns>True if user saved changes</returns>
    public static bool OpenPaletteEditorForSlot(ITarget target, SceneData scene, int slotIndex, Window owner)
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
            var currentSlot = scene.PaletteSlots[slotIndex];
            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(target, currentSlot)
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
    /// Resolves palette slot hex colors to hardware colors by finding closest match in target palette.
    /// </summary>
    /// <param name="slot">Palette slot data</param>
    /// <param name="target">Target platform</param>
    /// <returns>List of hardware colors</returns>
    public static IReadOnlyList<HardwareColor> ResolvePaletteColors(PaletteSlotData slot, ITarget target)
    {
        var hardwarePalette = target.GetHardwarePalette();
        var result = new List<HardwareColor>();

        foreach (var hexColor in slot.Colors)
        {
            var hw = FindClosestHardwareColor(hexColor, hardwarePalette);
            result.Add(hw);
        }

        if (result.Count == 0)
            result.Add(new HardwareColor(0, 0, 0));

        return result;
    }

    /// <summary>
    /// Finds the closest hardware color to a given hex color using Euclidean distance in RGB space.
    /// </summary>
    /// <param name="hexColor">Hex color string (e.g., "#FF0000")</param>
    /// <param name="palette">Hardware palette to search</param>
    /// <returns>Closest hardware color</returns>
    public static HardwareColor FindClosestHardwareColor(string hexColor, IReadOnlyList<HardwareColor> palette)
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
            if (dist == 0)
                break;
        }

        return best;
    }
}
