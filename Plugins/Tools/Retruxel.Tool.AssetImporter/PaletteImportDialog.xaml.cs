using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.AssetImporter;

public partial class PaletteImportDialog : Window
{
    private readonly List<string> _assetColors;
    private readonly SceneData _scene;
    private readonly ITarget _target;
    private readonly List<RadioButton> _slotRadioButtons = [];
    private readonly List<RadioButton> _mergeRadioButtons = [];
    private int _selectedTransparentColorIndex = 0;
    private readonly List<Border> _transparentColorBorders = [];

    public PaletteImportResult Result { get; private set; }
    public int ChosenSlot { get; private set; }
    public int TransparentColorIndex => _selectedTransparentColorIndex;

    /// <summary>
    /// After a MergeSlot, contains the resulting merged palette (hex strings, same size as slot).
    /// Colors at free positions (-1) that received an asset color are filled in.
    /// </summary>
    public List<string>? MergedColors { get; private set; }

    /// <summary>
    /// Remap table: assetColorIndex → mergedSlotIndex.
    /// Used by the caller to rewrite MapIndex.
    /// </summary>
    public int[]? MergeRemap { get; private set; }

    public PaletteImportDialog(List<string> assetColors, SceneData scene, ITarget target)
    {
        _assetColors = assetColors;
        _scene = scene;
        _target = target;

        InitializeComponent();

        TxtInfo.Text = $"This asset uses {assetColors.Count} color{(assetColors.Count == 1 ? "" : "s")}.";

        BuildSlotOptions();
        BuildTransparentColorSelector();
        BuildColorPreview();
    }

    // ── Merge eligibility ────────────────────────────────────────────────────

    private static bool IsSlotFree(string color) => color == "-1";

    /// <summary>
    /// Checks whether <paramref name="assetColors"/> can be merged into <paramref name="slot"/>.
    /// Returns true when all asset colors already exist in the slot OR the missing ones
    /// fit in the free (-1) positions.
    /// Also outputs the remap table and the resulting merged color list.
    /// </summary>
    private static bool CanMerge(
        List<string> assetColors,
        PaletteSlotData slot,
        out int[] remap,
        out List<string> mergedColors)
    {
        var slotColors = slot.Colors.ToList();
        remap = new int[assetColors.Count];
        mergedColors = slotColors.ToList();

        var freeSlots = new Queue<int>(
            Enumerable.Range(0, slotColors.Count).Where(i => IsSlotFree(slotColors[i])));

        for (int i = 0; i < assetColors.Count; i++)
        {
            var assetColor = assetColors[i];

            // Already in slot?
            var existing = slotColors
                .Select((c, idx) => (c, idx))
                .FirstOrDefault(x => !IsSlotFree(x.c) &&
                    string.Equals(x.c, assetColor, StringComparison.OrdinalIgnoreCase));

            if (existing.c != null)
            {
                remap[i] = existing.idx;
            }
            else if (freeSlots.Count > 0)
            {
                var freeIdx = freeSlots.Dequeue();
                mergedColors[freeIdx] = assetColor;
                remap[i] = freeIdx;
            }
            else
            {
                remap = [];
                mergedColors = [];
                return false;
            }
        }

        return true;
    }

    // ── UI building ──────────────────────────────────────────────────────────

