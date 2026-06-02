using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Module picker dialog — lists all modules compatible with the current target,
/// filtered by scope and singleton policy.
///
/// Usage:
///   ModulePickerWindow.Open(
///       registry:       _moduleRegistry,
///       scope:          isGlobal ? ModuleScope.Project : ModuleScope.Scene,
///       existing:       _elements.Select(e => e.ModuleId).ToList(),
///       projectModules: _project.Modules.Select(m => m.ModuleId).ToList(),
///       onSelected:     moduleId => AddModuleToScene(moduleId));
/// </summary>
public partial class ModulePickerWindow : Window
{
    // ── State ──────────────────────────────────────────────────────────────────

    private ModuleRegistry?      _registry;
    private ModuleScope          _scope;
    private List<string>         _existing      = [];
    private List<string>         _projectModules = [];
    private Action<string>?      _onSelected;

    private string?              _selectedModuleId;
    private string               _activeCategory = "ALL";
    private string               _searchText     = string.Empty;

    // All modules flattened for display
    private List<ModulePickerItem> _allItems = [];

    // ── Static factory ─────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the module picker as a modal dialog.
    /// </summary>
    /// <param name="registry">The module registry loaded for the current target.</param>
    /// <param name="scope">
    ///   Project → show only modules with DefaultScope == Project.
    ///   Scene   → show all modules; project-level ones appear with OVERRIDE badge.
    /// </param>
    /// <param name="existing">Module IDs already present at the target level (project or scene).</param>
    /// <param name="projectModules">Module IDs already added at project level (used for OVERRIDE badge).</param>
    /// <param name="onSelected">Callback invoked with the chosen moduleId when the user confirms.</param>
    /// <param name="owner">Owner window for centering.</param>
    public static void Open(
        ModuleRegistry  registry,
        ModuleScope     scope,
        List<string>    existing,
        List<string>    projectModules,
        Action<string>  onSelected,
        Window?         owner = null)
    {
        try
        {
            var window = new ModulePickerWindow
            {
                _registry       = registry,
                _scope          = scope,
                _existing       = existing,
                _projectModules = projectModules,
                _onSelected     = onSelected
            };

            if (owner is not null && owner.IsLoaded)
                window.Owner = owner;

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModulePickerWindow] Error: {ex}");
            System.Windows.MessageBox.Show(
                $"Failed to open Module Picker:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "Retruxel", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    // ── Constructor ────────────────────────────────────────────────────────────

    public ModulePickerWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_registry is null) return;

        BuildItemList();
        BuildCategoryButtons();
        UpdateScopeLabel();
        RefreshList();
        TxtSearch.Focus();
    }

    // ── Build item list ────────────────────────────────────────────────────────

    private void BuildItemList()
    {
        _allItems.Clear();

        // Collect all modules from the registry
        var allModules = new List<IModule>();
        allModules.AddRange(_registry!.GraphicModules.Values);
        allModules.AddRange(_registry.LogicModules.Values);
        allModules.AddRange(_registry.AudioModules.Values);

        foreach (var module in allModules.OrderBy(m => m.Category).ThenBy(m => m.DisplayName))
        {
            // Exclude modules with dedicated tree sections (Plane, Entity)
            if (module.Type == ModuleType.Plane ||
                module.Type == ModuleType.Entity)
                continue;

            // Scope filter: when opening from global MODULES, show only Project-scope modules
            if (_scope == ModuleScope.Project && module.DefaultScope != ModuleScope.Project)
                continue;

            var policy   = _registry.GetModulePolicy(module.ModuleId);
            var isInScene = _existing.Contains(module.ModuleId, StringComparer.OrdinalIgnoreCase);
            var isInProject = _projectModules.Contains(module.ModuleId, StringComparer.OrdinalIgnoreCase);

            // Singleton Global already added → disabled
            bool disabled = policy == SingletonPolicy.Global && isInScene;

            // Override badge: scene context + module already exists at project level
            bool isOverride = _scope == ModuleScope.Scene && isInProject;

            _allItems.Add(new ModulePickerItem(module, policy, disabled, isOverride));
        }
    }

    // ── Category sidebar ───────────────────────────────────────────────────────

    private void BuildCategoryButtons()
    {
        CategoryPanel.Children.Clear();

        var categories = new[] { "ALL" }
            .Concat(_allItems.Select(i => i.Module.Category).Distinct().OrderBy(c => c))
            .ToList();

        foreach (var cat in categories)
            CategoryPanel.Children.Add(BuildCategoryButton(cat));
    }

