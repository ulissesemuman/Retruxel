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
    private int _selectedTransparentColorIndex = 0;
    private readonly List<Border> _transparentColorBorders = [];

    public PaletteImportResult Result { get; private set; }
    public int ChosenSlot { get; private set; }
    public int TransparentColorIndex => _selectedTransparentColorIndex;

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

    private void BuildSlotOptions()
    {
        for (int i = 0; i < _scene.PaletteSlots.Count; i++)
        {
            var slot = _scene.PaletteSlots[i];
            var slotType = _target.GetPaletteSlotType(i);
            var slotIndex = i;

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var rb = new RadioButton
            {
                Content = $"Replace Slot {i} ({slotType})",
                GroupName = "PaletteAction",
                IsChecked = i == 0,
                Style = (Style)FindResource("SegmentedRadio"),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = slotIndex
            };
            _slotRadioButtons.Add(rb);
            row.Children.Add(rb);

            // Existing colors in this slot shown as small swatches
            var swatchRow = new WrapPanel { Margin = new Thickness(12, 0, 0, 0) };
            for (int c = 0; c < Math.Min(slot.Colors.Count, 16); c++)
            {
                swatchRow.Children.Add(MakeSwatch(slot.Colors[c], 14));
            }
            row.Children.Add(swatchRow);

            SlotOptionsPanel.Children.Add(row);
        }
    }

    private void BuildColorPreview()
    {
        foreach (var hexColor in _assetColors)
            ColorPreviewPanel.Children.Add(MakeSwatch(hexColor, 20));
    }


    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (RbKeepCurrent.IsChecked == true)
        {
            Result = PaletteImportResult.KeepCurrent;
        }
        else
        {
            var selected = _slotRadioButtons.FirstOrDefault(rb => rb.IsChecked == true);
            if (selected != null)
            {
                Result = PaletteImportResult.ReplaceSlot;
                ChosenSlot = (int)selected.Tag;

                // Reorder colors: move selected transparent color to index 0
                if (_selectedTransparentColorIndex != 0)
                {
                    var transparentColor = _assetColors[_selectedTransparentColorIndex];
                    _assetColors.RemoveAt(_selectedTransparentColorIndex);
                    _assetColors.Insert(0, transparentColor);
                }
            }
            else
            {
                Result = PaletteImportResult.KeepCurrent;
            }
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private Rectangle MakeSwatch(string hexColor, int size)
    {
        Color color;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(hexColor);
        }
        catch
        {
            color = Colors.Transparent;
        }

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            Margin = new Thickness(2)
        };
    }
}
