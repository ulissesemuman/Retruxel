using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Views;

public enum PaletteImportResult { ReplaceSlot, KeepCurrent }

public partial class PaletteImportDialog : Window
{
    public PaletteImportResult Result     { get; private set; }
    public int                 ChosenSlot { get; private set; }

    private readonly List<RadioButton> _slotRadioButtons = new();

    public PaletteImportDialog(
        List<string> suggestedColors,
        SceneData currentScene,
        ITarget target)
    {
        InitializeComponent();

        TxtDescription.Text = $"This asset uses {suggestedColors.Count} colors. What would you like to do?";

        // Create radio button for each palette slot
        for (int i = 0; i < currentScene.PaletteSlots.Count; i++)
        {
            var slotIndex = i;
            var slot = currentScene.PaletteSlots[i];
            var slotType = target.GetPaletteSlotType(i);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };

            var rb = new RadioButton
            {
                Content = $"Replace Slot {i} ({slotType})",
                Style = (Style)FindResource("RadioButtonDefault"),
                Tag = slotIndex,
                IsChecked = i == 0
            };
            rb.Checked += (s, e) => ChosenSlot = slotIndex;
            _slotRadioButtons.Add(rb);

            panel.Children.Add(rb);

            // Show current slot colors
            var colorPanel = new WrapPanel { Margin = new Thickness(8, 0, 0, 0) };
            for (int c = 0; c < Math.Min(slot.Colors.Count, 16); c++)
            {
                var rect = new Rectangle
                {
                    Width = 16,
                    Height = 16,
                    Fill = ParseHexBrush(slot.Colors[c]),
                    Margin = new Thickness(1)
                };
                colorPanel.Children.Add(rect);
            }
            panel.Children.Add(colorPanel);

            SlotOptionsPanel.Children.Add(panel);
        }

        // Show asset colors preview
        foreach (var hexColor in suggestedColors.Take(16))
        {
            var rect = new Rectangle
            {
                Width = 24,
                Height = 24,
                Fill = ParseHexBrush(hexColor),
                Margin = new Thickness(2)
            };
            ColorPreviewPanel.Children.Add(rect);
        }

        ChosenSlot = 0;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result = RbKeepCurrent.IsChecked == true
            ? PaletteImportResult.KeepCurrent
            : PaletteImportResult.ReplaceSlot;

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private Brush ParseHexBrush(string hex)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFrom(hex) ?? (Brush)FindResource("BrushOnSurface");
        }
        catch
        {
            return (Brush)FindResource("BrushOnSurface");
        }
    }
}