    private Border BuildCategoryButton(string category)
    {
        var isActive = category == _activeCategory;

        var btn = new Border
        {
            Padding = new Thickness(16, 10, 16, 10),
            Cursor  = Cursors.Hand,
            Background = isActive
                ? (Brush)FindResource("BrushPrimary")
                : Brushes.Transparent
        };

        var icon = CategoryIcon(category);
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(new TextBlock
        {
            Text              = icon,
            FontSize          = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 8, 0),
            Foreground        = isActive
                ? (Brush)FindResource("BrushOnPrimary")
                : (Brush)FindResource("BrushOnSurfaceVariant")
        });
        stack.Children.Add(new TextBlock
        {
            Text              = category.ToUpperInvariant(),
            FontSize          = 10,
            FontFamily        = new FontFamily("Inter"),
            FontWeight        = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground        = isActive
                ? (Brush)FindResource("BrushOnPrimary")
                : (Brush)FindResource("BrushOnSurfaceVariant")
        });

        btn.Child = stack;
        btn.MouseLeftButtonDown += (_, e) =>
        {
            _activeCategory = category;
            BuildCategoryButtons();
            RefreshList();
            e.Handled = true;
        };

        // Hover effect for inactive buttons
        if (!isActive)
        {
            btn.MouseEnter += (_, _) => btn.Background = (Brush)FindResource("BrushSurfaceContainerHighest");
            btn.MouseLeave += (_, _) => btn.Background = Brushes.Transparent;
        }

