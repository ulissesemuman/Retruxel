using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Target.SMS;
using Retruxel.Target.NES;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class WelcomeView : UserControl
{
    private bool _isGridView = true;
    private readonly List<ITarget> _targets = [new SmsTarget(), new NesTarget()];
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
        RenderTargets();
        RenderRecentProjects();
        RenderSidebarRecentProjects();
    }

    // Renders target cards in grid or list mode
    private void RenderTargets()
    {
        TargetsPanel.Children.Clear();

        foreach (var target in _targets)
        {
            var card = _isGridView
                ? BuildGridCard(target)
                : BuildListCard(target);

            TargetsPanel.Children.Add(card);
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
                var target = _targets.FirstOrDefault(t => t.TargetId == project.TargetId);
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
                var target = _targets.FirstOrDefault(t => t.TargetId == project.TargetId);
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
            MessageBox.Show(
                $"Failed to load project:\n{ex.Message}",
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
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Retruxel Project (*.rtrxproject)|*.rtrxproject",
            Title = "Open Retruxel Project"
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
                    $"Failed to load project:\n{ex.Message}",
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
                Text = "No recent projects",
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
            MessageBox.Show(
                $"Failed to load project:\n{ex.Message}",
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
                        Text = "DROP .RTRXPROJECT FILE",
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