using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow
{
    private readonly List<Border> _paletteBorders = [];

    private void InitializePalette()
    {
        _hardwarePalette = _target?.GetHardwarePalette() ?? [];

        // Resolve palette colors from scene slot 0 (BG), fallback to hardware palette
        if (_currentScene?.PaletteSlots.Count > 0)
        {
            _paletteColors = _currentScene.PaletteSlots[0].Colors
                .Where(c => c != "-1")
                .ToList();
        }
        else
        {
            // Use first 16 hardware colors as default
            _paletteColors = _hardwarePalette
                .Take(16)
                .Select(c => $"#{c.R:X2}{c.G:X2}{c.B:X2}")
                .ToList();
        }

        BuildPaletteUI();
    }

    private void BuildPaletteUI()
    {
        CmbPalette.Items.Clear();
        _paletteBorders.Clear();

        // Replace ComboBox with a WrapPanel of color swatches
        var swatchPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

        for (int i = 0; i < _paletteColors.Count; i++)
        {
            var colorIndex = i;
            var hex = _paletteColors[i];

            Color color;
            try { color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex); }
            catch { color = Colors.Black; }

            var border = new Border
            {
                Width           = 20,
                Height          = 20,
                Background      = new SolidColorBrush(color),
                BorderBrush     = colorIndex == _selectedColorIndex
                    ? Brushes.White : Brushes.Transparent,
                BorderThickness = new Thickness(2),
                Margin          = new Thickness(1),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Tag             = colorIndex,
                ToolTip         = $"Index {colorIndex}: {hex}"
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                _selectedColorIndex = colorIndex;
                UpdatePaletteSelection();
                TxtPixelColor.Text = $"Index {colorIndex} — {hex}";
            };

            _paletteBorders.Add(border);
            swatchPanel.Children.Add(border);

            // Also add to ComboBox for text reference
            CmbPalette.Items.Add($"{i}: {hex}");
        }

        // Insert the swatch panel after the PALETTE label
        // Find PropertiesPanel and inject before CmbPalette
        if (PropertiesPanel != null)
        {
            // Remove old swatch panel if exists
            var existing = PropertiesPanel.Children
                .OfType<WrapPanel>()
                .FirstOrDefault();
            if (existing != null) PropertiesPanel.Children.Remove(existing);

            // Insert after CmbPalette
            int idx = PropertiesPanel.Children.IndexOf(CmbPalette);
            if (idx >= 0)
                PropertiesPanel.Children.Insert(idx + 1, swatchPanel);
        }

        if (CmbPalette.Items.Count > 0)
            CmbPalette.SelectedIndex = _selectedColorIndex;

        TxtPaletteInfo.Text = $"{_paletteColors.Count} colors — hardware limited";
    }

    private void UpdatePaletteSelection()
    {
        for (int i = 0; i < _paletteBorders.Count; i++)
        {
            _paletteBorders[i].BorderBrush = i == _selectedColorIndex
                ? Brushes.White : Brushes.Transparent;
        }
        if (CmbPalette.Items.Count > _selectedColorIndex)
            CmbPalette.SelectedIndex = _selectedColorIndex;
    }

    private Color GetSelectedColor()
    {
        if (_selectedColorIndex < 0 || _selectedColorIndex >= _paletteColors.Count)
            return Colors.Black;
        try { return (Color)System.Windows.Media.ColorConverter.ConvertFromString(_paletteColors[_selectedColorIndex]); }
        catch { return Colors.Black; }
    }

    private Color GetColorAt(int paletteIndex)
    {
        if (paletteIndex < 0 || paletteIndex >= _paletteColors.Count)
            return Colors.Transparent;
        try { return (Color)System.Windows.Media.ColorConverter.ConvertFromString(_paletteColors[paletteIndex]); }
        catch { return Colors.Transparent; }
    }
}