        return btn;
    }

    private static string CategoryIcon(string category) => category.ToUpperInvariant() switch
    {
        "ALL"        => "◈",
        "LOGIC"      => "⚙",
        "GRAPHICS"   => "▣",
        "AUDIO"      => "♪",
        "BACKGROUND" => "▣",
        "SPRITES"    => "◉",
        "PHYSICS"    => "⊕",
        "MUSIC"      => "♫",
        _            => "◆"
    };

    // ── Scope label ────────────────────────────────────────────────────────────

    private void UpdateScopeLabel()
    {
        TxtScopeLabel.Text = _scope == ModuleScope.Project ? "PROJECT SCOPE" : "SCENE SCOPE";
    }

    // ── List rendering ─────────────────────────────────────────────────────────

    private void RefreshList()
    {
        ModuleListPanel.Children.Clear();

        var filtered = _allItems.Where(item =>
        {
            // Category filter
            if (_activeCategory != "ALL" &&
                !item.Module.Category.Equals(_activeCategory, StringComparison.OrdinalIgnoreCase))
                return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var q = _searchText.Trim();
                return item.Module.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || item.Module.ModuleId.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || item.Module.Category.Contains(q, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }).ToList();

        if (filtered.Count == 0)
        {
            ModuleListPanel.Children.Add(new TextBlock
            {
                Text       = "No modules match the current filter.",
                FontSize   = 12,
                FontFamily = new FontFamily("Inter"),
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
                Margin     = new Thickness(0, 24, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return;
        }

        foreach (var item in filtered)
            ModuleListPanel.Children.Add(BuildModuleCard(item));
    }

    private Border BuildModuleCard(ModulePickerItem item)
    {
        bool isSelected = item.Module.ModuleId == _selectedModuleId;

        var card = new Border
        {
            Padding    = new Thickness(16),
            Margin     = new Thickness(0, 0, 0, 8),
            Cursor     = item.Disabled ? Cursors.Arrow : Cursors.Hand,
            Opacity    = item.Disabled ? 0.45 : 1.0,
            Background = isSelected
                ? (Brush)FindResource("BrushSurfaceContainerHighest")
                : (Brush)FindResource("BrushSurfaceContainerLow"),
            BorderBrush = isSelected
                ? (Brush)FindResource("BrushPrimary")
                : (Brush)FindResource("BrushSurfaceContainerHigh"),
            BorderThickness = new Thickness(1)
        };

        // ── Card grid: content | badge ──
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: name + description
        var leftStack = new StackPanel();

        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text       = item.Module.DisplayName,
            FontSize   = 15,
            FontFamily = new FontFamily("Space Grotesk"),
            FontWeight = FontWeights.Bold,
            Foreground = isSelected
                ? (Brush)FindResource("BrushPrimary")
                : (Brush)FindResource("BrushOnSurface")
        });

        // OVERRIDE badge inline with name
        if (item.IsOverride)
        {
            nameRow.Children.Add(new Border
            {
                Margin          = new Thickness(8, 0, 0, 0),
                Padding         = new Thickness(6, 1, 6, 1),
                Background      = new SolidColorBrush(Color.FromArgb(0x22, 0x81, 0xEC, 0xFF)),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(0x44, 0x81, 0xEC, 0xFF)),
                BorderThickness = new Thickness(1),
                Child           = new TextBlock
                {
                    Text       = "OVERRIDE",
                    FontSize   = 8,
                    FontFamily = new FontFamily("Inter"),
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("BrushTertiary")
                }
            });
        }

        leftStack.Children.Add(nameRow);

        // Description (module ID as subtitle for now — modules can expose Description later)
        leftStack.Children.Add(new TextBlock
        {
            Text       = item.Module.ModuleId,
            FontSize   = 11,
            FontFamily = new FontFamily("Inter"),
            Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
            Margin     = new Thickness(0, 4, 0, 0)
        });

        // Tags row: category + scope
        var tagsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        tagsRow.Children.Add(BuildTag($"#{item.Module.Category.ToUpperInvariant()}"));
        tagsRow.Children.Add(BuildTag($"#{item.Module.DefaultScope.ToString().ToUpperInvariant()}"));
        if (item.Policy != SingletonPolicy.Multiple)
            tagsRow.Children.Add(BuildTag($"#{item.Policy.ToString().ToUpperInvariant()}"));
        leftStack.Children.Add(tagsRow);

        Grid.SetColumn(leftStack, 0);
        grid.Children.Add(leftStack);

        // Right: status badge
        var badgeStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };

        var (badgeText, badgeFg, badgeBg) = GetBadgeStyle(item);
        badgeStack.Children.Add(new Border
        {
            Padding         = new Thickness(8, 2, 8, 2),
            Background      = badgeBg,
            BorderThickness = new Thickness(0),
            Child           = new TextBlock
            {
                Text       = badgeText,
                FontSize   = 9,
                FontFamily = new FontFamily("Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = badgeFg
            }
        });

        Grid.SetColumn(badgeStack, 1);
        grid.Children.Add(badgeStack);

        card.Child = grid;

        // ── Interaction ──
        if (!item.Disabled)
        {
            card.MouseEnter += (_, _) =>
            {
                if (item.Module.ModuleId != _selectedModuleId)
                {
                    card.Background   = (Brush)FindResource("BrushSurface");
                    card.BorderBrush  = (Brush)FindResource("BrushPrimary");
                }
            };
            card.MouseLeave += (_, _) =>
            {
                if (item.Module.ModuleId != _selectedModuleId)
                {
                    card.Background  = (Brush)FindResource("BrushSurfaceContainerLow");
                    card.BorderBrush = (Brush)FindResource("BrushSurfaceContainerHigh");
                }
            };
            card.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    SelectModule(item.Module.ModuleId);
                    Confirm();
                }
                else
                {
                    SelectModule(item.Module.ModuleId);
                }
                e.Handled = true;
            };
        }

        return card;
    }

    private static TextBlock BuildTag(string text) => new()
    {
        Text       = text,
        FontSize   = 9,
        FontFamily = new FontFamily("Inter"),
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
        Margin     = new Thickness(0, 0, 12, 0),
        Opacity    = 0.6
    };

    private static (string text, Brush fg, Brush bg) GetBadgeStyle(ModulePickerItem item)
    {
        if (item.Disabled)
            return ("JÁ ADICIONADO",
                new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
                new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)));

        if (item.IsOverride)
            return ("OVERRIDE",
                new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
                new SolidColorBrush(Color.FromArgb(0x22, 0x81, 0xEC, 0xFF)));

        return item.Policy switch
        {
            SingletonPolicy.Global   => ("GLOBAL",
                new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                new SolidColorBrush(Color.FromArgb(0x18, 0x8E, 0xFF, 0x71))),
            SingletonPolicy.PerScene => ("PER SCENE",
                new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
                new SolidColorBrush(Color.FromArgb(0x18, 0x8E, 0xFF, 0x71))),
            _                        => ("MULTIPLE",
                new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
                new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)))
        };
    }

    // ── Selection ──────────────────────────────────────────────────────────────

    private void SelectModule(string moduleId)
    {
        _selectedModuleId = moduleId;
        BtnAdd.IsEnabled  = true;

        // Update status bar
        var item = _allItems.FirstOrDefault(i => i.Module.ModuleId == moduleId);
        TxtStatus.Text = item is not null
            ? $"[{item.Module.DisplayName}  ·  {item.Module.ModuleId}]"
            : "—";

        // Rebuild list to reflect selection highlight
        RefreshList();
    }

    // ── Confirm / cancel ───────────────────────────────────────────────────────

    private void Confirm()
    {
        if (_selectedModuleId is null) return;
        _onSelected?.Invoke(_selectedModuleId);
        DialogResult = true;
        Close();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e) => Confirm();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Search ─────────────────────────────────────────────────────────────────

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = TxtSearch.Text;
        TxtSearchPlaceholder.Visibility =
            string.IsNullOrEmpty(_searchText) ? Visibility.Visible : Visibility.Collapsed;
        RefreshList();
    }

    // ── Drag ───────────────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}

// ── Data model ─────────────────────────────────────────────────────────────────

/// <summary>
/// View model for a single module entry in the picker list.
/// </summary>
internal sealed record ModulePickerItem(
    IModule         Module,
    SingletonPolicy Policy,
    bool            Disabled,
    bool            IsOverride);