    private void BuildSlotOptions()
    {
        for (int i = 0; i < _scene.PaletteSlots.Count; i++)
        {
            var slot = _scene.PaletteSlots[i];
            var slotType = _target.GetPaletteSlotType(i);
            var slotIndex = i;

            bool canMerge = CanMerge(_assetColors, slot, out _, out _);

            var row = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var buttonsRow = new WrapPanel { Orientation = Orientation.Horizontal };

            // Replace button
            var rbReplace = new RadioButton
            {
                Content = $"Replace Slot {i} ({slotType})",
                GroupName = "PaletteAction",
                IsChecked = i == 0,
                Style = (Style)FindResource("SegmentedRadio"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = (slotIndex, PaletteImportResult.ReplaceSlot)
            };
            _slotRadioButtons.Add(rbReplace);
            buttonsRow.Children.Add(rbReplace);

            // Merge button (only when eligible)
            if (canMerge)
            {
                var rbMerge = new RadioButton
                {
                    Content = $"Merge into Slot {i} ({slotType})",
                    GroupName = "PaletteAction",
                    IsChecked = false,
                    Style = (Style)FindResource("SegmentedRadio"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                    Tag = (slotIndex, PaletteImportResult.MergeSlot)
                };
                _mergeRadioButtons.Add(rbMerge);
                buttonsRow.Children.Add(rbMerge);
            }

            row.Children.Add(buttonsRow);

            // Existing colors in this slot as small swatches
            var swatchRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            for (int c = 0; c < Math.Min(slot.Colors.Count, 16); c++)
            {
                var color = slot.Colors[c];
                swatchRow.Children.Add(IsSlotFree(color)
                    ? MakeFreeSwatch(14)
                    : MakeSwatch(color, 14));
            }
            row.Children.Add(swatchRow);

            SlotOptionsPanel.Children.Add(row);
        }
    }

    private void BuildTransparentColorSelector()
    {
        for (int i = 0; i < _assetColors.Count; i++)
        {
            var colorIndex = i;
            var hexColor = _assetColors[i];

            var border = new Border
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)),
                BorderBrush = i == 0 ? Brushes.Yellow : Brushes.Transparent,
                BorderThickness = new Thickness(3),
                Margin = new Thickness(4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = colorIndex
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                _selectedTransparentColorIndex = colorIndex;
                UpdateTransparentColorSelection();
            };

            _transparentColorBorders.Add(border);
            TransparentColorPanel.Children.Add(border);
        }
    }

    private void UpdateTransparentColorSelection()
    {
        for (int i = 0; i < _transparentColorBorders.Count; i++)
        {
            _transparentColorBorders[i].BorderBrush = i == _selectedTransparentColorIndex
                ? Brushes.Yellow
                : Brushes.Transparent;
        }
    }

    private void BuildColorPreview()
    {
        foreach (var hexColor in _assetColors)
            ColorPreviewPanel.Children.Add(MakeSwatch(hexColor, 20));
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (RbKeepCurrent.IsChecked == true)
        {
            Result = PaletteImportResult.KeepCurrent;
            DialogResult = true;
            Close();
            return;
        }

        // Check replace buttons
        var selectedReplace = _slotRadioButtons.FirstOrDefault(rb => rb.IsChecked == true);
        if (selectedReplace != null)
        {
            var (slotIdx, _) = ((int, PaletteImportResult))selectedReplace.Tag;
            Result = PaletteImportResult.ReplaceSlot;
            ChosenSlot = slotIdx;
            ReorderTransparentColor();
            DialogResult = true;
            Close();
            return;
        }

        // Check merge buttons
        var selectedMerge = _mergeRadioButtons.FirstOrDefault(rb => rb.IsChecked == true);
        if (selectedMerge != null)
        {
            var (slotIdx, _) = ((int, PaletteImportResult))selectedMerge.Tag;
            ReorderTransparentColor();

            var slot = _scene.PaletteSlots[slotIdx];
            CanMerge(_assetColors, slot, out var remap, out var merged);

            Result = PaletteImportResult.MergeSlot;
            ChosenSlot = slotIdx;
            MergeRemap = remap;
            MergedColors = merged;
            DialogResult = true;
            Close();
            return;
        }

        Result = PaletteImportResult.KeepCurrent;
        DialogResult = true;
        Close();
    }

    private void ReorderTransparentColor()
    {
        if (_selectedTransparentColorIndex != 0)
        {
            var transparentColor = _assetColors[_selectedTransparentColorIndex];
            _assetColors.RemoveAt(_selectedTransparentColorIndex);
            _assetColors.Insert(0, transparentColor);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Rectangle MakeSwatch(string hexColor, int size)
    {
        Color color;
        try { color = (Color)ColorConverter.ConvertFromString(hexColor); }
        catch { color = Colors.Transparent; }

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            Margin = new Thickness(2)
        };
    }

    private static Rectangle MakeFreeSwatch(int size) => new()
    {
        Width = size,
        Height = size,
        Fill = Brushes.Transparent,
        Stroke = Brushes.DimGray,
        StrokeThickness = 1,
        StrokeDashArray = [2, 2],
        Margin = new Thickness(2)
    };
}
