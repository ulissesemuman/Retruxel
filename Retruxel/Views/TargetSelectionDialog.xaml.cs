using Retruxel.Core.Interfaces;
using Retruxel.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class TargetSelectionDialog : Window
{
    public ITarget? SelectedTarget { get; private set; }

    public TargetSelectionDialog()
    {
        InitializeComponent();
        ApplyLocalization();
        RenderTargets();
    }

    private void ApplyLocalization()
    {
        var loc = Retruxel.Core.Services.LocalizationService.Instance;
        Title = loc.Get("targetselection.title");
        TxtDialogTitle.Text = loc.Get("targetselection.title");
        TxtDescription.Text = loc.Get("targetselection.description");
        BtnCancel.Content = loc.Get("targetselection.cancel");
    }

    private void RenderTargets()
    {
        TargetsPanel.Children.Clear();

        foreach (var target in TargetRegistry.GetAllTargets())
        {
            var card = BuildTargetCard(target);
            TargetsPanel.Children.Add(card);
        }
    }

    private Border BuildTargetCard(ITarget target)
    {
        var card = new Border
        {
            Width = 220,
            Height = 200,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            Padding = new Thickness(16)
        };

        var panel = new StackPanel();

        // Architecture tag
        var arch = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = $"{target.Specs.CPU.Split(' ').Last()} ARCH",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                FontFamily = new FontFamily("Consolas")
            }
        };

        // Target name
        var name = new TextBlock
        {
            Text = target.DisplayName.ToUpper(),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // Specs
        var specs = new TextBlock
        {
            Text = $"{target.Specs.CPU} | {target.Specs.RamBytes / 1024}KB RAM",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap
        };

        // Bottom accent line
        var accent = new Border
        {
            Height = 3,
            Background = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(-16, 16, -16, -16),
            VerticalAlignment = VerticalAlignment.Bottom
        };

        panel.Children.Add(arch);
        panel.Children.Add(name);
        panel.Children.Add(specs);
        panel.Children.Add(accent);
        card.Child = panel;

        card.MouseLeftButtonDown += (_, _) =>
        {
            SelectedTarget = target;
            DialogResult = true;
            Close();
        };

        return card;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
