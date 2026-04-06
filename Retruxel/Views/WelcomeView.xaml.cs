using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class WelcomeView : UserControl
{
    private bool _isGridView = true;
    private Border? _dropOverlay;
    private string _currentSort = "name";
    private string _currentFilter = "all";
    private HashSet<string> _favoriteTargets = new();
    
    public event Action<RetruxelProject>? OnProjectCreated;
    public event Action? OnAboutRequested;

    public WelcomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadFavorites();
        InitializeSortAndFilter();
        RenderTargets();
        RenderRecentProjects();
        RenderSidebarRecentProjects();
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

    private void InitializeSortAndFilter()
    {
        var loc = LocalizationService.Instance;
        
        // Sort options
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.name"), Tag = "name" });
        SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.manufacturer"), Tag = "manufacturer" });
        SortComboBox.SelectedIndex = 0;

        // Filter options - dynamic based on manufacturers
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.all"), Tag = "all" });
        FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.favorites"), Tag = "favorites" });
        
        foreach (var manufacturer in TargetRegistry.GetManufacturers().OrderBy(m => m))
        {
            var key = $"welcome.filter.{manufacturer.ToLower()}";
            var displayName = loc.Get(key);
            FilterComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = manufacturer.ToLower() });
        }
        
        FilterComboBox.SelectedIndex = 0;
    }

    private void Sort_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedItem is ComboBoxItem item && item.Tag is string sortType)
        {
            _currentSort = sortType;
            RenderTargets();
        }
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (FilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string filterType)
        {
            _currentFilter = filterType;
            RenderTargets();
        }
    }

    // Renders target cards in grid or list mode
    private void RenderTargets()
    {
        var allTargets = TargetRegistry.GetAllTargets().ToList();

        // Apply filter
        var filteredTargets = _currentFilter switch
        {
            "favorites" => allTargets.Where(t => _favoriteTargets.Contains(t.TargetId)).ToList(),
            "all" => allTargets,
            _ => allTargets.Where(t => t.Specs.Manufacturer?.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase) == true).ToList()
        };

        // Apply sort
        var sortedTargets = _currentSort switch
        {
            "manufacturer" => filteredTargets.OrderBy(t => t.Specs.Manufacturer).ThenBy(t => t.DisplayName).ToList(),
            _ => filteredTargets.OrderBy(t => t.DisplayName).ToList()
        };

        // Update count
        TargetCount.Text = $"COUNT: {sortedTargets.Count:D2}";

        Panel panel = _isGridView
            ? new WrapPanel { Orientation = Orientation.Horizontal }
            : new StackPanel();

        foreach (var target in sortedTargets)
        {
            var card = _isGridView
                ? BuildGridCard(target)
                : BuildListCard(target);

            panel.Children.Add(card);
        }

        TargetsPanel.Content = panel;
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
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = $"{target.Specs.CPU.Split(' ').Last()} ARCH",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                FontFamily = new FontFamily("Consolas")
            }
        };

        var isFavorite = _favoriteTargets.Contains(target.TargetId);
        var starButton = new Button
        {
            Content = isFavorite ? "★" : "☆",
            FontSize = 20,
            Background = Brushes.Transparent,
            Foreground = isFavorite ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) : new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            Tag = target.TargetId
        };
        starButton.Click += ToggleFavorite_Click;

        Grid.SetColumn(arch, 0);
        Grid.SetColumn(starButton, 1);
        topGrid.Children.Add(arch);
        topGrid.Children.Add(starButton);

        // Middle: Console name and specs
        var contentPanel = new StackPanel();
        var name = new TextBlock
        {
            Text = target.DisplayName.ToUpper(),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
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

        contentPanel.Children.Add(name);
        contentPanel.Children.Add(specs);

        // Bottom accent line
        var accent = new Border
        {
            Height = 3,
            Background = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(-16, 0, -16, -16)
        };

        Grid.SetRow(topGrid, 0);
        Grid.SetRow(contentPanel, 1);
        Grid.SetRow(accent, 2);

        mainGrid.Children.Add(topGrid);
        mainGrid.Children.Add(contentPanel);
        mainGrid.Children.Add(accent);
        card.Child = mainGrid;

        card.MouseLeftButtonDown += (_, _) => OnTargetSelected(target);
        return card;
    }

    private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string targetId)
            return;

        if (_favoriteTargets.Contains(targetId))
        {
            _favoriteTargets.Remove(targetId);
            button.Content = "☆";
            button.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        }
        else
        {
            _favoriteTargets.Add(targetId);
            button.Content = "★";
            button.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
        }

        SaveFavorites();
    }

    // List card — compact horizontal row
    private Border BuildListCard(ITarget target)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(16, 12, 16, 12),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Favorite star
        var isFavorite = _favoriteTargets.Contains(target.TargetId);
        var starButton = new Button
        {
            Content = isFavorite ? "★" : "☆",
            FontSize = 16,
            Background = Brushes.Transparent,
            Foreground = isFavorite ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) : new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            Cursor = System.Windows.Input.Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Tag = target.TargetId
        };
        starButton.Click += ToggleFavorite_Click;

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

        Grid.SetColumn(starButton, 0);
        Grid.SetColumn(name, 1);
        Grid.SetColumn(specs, 2);
        Grid.SetColumn(arch, 3);

        grid.Children.Add(starButton);
        grid.Children.Add(name);
        grid.Children.Add(specs);
        grid.Children.Add(arch);
        card.Child = grid;

        card.MouseLeftButtonDown += (_, _) => OnTargetSelected(target);
        return card;
    }

    private void RenderRecentProjects()
    {
        RecentProjectsPanel.Children.Clear();
        
        var settings = SettingsService.Load();
        var recentProjects = settings.General.RecentProjects
            .Where(File.Exists)
            .ToList();

        if (recentProjects.Count == 0)
        {
            // Show empty state - hide targets and show centered welcome
            PageTitle.Visibility = Visibility.Collapsed;
            PageDescription.Visibility = Visibility.Collapsed;
            TargetHeader.Visibility = Visibility.Collapsed;
            TargetCardsSection.Visibility = Visibility.Collapsed;
            RecentProjectsHeader.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        // Show recent projects and targets
        PageTitle.Visibility = Visibility.Visible;
        PageDescription.Visibility = Visibility.Visible;
        TargetHeader.Visibility = Visibility.Visible;
        TargetCardsSection.Visibility = Visibility.Visible;
        RecentProjectsHeader.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;

        // Use WrapPanel for grid mode, StackPanel for list mode
        if (_isGridView)
        {
            var wrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var projectPath in recentProjects)
            {
                wrapPanel.Children.Add(BuildRecentProjectGridCard(projectPath));
            }
            RecentProjectsPanel.Children.Add(wrapPanel);
        }
        else
        {
            foreach (var projectPath in recentProjects)
            {
                RecentProjectsPanel.Children.Add(BuildRecentProjectListCard(projectPath));
            }
        }
    }

    private Border BuildRecentProjectGridCard(string projectPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        
        // Try to load project to get target info
        string targetLabel = "UNKNOWN";
        try
        {
            var json = File.ReadAllText(projectPath);
            var project = System.Text.Json.JsonSerializer.Deserialize<RetruxelProject>(json);
            if (project != null)
            {
                var target = TargetRegistry.GetTargetById(project.TargetId);
                targetLabel = target?.TargetId.ToUpper() ?? project.TargetId.ToUpper();
            }
        }
        catch { /* Ignore errors, show UNKNOWN */ }

        var card = new Border
        {
            Width = 220,
            Height = 200,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 16, 16),
            Cursor = System.Windows.Input.Cursors.Hand,
            Padding = new Thickness(16),
            Tag = projectPath
        };

        var panel = new StackPanel();
        
        // Target badge at top right
        var targetBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = targetLabel,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
                FontFamily = new FontFamily("Consolas")
            }
        };
        panel.Children.Add(targetBadge);

        panel.Children.Add(new TextBlock
        {
            Text = projectName.ToUpper(),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = projectDir,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap
        });

        // Bottom accent line
        var accent = new Border
        {
            Height = 3,
            Background = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
            Margin = new Thickness(-16, 0, -16, -16),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        panel.Children.Add(accent);

        card.Child = panel;
        card.MouseLeftButtonDown += RecentProject_Click;
        return card;
    }

    private Border BuildRecentProjectListCard(string projectPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        
        // Try to load project to get target info
        string targetLabel = "UNKNOWN";
        try
        {
            var json = File.ReadAllText(projectPath);
            var project = System.Text.Json.JsonSerializer.Deserialize<RetruxelProject>(json);
            if (project != null)
            {
                var target = TargetRegistry.GetTargetById(project.TargetId);
                targetLabel = target?.TargetId.ToUpper() ?? project.TargetId.ToUpper();
            }
        }
        catch { /* Ignore errors, show UNKNOWN */ }

        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = projectPath
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = projectName.ToUpper(),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        panel.Children.Add(new TextBlock
        {
            Text = projectDir,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        
        var targetBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = targetLabel,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
                FontFamily = new FontFamily("Consolas")
            }
        };
        
        Grid.SetColumn(panel, 0);
        Grid.SetColumn(targetBadge, 1);
        grid.Children.Add(panel);
        grid.Children.Add(targetBadge);

        card.Child = grid;
        card.MouseLeftButtonDown += RecentProject_Click;
        return card;
    }

    private async void RecentProject_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border card || card.Tag is not string projectPath)
            return;

        try
        {
            var manager = new ProjectManager();
            var project = await manager.LoadAsync(projectPath);
            OnProjectCreated?.Invoke(project);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            MessageBox.Show(
                string.Format(loc.Get("welcome.error.load"), ex.Message),
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnTargetSelected(ITarget target)
    {
        var dialog = new NewProjectDialog(target) { Owner = Window.GetWindow(this) };

        if (dialog.ShowDialog() == true && dialog.CreatedProject is not null)
            OnProjectCreated?.Invoke(dialog.CreatedProject);
    }

    private void BtnGrid_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = true;
        BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
        BtnList.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        RenderTargets();
        RenderRecentProjects();
    }

    private void BtnList_Click(object sender, RoutedEventArgs e)
    {
        _isGridView = false;
        BtnList.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        BtnList.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
        BtnGrid.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        BtnGrid.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
        RenderTargets();
        RenderRecentProjects();
    }

    private void Documentation_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => OnAboutRequested?.Invoke();

    private void About_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => OnAboutRequested?.Invoke();

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        // Open target selection dialog
        var dialog = new TargetSelectionDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.SelectedTarget != null)
        {
            OnTargetSelected(dialog.SelectedTarget);
        }
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Retruxel Project (*.rtrxproject)|*.rtrxproject",
            Title = loc.Get("welcome.open_project.title")
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var manager = new ProjectManager();
                var project = await manager.LoadAsync(dialog.FileName);
                OnProjectCreated?.Invoke(project);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(loc.Get("welcome.error.load"), ex.Message),
                    "Retruxel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void RenderSidebarRecentProjects()
    {
        SidebarRecentProjects.Children.Clear();
        
        var settings = SettingsService.Load();
        var recentProjects = settings.General.RecentProjects
            .Where(File.Exists)
            .Take(3) // Max 3 in sidebar
            .ToList();

        if (recentProjects.Count == 0)
        {
            var placeholder = new TextBlock
            {
                Text = LocalizationService.Instance.Get("welcome.no_recent"),
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            SidebarRecentProjects.Children.Add(placeholder);
            return;
        }

        foreach (var projectPath in recentProjects)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            
            var item = new Border
            {
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = projectPath,
                Background = System.Windows.Media.Brushes.Transparent
            };

            var text = new TextBlock
            {
                Text = projectName,
                Style = (Style)FindResource("TextBody"),
                Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            item.Child = text;
            item.MouseLeftButtonDown += RecentProject_Click;
            
            // Hover effect
            item.MouseEnter += (s, e) =>
            {
                item.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
                text.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            };
            item.MouseLeave += (s, e) =>
            {
                item.Background = System.Windows.Media.Brushes.Transparent;
                text.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA));
            };

            SidebarRecentProjects.Children.Add(item);
        }
    }

    /// <summary>
    /// Refreshes the recent projects list. Called when returning from SceneEditor.
    /// </summary>
    public void RefreshRecentProjects()
    {
        RenderRecentProjects();
        RenderSidebarRecentProjects();
    }

    private void MainContent_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1 && Path.GetExtension(files[0]).Equals(".rtrxproject", StringComparison.OrdinalIgnoreCase))
            {
                e.Effects = DragDropEffects.Copy;
                ShowDropOverlay();
                return;
            }
        }
        e.Effects = DragDropEffects.None;
    }

    private void MainContent_DragLeave(object sender, DragEventArgs e)
    {
        HideDropOverlay();
    }

    private async void MainContent_Drop(object sender, DragEventArgs e)
    {
        HideDropOverlay();

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length != 1)
            return;

        var filePath = files[0];
        if (!Path.GetExtension(filePath).Equals(".rtrxproject", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var manager = new ProjectManager();
            var project = await manager.LoadAsync(filePath);
            OnProjectCreated?.Invoke(project);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            MessageBox.Show(
                string.Format(loc.Get("welcome.error.load"), ex.Message),
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowDropOverlay()
    {
        if (_dropOverlay != null)
            return;

        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.Content is not Grid rootGrid)
            return;

        _dropOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 0x0E, 0x0E, 0x0E)),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = LocalizationService.Instance.Get("welcome.drop_overlay"),
                        FontSize = 32,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 16)
                    },
                    new Border
                    {
                        Width = 200,
                        Height = 200,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                        BorderThickness = new Thickness(3),
                        Child = new TextBlock
                        {
                            Text = "↓",
                            FontSize = 120,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            },
            IsHitTestVisible = false
        };

        rootGrid.Children.Add(_dropOverlay);
    }

    private void HideDropOverlay()
    {
        if (_dropOverlay == null)
            return;

        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.Content is Grid rootGrid)
        {
            rootGrid.Children.Remove(_dropOverlay);
        }
        _dropOverlay = null;
    }
}