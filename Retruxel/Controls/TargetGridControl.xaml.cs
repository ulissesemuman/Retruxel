using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Controls;

public partial class TargetGridControl : UserControl
{
    public static event Action? FavoritesChanged;
    
    public event Action<ITarget>? TargetSelected;

    private string _currentSort = "name";
    private string _currentFilter = "all";
    private bool _isGridView = true;
    private HashSet<string> _favoriteTargets = new();

    public TargetGridControl()
    {
        InitializeComponent();
        Loaded += (s, e) => Initialize();
    }

    private void Initialize()
    {
        LoadFavorites();
        ApplyLocalization();
        UpdateViewModeButtons();
        RenderTargets();
        
        FavoritesChanged += OnFavoritesChanged;
    }

    private void OnFavoritesChanged()
    {
        LoadFavorites();
        RenderTargets();
    }

    private void LoadFavorites()
    {
        var settings = SettingsService.Load();
        _favoriteTargets = new HashSet<string>(settings.General.FavoriteTargets);
    }

    private void SaveFavorites()
    {
        var settings = SettingsService.Load();
        settings.General.FavoriteTargets = _favoriteTargets.ToList();
        SettingsService.Save(settings);
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance;

        TxtSortLabel.Text = loc.Get("welcome.sort");
        TxtFilterLabel.Text = loc.Get("welcome.filter");

        SortComboBox.Items.Clear();
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.name"), Tag = "name" });
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.manufacturer"), Tag = "manufacturer" });
        SortComboBox.SelectedIndex = 0;

        FilterComboBox.Items.Clear();
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.all"), Tag = "all" });
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.favorites"), Tag = "favorites" });

        var manufacturers = TargetRegistry.GetManufacturers().OrderBy(m => m);
        foreach (var manufacturer in manufacturers)
        {
            var key = $"welcome.filter.{manufacturer.ToLower()}";
            var displayName = loc.Get(key);
            FilterComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = manufacturer.ToLower() });
        }
        FilterComboBox.SelectedIndex = 0;

        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        var loc = LocalizationService.Instance;
        
        TxtSortLabel.Text = loc.Get("welcome.sort");
        TxtFilterLabel.Text = loc.Get("welcome.filter");

        var currentSort = _currentSort;
        var currentFilter = _currentFilter;

        SortComboBox.Items.Clear();
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.name"), Tag = "name" });
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.manufacturer"), Tag = "manufacturer" });
        SortComboBox.SelectedIndex = currentSort == "manufacturer" ? 1 : 0;

        FilterComboBox.Items.Clear();
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.all"), Tag = "all" });
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.favorites"), Tag = "favorites" });

        var manufacturers = TargetRegistry.GetManufacturers().OrderBy(m => m);
        int selectedIndex = 0;
        int index = 2;
        foreach (var manufacturer in manufacturers)
        {
            var key = $"welcome.filter.{manufacturer.ToLower()}";
            var displayName = loc.Get(key);
            FilterComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = manufacturer.ToLower() });
            
            if (manufacturer.ToLower() == currentFilter)
                selectedIndex = index;
            index++;
        }
        
        if (currentFilter == "all")
            FilterComboBox.SelectedIndex = 0;
        else if (currentFilter == "favorites")
            FilterComboBox.SelectedIndex = 1;
        else
            FilterComboBox.SelectedIndex = selectedIndex;
    }

    private void RenderTargets()
    {
        TargetsPanel.Children.Clear();

        var targets = TargetRegistry.GetAllTargets();

        if (_currentFilter == "favorites")
        {
            targets = targets.Where(t => _favoriteTargets.Contains(t.TargetId)).ToList();
        }
        else if (_currentFilter != "all")
        {
            targets = targets.Where(t => t.Specs.Manufacturer.Equals(_currentFilter, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        targets = _currentSort switch
        {
            "manufacturer" => targets.OrderBy(t => t.Specs.Manufacturer).ThenBy(t => t.DisplayName).ToList(),
            _ => targets.OrderBy(t => t.DisplayName).ToList(),
        };

        var targetList = targets.ToList();

        if (_isGridView)
        {
            var wrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var target in targetList)
            {
                var card = BuildGridCard(target);
                wrapPanel.Children.Add(card);
            }
            TargetsPanel.Children.Add(wrapPanel);
        }
        else
        {
            foreach (var target in targetList)
            {
                var card = BuildListCard(target);
                TargetsPanel.Children.Add(card);
            }
        }
    }

    private Border BuildGridCard(ITarget target)
    {
        var card = new Border
        {
            Width = 220,
            Height = 200,
            Background = (Brush)FindResource("BrushSurfaceContainerHigh"),
            Margin = new Thickness(0, 0, 8, 16),
            Cursor = Cursors.Hand,
            Padding = new Thickness(16)
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Top row: Architecture tag + Favorite star
        var topGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var arch = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHighest"),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = $"{target.Specs.CPU.Split(' ').Last()} ARCH",
                Style = (Style)FindResource("TextCode"),
                Foreground = (Brush)FindResource("BrushTertiary")
            }
        };

        var isFavorite = _favoriteTargets.Contains(target.TargetId);
        var starButton = new TextBlock
        {
            Text = isFavorite ? "★" : "☆",
            FontSize = 20,
            Foreground = isFavorite ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) : new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = target.TargetId
        };
        starButton.MouseLeftButtonDown += (s, e) =>
        {
            ToggleFavorite(target.TargetId);
            e.Handled = true;
        };

        Grid.SetColumn(arch, 0);
        Grid.SetColumn(starButton, 1);
        topGrid.Children.Add(arch);
        topGrid.Children.Add(starButton);

        // Middle: Console name and specs
        var panel = new StackPanel();
        var name = new TextBlock
        {
            Text = target.DisplayName.ToUpper(),
            Style = (Style)FindResource("TextTitle"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var specs = new TextBlock
        {
            Text = $"{target.Specs.CPU} | {target.Specs.RamBytes / 1024}KB RAM",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap
        };

        panel.Children.Add(name);
        panel.Children.Add(specs);

        // Bottom accent line
        var accent = new Border
        {
            Height = 3,
            Background = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(-16, 16, -16, -16),
            VerticalAlignment = VerticalAlignment.Bottom
        };

        Grid.SetRow(topGrid, 0);
        Grid.SetRow(panel, 1);
        Grid.SetRow(accent, 2);

        mainGrid.Children.Add(topGrid);
        mainGrid.Children.Add(panel);
        mainGrid.Children.Add(accent);
        card.Child = mainGrid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24));
        card.MouseLeave += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

        card.MouseLeftButtonDown += (_, _) => TargetSelected?.Invoke(target);

        return card;
    }

    private void ToggleFavorite(string targetId)
    {
        if (_favoriteTargets.Contains(targetId))
            _favoriteTargets.Remove(targetId);
        else
            _favoriteTargets.Add(targetId);

        SaveFavorites();
        RenderTargets();
        
        FavoritesChanged?.Invoke();
    }

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
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Favorite star
        var isFavorite = _favoriteTargets.Contains(target.TargetId);
        var starButton = new TextBlock
        {
            Text = isFavorite ? "★" : "☆",
            FontSize = 24,
            Foreground = isFavorite ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) : new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            Cursor = System.Windows.Input.Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Tag = target.TargetId
        };
        starButton.MouseLeftButtonDown += (s, e) =>
        {
            ToggleFavorite(target.TargetId);
            e.Handled = true;
        };

        var name = new TextBlock
        {
            Text = target.DisplayName.ToUpper(),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };
        Grid.SetColumn(name, 1);

        var specs = new TextBlock
        {
            Text = $"{target.Specs.CPU} | {target.Specs.RamBytes / 1024}KB RAM",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(specs, 2);

        var arch = new TextBlock
        {
            Text = $"{target.Specs.CPU.Split(' ').Last()} ARCH",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arch, 3);

        Grid.SetColumn(starButton, 0);
        grid.Children.Add(starButton);
        grid.Children.Add(name);
        grid.Children.Add(specs);
        grid.Children.Add(arch);
        card.Child = grid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24));
        card.MouseLeave += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

        card.MouseLeftButtonDown += (_, _) => TargetSelected?.Invoke(target);

        return card;
    }

    private void UpdateViewModeButtons()
    {
        if (_isGridView)
        {
            BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
            BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
            BtnList.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        }
        else
        {
            BtnList.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
            BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
            BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        }
    }

    private void Sort_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedItem is ComboBoxItem item)
        {
            _currentSort = item.Tag?.ToString() ?? "name";
            RenderTargets();
        }
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (FilterComboBox.SelectedItem is ComboBoxItem item)
        {
            _currentFilter = item.Tag?.ToString() ?? "all";
            RenderTargets();
        }
    }

    private void BtnGrid_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = true;
        UpdateViewModeButtons();
        RenderTargets();
    }

    private void BtnList_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = false;
        UpdateViewModeButtons();
        RenderTargets();
    }
}
