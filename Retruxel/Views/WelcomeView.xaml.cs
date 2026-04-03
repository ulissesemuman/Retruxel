using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Target.SMS;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class WelcomeView : UserControl
{
    private bool _isGridView = true;
    private readonly List<ITarget> _targets = [new SmsTarget()];

    public WelcomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RenderConsoles();
        RenderRecentProjects();
    }

    // Renders console cards in grid or list mode
    private void RenderConsoles()
    {
        ConsolesPanel.Children.Clear();

        foreach (var target in _targets)
        {
            var card = _isGridView
                ? BuildGridCard(target)
                : BuildListCard(target);

            ConsolesPanel.Children.Add(card);
        }
    }

    // Grid card — large card with specs
    private Border BuildGridCard(ITarget target)
    {
        var card = new Border
        {
            Width = 220,
            Height = 200,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 16, 16),
            Cursor = System.Windows.Input.Cursors.Hand,
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

        // Console name
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

        // Bottom accent line — color per target
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

        card.MouseLeftButtonDown += (_, _) => OnTargetSelected(target);
        return card;
    }

    // List card — compact horizontal row
    private Border BuildListCard(ITarget target)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(16, 12, 16, 12),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock
        {
            Text = target.DisplayName.ToUpper(),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };

        var specs = new TextBlock
        {
            Text = $"{target.Specs.CPU} | {target.Specs.RamBytes / 1024}KB RAM | {target.Specs.SoundChip}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var arch = new TextBlock
        {
            Text = $"{target.Specs.CPU.Split(' ').Last()} ARCH",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(name, 0);
        Grid.SetColumn(specs, 1);
        Grid.SetColumn(arch, 2);

        grid.Children.Add(name);
        grid.Children.Add(specs);
        grid.Children.Add(arch);
        card.Child = grid;

        card.MouseLeftButtonDown += (_, _) => OnTargetSelected(target);
        return card;
    }

    private void RenderRecentProjects()
    {
        // Placeholder — will load from ProjectManager
        var placeholder = new TextBlock
        {
            Text = "No recent projects.",
            Style = (Style)FindResource("TextBody"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        RecentProjectsPanel.Children.Add(placeholder);
    }

    private void OnTargetSelected(ITarget target)
    {
        // Will open New Project wizard
        MessageBox.Show($"Target selected: {target.DisplayName}");
    }

    private void BtnGrid_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = true;
        BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
        BtnList.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        RenderConsoles();
    }

    private void BtnList_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = false;
        BtnList.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
        BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        RenderConsoles();
    }
}