using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.AssetImporter;

public partial class PaletteImportDialog : Window
{
    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<string> _assetColors;
    private readonly SceneData _scene;
    private readonly ITarget _target;
    private readonly List<RadioButton> _slotRadioButtons = [];

    // ── Results ───────────────────────────────────────────────────────────────

    public PaletteImportResult Result { get; private set; }
    public int ChosenSlot { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public PaletteImportDialog(List<string> assetColors, SceneData scene, ITarget target)
    {
        _assetColors = assetColors;
        _scene = scene;
        _target = target;

        InitializeComponent();

        TxtInfo.Text = $"This asset uses {assetColors.Count} color{(assetColors.Count == 1 ? "" : "s")}.";

        BuildSlotOptions();
        BuildColorPreview();
    }

    // ── UI Builders ───────────────────────────────────────────────────────────

    private void BuildSlotOptions()
    {
        for (int i = 0; i < _scene.PaletteSlots.Count; i++)
        {
            var slot     = _scene.PaletteSlots[i];
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

    // ── Event handlers ────────────────────────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────

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
