using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class WelcomeView : UserControl
{
    private bool _isGridView = true;
    private Border? _dropOverlay;

    public event Action<RetruxelProject>? OnProjectCreated;
    public event Action? OnAboutRequested;

    public WelcomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Connect TargetGridControl event
        TargetGrid.TargetSelected += OnTargetSelected;

        UpdateTargetCount();
        RenderRecentProjects();
        RenderSidebarRecentProjects();
    }

    private void UpdateTargetCount()
    {
        var count = TargetRegistry.GetAllTargets().Count;
        TargetCount.Text = count.ToString();
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
            RecentProjectsHeader.Visibility = Visibility.Collapsed;
            TargetGrid.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        // Show recent projects and targets
        PageTitle.Visibility = Visibility.Visible;
        PageDescription.Visibility = Visibility.Visible;
        TargetHeader.Visibility = Visibility.Visible;
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
        var targetLabel = ResolveTargetLabel(projectPath);

        var card = new Border
        {
            Width = 220,
            Height = 200,
            Background = (Brush)FindResource("BrushSurfaceContainerHigh"),
            Margin = new Thickness(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            Padding = new Thickness(16),
            Tag = projectPath
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Target badge at top right
        var targetBadge = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHighest"),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = targetLabel,
                Style = (Style)FindResource("TextCode"),
                Foreground = (Brush)FindResource("BrushTertiary")
            }
        };

        var contentPanel = new StackPanel();
        contentPanel.Children.Add(new TextBlock
        {
            Text = projectName.ToUpper(),
            Style = (Style)FindResource("TextTitle"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        contentPanel.Children.Add(new TextBlock
        {
            Text = projectDir,
            Style = (Style)FindResource("TextLabel"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap
        });

        // Bottom accent line
        var accent = new Border
        {
            Height = 3,
            Background = (Brush)FindResource("BrushTertiary"),
            Margin = new Thickness(-16, 16, -16, -16),
            VerticalAlignment = VerticalAlignment.Bottom
        };

        Grid.SetRow(targetBadge, 0);
        Grid.SetRow(contentPanel, 1);
        Grid.SetRow(accent, 2);

        mainGrid.Children.Add(targetBadge);
        mainGrid.Children.Add(contentPanel);
        mainGrid.Children.Add(accent);
        card.Child = mainGrid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = (Brush)FindResource("BrushSurfaceContainerHighest");
        card.MouseLeave += (s, e) => card.Background = (Brush)FindResource("BrushSurfaceContainerHigh");

        card.MouseLeftButtonDown += RecentProject_Click;
        return card;
    }

    private Border BuildRecentProjectListCard(string projectPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var targetLabel = ResolveTargetLabel(projectPath);

        var card = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHigh"),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
            Tag = projectPath
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = projectName.ToUpper(),
            Style = (Style)FindResource("TextTitle")
        });

        panel.Children.Add(new TextBlock
        {
            Text = projectDir,
            Style = (Style)FindResource("TextLabel"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var targetBadge = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHighest"),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = targetLabel,
                Style = (Style)FindResource("TextCode"),
                Foreground = (Brush)FindResource("BrushTertiary")
            }
        };

        Grid.SetColumn(panel, 0);
        Grid.SetColumn(targetBadge, 1);
        grid.Children.Add(panel);
        grid.Children.Add(targetBadge);

        card.Child = grid;

        // Hover effect
        card.MouseEnter += (s, e) => card.Background = (Brush)FindResource("BrushSurfaceContainerHighest");
        card.MouseLeave += (s, e) => card.Background = (Brush)FindResource("BrushSurfaceContainerHigh");
        card.MouseLeftButtonDown += RecentProject_Click;
        return card;
    }

    // Helpers

    private string ResolveTargetLabel(string projectPath)
    {
        try
        {
            var json = File.ReadAllText(projectPath);
            var project = System.Text.Json.JsonSerializer.Deserialize<RetruxelProject>(json);
            if (project != null)
            {
                var target = TargetRegistry.GetTargetById(project.TargetId);
                return target?.TargetId.ToUpper() ?? project.TargetId.ToUpper();
            }
        }
        catch { /* Ignore errors, show UNKNOWN */ }
        return "UNKNOWN";
    }

    // Handler para Button.Click
    private void RecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string projectPath)
            LoadRecentProject(projectPath);
    }

    // Handler para Border.MouseLeftButtonDown
    private void RecentProject_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string projectPath)
            LoadRecentProject(projectPath);
    }

    // Método comum que faz o trabalho real
    private async void LoadRecentProject(string projectPath)
    {
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

    private void Documentation_Click(object sender, RoutedEventArgs e)
        => OnAboutRequested?.Invoke();

    private void About_Click(object sender, RoutedEventArgs e)
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

            var button = new Button
            {
                Content = Path.GetFileNameWithoutExtension(projectPath),
                Tag = projectPath,
                Style = (Style)FindResource("ButtonToolbar"),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += RecentProject_Click;

            SidebarRecentProjects.Children.Add(button);
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
                        Style = (Style)FindResource("TextDisplay"),
                        Foreground = (Brush)FindResource("BrushPrimary"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 16)
                    },
                    new Border
                    {
                        Width = 200,
                        Height = 200,
                        BorderBrush = (Brush)FindResource("BrushPrimary"),
                        BorderThickness = new Thickness(3),
                        Child = new TextBlock
                        {
                            Text = "↓",
                            Style = (Style)FindResource("TextDisplay"),
                            FontSize = 120,
                            Foreground = (Brush)FindResource("BrushPrimary"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            },
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