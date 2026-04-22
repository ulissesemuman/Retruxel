using Retruxel.Core.Commands;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using Retruxel.Tool.AssetImporter;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Views;

public partial class SceneEditorView : UserControl
{
    private RetruxelProject? _project;
    private ITarget? _target;
    private SceneData? _currentScene;
    private readonly List<SceneElement> _elements = [];
    private SceneElement? _selectedElement;
    private bool _isDragging;
    private Point _dragOffset;
    private SceneElement? _draggedElement;
    private ProjectManager? _projectManager;
    private ModuleRegistry? _moduleRegistry;
    private bool _isUpdatingUI;
    private bool _isLoadingProject;

    private readonly UndoRedoStack _undoRedo = new();

    // Drag start position — stored when drag begins, used to create MoveElementCommand on release
    private int _dragStartTileX;
    private int _dragStartTileY;

    public event Action<RetruxelProject>? OnGenerateRomRequested;
    public event Action? OnAboutRequested;

    public SceneEditorView()
    {
        InitializeComponent();
        KeyDown += SceneEditorView_KeyDown;
        Focusable = true;
    }

    public void SetProjectManager(ProjectManager manager) => _projectManager = manager;
    public void SetModuleRegistry(ModuleRegistry registry) => _moduleRegistry = registry;

    private void BtnUndo_Click(object sender, RoutedEventArgs e) => _undoRedo.Undo();
    private void BtnRedo_Click(object sender, RoutedEventArgs e) => _undoRedo.Redo();

    // ── Sidebar tab switching ─────────────────────────────────────────────────

    private void BtnTabModules_Click(object sender, RoutedEventArgs e)
    {
        PanelModules.Visibility = Visibility.Visible;
        PanelAssets.Visibility = Visibility.Collapsed;
        BtnTabModules.Tag = "active";
        BtnTabAssets.Tag = null;
    }

    private void BtnTabAssets_Click(object sender, RoutedEventArgs e)
    {
        PanelModules.Visibility = Visibility.Collapsed;
        PanelAssets.Visibility = Visibility.Visible;
        BtnTabModules.Tag = null;
        BtnTabAssets.Tag = "active";
        RefreshAssetPanel();
    }

    // ── Asset panel ───────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the asset panel from the current project's asset list.
    /// </summary>
    private void RefreshAssetPanel()
    {
        if (_project is null) return;

        AssetTilesPanel.Children.Clear();
        AssetSpritesPanel.Children.Clear();

        foreach (var asset in _project.Assets)
        {
            // Determine panel based on VramRegionId
            var panel = asset.VramRegionId.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        asset.VramRegionId.Contains("bg", StringComparison.OrdinalIgnoreCase) ||
                        asset.VramRegionId.Contains("tiles", StringComparison.OrdinalIgnoreCase)
                ? AssetTilesPanel
                : AssetSpritesPanel;

            panel.Children.Add(BuildAssetRow(asset));
        }
    }

    /// <summary>
    /// Builds a single asset row for the asset panel.
    /// </summary>
    private FrameworkElement BuildAssetRow(AssetEntry asset)
    {
        var border = new Border
        {
            Margin = new Thickness(12, 0, 12, 4),
            Padding = new Thickness(8, 6, 8, 6),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = asset.Id
        };
        border.SetResourceReference(Border.BackgroundProperty, "BrushSurfaceContainerHigh");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Color indicator
        var indicator = new Border
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        indicator.SetResourceReference(Border.BackgroundProperty,
            asset.VramRegionId.Contains("background", StringComparison.OrdinalIgnoreCase) ||
            asset.VramRegionId.Contains("bg", StringComparison.OrdinalIgnoreCase) ||
            asset.VramRegionId.Contains("tiles", StringComparison.OrdinalIgnoreCase)
                ? "BrushSecondary" : "BrushTertiary");
        Grid.SetColumn(indicator, 0);

        // Asset name
        var name = new TextBlock
        {
            Text = asset.Id,
            VerticalAlignment = VerticalAlignment.Center
        };
        name.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        Grid.SetColumn(name, 1);

        // Tile count
        var count = new TextBlock
        {
            Text = $"{asset.TileCount}t",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 10
        };
        count.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        Grid.SetColumn(count, 2);

        grid.Children.Add(indicator);
        grid.Children.Add(name);
        grid.Children.Add(count);
        border.Child = grid;

        // Enable drag from asset panel to canvas
        border.MouseMove += (s, e) =>
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var data = new DataObject("RetruxelAssetDrop", asset);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
            }
        };

        return border;
    }

    private void BtnImportTiles_Click(object sender, RoutedEventArgs e)
        => OpenAssetImporter("background");

    private void BtnImportSprites_Click(object sender, RoutedEventArgs e)
        => OpenAssetImporter("sprites");

    private void OpenAssetImporter(string vramRegionId)
    {
        if (_project is null || _target is null) return;

        var window = new AssetImporterWindow(_target, _project.ProjectPath)
        {
            Owner = Window.GetWindow(this)
        };

        // Pre-select VRAM region
        window.PreSelectRegion(vramRegionId);

        if (window.ShowDialog() == true && window.ImportedAsset is not null)
        {
            // Check for duplicate ID
            if (_project.Assets.Any(a => a.Id == window.ImportedAsset.Id))
            {
                MessageBox.Show(
                    $"An asset named '{window.ImportedAsset.Id}' already exists.",
                    "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _project.Assets.Add(window.ImportedAsset);
            _ = _projectManager?.SaveAsync();
            RefreshAssetPanel();
        }
    }

    // ── New Scene ─────────────────────────────────────────────────────────────

    private void BtnNewScene_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null) return;

        var index = _project.Scenes.Count + 1;
        var newName = $"Scene {index}";
        while (_project.Scenes.Any(s => s.SceneName == newName))
        {
            index++;
            newName = $"Scene {index}";
        }

        var scene = new SceneData { SceneId = Guid.NewGuid().ToString(), SceneName = newName };
        _project.Scenes.Add(scene);
        _ = _projectManager?.SaveAsync();

        RebuildSceneTabs();
        ActivateScene(scene);
    }

    // ── Scene tabs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the scene tab strip from the current project's scene list.
    /// Called on project load and after adding/removing/renaming scenes.
    /// </summary>
    private void RebuildSceneTabs()
    {
        if (_project is null) return;

        SceneTabsPanel.Children.Clear();

        foreach (var scene in _project.Scenes)
            SceneTabsPanel.Children.Add(BuildSceneTab(scene));
    }

    /// <summary>
    /// Builds a single scene tab. Double-click or right-click → Rename/Delete.
    /// </summary>
    private FrameworkElement BuildSceneTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;

        var border = new Border
        {
            Padding = new Thickness(16, 0, 16, 0),
            Cursor = Cursors.Hand,
            Tag = scene.SceneId,
            BorderThickness = new Thickness(0, 0, 0, isActive ? 2 : 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        border.SetResourceReference(Border.BorderBrushProperty, "BrushPrimary");
        border.SetResourceReference(Border.BackgroundProperty,
            isActive ? "BrushSurfaceContainerHigh" : "BrushSurfaceContainerLow");

        border.Child = BuildLabelForTab(scene);

        // Single click → activate, double click → rename
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
                StartInlineRename(border, scene);
            else
                ActivateScene(scene);
        };

        // Right-click context menu: Rename / Delete
        var menu = new ContextMenu();
        var menuRename = new MenuItem { Header = "Rename" };
        var menuDelete = new MenuItem { Header = "Delete" };

        menuRename.Click += (_, _) => StartInlineRename(border, scene);
        menuDelete.Click += (_, _) => DeleteScene(scene);

        // Disable Delete when only one scene remains
        if (_project!.Scenes.Count <= 1)
            menuDelete.IsEnabled = false;

        menu.Items.Add(menuRename);
        menu.Items.Add(menuDelete);
        border.ContextMenu = menu;

        return border;
    }

    private TextBlock BuildLabelForTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;
        var label = new TextBlock
        {
            Text = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.SetResourceReference(TextBlock.StyleProperty, "TextLabel");
        if (isActive)
            label.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        return label;
    }

    /// <summary>
    /// Activates a scene — clears the canvas and reloads elements for the selected scene.
    /// </summary>
    private void ActivateScene(SceneData scene)
    {
        // Save current scene state before switching
        SyncProjectModules();

        // Switch active scene
        _currentScene = scene;

        // Clear canvas and element list
        _elements.Clear();
        SceneCanvas.Children.Clear();
        SelectElement(null);

        // Clear events panel
        LoadEvents();

        // Clear undo stack — history is per-session, not per-scene
        _undoRedo.Clear();

        // Reload elements for the new scene
        LoadFromProject();

        // Refresh palette — singletons already on canvas must be hidden
        RefreshModulePalette();

        // Rebuild tabs to reflect active state
        RebuildSceneTabs();
    }

    /// <summary>
    /// Starts inline rename — replaces tab label with a TextBox.
    /// Confirms on Enter or focus loss, cancels on Esc.
    /// </summary>
    private void StartInlineRename(Border tab, SceneData scene)
    {
        var textBox = new TextBox
        {
            Text = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 60,
            MaxWidth = 160
        };
        textBox.SetResourceReference(TextBox.StyleProperty, "RetruxelTextBox");
        textBox.SelectAll();

        tab.Child = textBox;
        textBox.Focus();

        void Confirm()
        {
            var newName = textBox.Text.Trim();
            if (string.IsNullOrEmpty(newName)
                || _project!.Scenes.Any(s => s.SceneId != scene.SceneId && s.SceneName == newName))
            {
                tab.Child = BuildLabelForTab(scene); // revert
                return;
            }

            scene.SceneName = newName;
            _ = _projectManager?.SaveAsync();
            RebuildSceneTabs();
        }

        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Return) { Confirm(); e.Handled = true; }
            if (e.Key == Key.Escape) { tab.Child = BuildLabelForTab(scene); e.Handled = true; }
        };
        textBox.LostFocus += (_, _) => Confirm();
    }

    /// <summary>
    /// Deletes a scene. No-op when only one scene remains.
    /// </summary>
    private void DeleteScene(SceneData scene)
    {
        if (_project is null || _project.Scenes.Count <= 1) return;

        _project.Scenes.Remove(scene);

        if (_currentScene?.SceneId == scene.SceneId)
            ActivateScene(_project.Scenes[0]);
        else
            RebuildSceneTabs();

        _ = _projectManager?.SaveAsync();
    }

    private void SceneEditorView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _selectedElement is not null)
        {
            RemoveElement(_selectedElement);
            e.Handled = true;
        }

        // Undo: Ctrl+Z
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _undoRedo.Undo();
            e.Handled = true;
        }

        // Redo: Ctrl+Y or Ctrl+Shift+Z
        if ((e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) ||
            (e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
        {
            _undoRedo.Redo();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Updates the enabled state of the Undo/Redo buttons in the top bar.
    /// Subscribed to UndoRedoStack.StateChanged.
    /// </summary>
    private void UpdateUndoRedoButtons()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("BtnUndo") is System.Windows.Controls.Button btnUndo)
            {
                btnUndo.IsEnabled = _undoRedo.CanUndo;
                btnUndo.ToolTip = _undoRedo.CanUndo
                    ? $"Undo: {_undoRedo.NextUndoDescription}"
                    : "Nothing to undo";
            }

            if (FindName("BtnRedo") is System.Windows.Controls.Button btnRedo)
            {
                btnRedo.IsEnabled = _undoRedo.CanRedo;
                btnRedo.ToolTip = _undoRedo.CanRedo
                    ? $"Redo: {_undoRedo.NextRedoDescription}"
                    : "Nothing to redo";
            }
        });
    }

    /// <summary>
    /// Initializes the editor with the given project and target.
    /// </summary>
    public void Initialize(RetruxelProject project, ITarget target)
    {
        _project = project;
        _target = target;

        // Clear previous session data
        _elements.Clear();
        SceneCanvas.Children.Clear();
        _selectedElement = null;

        // Apply undo history limit from settings
        var settings = SettingsService.Load();
        _undoRedo.MaxHistorySize = Math.Clamp(settings.General.UndoHistoryLimit, 20, 100);
        _undoRedo.Clear();
        _undoRedo.StateChanged += UpdateUndoRedoButtons;

        // Get or create main scene
        _currentScene = project.Scenes.FirstOrDefault();
        if (_currentScene is null)
        {
            _currentScene = new SceneData
            {
                SceneId = Guid.NewGuid().ToString(),
                SceneName = "Main",
                Elements = []
            };
            project.Scenes.Add(_currentScene);
        }

        RebuildSceneTabs();
        ApplyTargetSpecs(target);
        LoadModulePalette(target);
        LoadEvents();
        LoadFromProject();
    }

    private void ApplyTargetSpecs(ITarget target)
    {
        var specs = target.Specs;
        SceneCanvas.Width = specs.ScreenWidth;
        SceneCanvas.Height = specs.ScreenHeight;
        TxtCanvasSize.Text = $"{specs.ScreenWidth} × {specs.ScreenHeight} px";
    }

    // ===== MODULE PALETTE =====

    /// <summary>
    /// Populates the left panel with available modules for this target.
    /// </summary>
    private void LoadModulePalette(ITarget target)
    {
        RefreshModulePalette();
    }

    /// <summary>
    /// Rebuilds the module palette sidebar, hiding singleton modules
    /// that are already placed on the current canvas.
    /// Called on scene load and whenever an element is added or removed.
    /// </summary>
    private void RefreshModulePalette()
    {
        ModulePalettePanel.Children.Clear();

        if (_moduleRegistry is null) return;

        // Collect IDs of singleton modules already on the canvas
        var usedSingletons = _elements
            .Where(e => _moduleRegistry.IsModuleSingleton(e.ModuleId))
            .Select(e => e.ModuleId)
            .ToHashSet();

        var modules = new List<(string Category, string ModuleId, string DisplayName)>();

        foreach (var (moduleId, module) in _moduleRegistry.LogicModules)
        {
            var m = (IModule)module;
            if (_moduleRegistry.IsModuleSingleton(moduleId) && usedSingletons.Contains(moduleId)) continue;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        foreach (var (moduleId, module) in _moduleRegistry.GraphicModules)
        {
            var m = (IModule)module;
            if (_moduleRegistry.IsModuleSingleton(moduleId) && usedSingletons.Contains(moduleId)) continue;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        foreach (var (moduleId, module) in _moduleRegistry.AudioModules)
        {
            var m = (IModule)module;
            if (_moduleRegistry.IsModuleSingleton(moduleId) && usedSingletons.Contains(moduleId)) continue;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        var grouped = modules.GroupBy(m => m.Category);

        foreach (var group in grouped)
        {
            var header = new TextBlock
            {
                Text = group.Key,
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(12, 8, 12, 4)
            };
            ModulePalettePanel.Children.Add(header);

            foreach (var module in group)
            {
            var item = BuildModulePaletteItem(module.ModuleId, module.DisplayName);
                ModulePalettePanel.Children.Add(item);
            }
        }
    }

    private Border BuildModulePaletteItem(string moduleId, string displayName)
    {
        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(8, 0, 8, 4),
            Cursor = Cursors.Hand,
            Tag = moduleId
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        panel.Children.Add(new TextBlock
        {
            Text = "⬛ ",
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = displayName,
            Style = (Style)FindResource("TextBody"),
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        item.Child = panel;

        // Enable drag from palette to canvas or events
        item.MouseMove += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(item, moduleId, DragDropEffects.Copy);
            }
        };

        return item;
    }

    // ===== CANVAS =====

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(SceneCanvas);
        TxtCoordinates.Text = $"X: {(int)pos.X} | Y: {(int)pos.Y}";
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Deselect when clicking empty canvas area
        SelectElement(null);
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(typeof(string)) || e.Data.GetDataPresent("RetruxelAssetDrop"))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        var pos = e.GetPosition(SceneCanvas);
        var tileX = (int)pos.X / 8;
        var tileY = (int)pos.Y / 8;

        // Module drag from palette
        if (e.Data.GetData(typeof(string)) is string moduleId)
        {
            AddModuleToCanvas(moduleId, tileX, tileY);
            return;
        }

        // Asset drag from asset panel
        if (e.Data.GetData("RetruxelAssetDrop") is AssetEntry asset)
            DropAssetOnCanvas(asset, tileX, tileY);
    }

    /// <summary>
    /// Handles an asset dropped from the asset panel onto the canvas.
    /// Resolves the correct module for the asset's VRAM region, creates it, and
    /// sets tilesAssetId automatically — no manual typing required.
    ///
    /// Tilemap  ← background/bg/tiles region (module with "tilemap" in ID + tilesAssetId param)
    /// Sprite   ← sprites region (module with "sprite" in ID + tilesAssetId param)
    ///
    /// Singleton check: singletons already on canvas are hidden from the module palette
    /// (handled by RefreshModulePalette). Since TilemapModule and SpriteModule are not
    /// singletons, multiple instances are allowed — consistent with dragging from the palette.
    /// </summary>
    private void DropAssetOnCanvas(AssetEntry asset, int tileX, int tileY)
    {
        var moduleId = ResolveModuleForAsset(asset.VramRegionId);

        if (moduleId is null)
        {
            MessageBox.Show(
                $"No module available for VRAM region '{asset.VramRegionId}'. " +
                $"Make sure the active target supports this asset type.",
                "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create and place the module — uses existing undo stack, events panel, etc.
        AddModuleToCanvas(moduleId, tileX, tileY);

        // The new element is always the last one added
        var element = _elements.LastOrDefault();
        if (element?.Module is not IModule module) return;

        // Inject the asset ID directly into the module
        SetModuleParameterValue(module, "tilesAssetId", asset.Id, ParameterType.String);

        // Refresh the canvas visual (gray box → actual asset image)
        RefreshElementVisual(element);

        // Refresh properties panel to show the pre-filled tilesAssetId
        if (_selectedElement == element)
            BuildPropertiesPanel(element);

        SyncProjectModules();
        _ = _projectManager?.SaveAsync();

        // Open visual tool if available (e.g., tilemap editor)
        OpenVisualToolForElement(element);
    }

    /// <summary>
    /// Finds the appropriate module ID for the given VRAM region by searching
    /// all loaded modules for one whose ID contains the expected keyword
    /// and whose manifest declares a tilesAssetId parameter.
    /// This approach is target-agnostic — works for SMS, GG, NES, etc.
    /// </summary>
    private string? ResolveModuleForAsset(string vramRegionId)
    {
        if (_moduleRegistry is null) return null;

        // Determine keyword based on VRAM region
        var keyword = vramRegionId.Contains("sprite", StringComparison.OrdinalIgnoreCase)
            ? "sprite"
            : "tilemap";

        foreach (var (id, m) in _moduleRegistry.LogicModules)
        {
            if (!id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (m is ILogicModule lm && lm.GetManifest().Parameters.Any(p => p.Name == "tilesAssetId"))
                return id;
        }

        foreach (var (id, m) in _moduleRegistry.GraphicModules)
        {
            if (!id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (m is IGraphicModule gm && gm.GetManifest().Parameters.Any(p => p.Name == "tilesAssetId"))
                return id;
        }

        return null;
    }

    private void Documentation_Click(object sender, RoutedEventArgs e)
        => OnAboutRequested?.Invoke();

    /// <summary>
    /// Adds a module element to the canvas at the given tile position.
    /// Wrapped in an AddElementCommand for undo support.
    /// </summary>
    private void AddModuleToCanvas(string moduleId, int tileX, int tileY)
    {
        var element = CreateSceneElement(moduleId, tileX, tileY);

        var displayName = element.Module is IModule m ? m.DisplayName : moduleId;

        _undoRedo.Push(new AddElementCommand(
            description: $"Add {displayName}",
            add: () =>
            {
                _elements.Add(element);

                var visual = BuildCanvasElement(element);
                Canvas.SetLeft(visual, tileX * 8);
                Canvas.SetTop(visual, tileY * 8);
                SceneCanvas.Children.Add(visual);
                element.CanvasVisual = visual;

                AddModuleToEvent("OnStart", moduleId, element);
                SelectElement(element);
                SyncProjectModules();
                RefreshModulePalette();
            },
            remove: () =>
            {
                RemoveElementCore(element);
                SyncProjectModules();
                RefreshModulePalette();
            }
        ));
    }

    private Border BuildCanvasElement(SceneElement element)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 2, 4, 2),
            Cursor = Cursors.Hand,
            Tag = element
        };

        // Try to render real asset — fallback to label if not available
        var assetVisual = TryBuildAssetVisual(element);
        if (assetVisual is not null)
        {
            border.Padding = new Thickness(0);
            border.Child = assetVisual;
        }
        else
        {
            border.Child = new TextBlock
            {
                Text = element.DisplayLabel,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71))
            };
        }
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                // Double click → open visual tool if available
                OpenVisualToolForElement(element);
                e.Handled = true;
                return;
            }

            SelectElement(element);
            _isDragging = true;
            _dragStartTileX = element.TileX;
            _dragStartTileY = element.TileY;
            var clickPos = e.GetPosition(border);
            _dragOffset = clickPos;
            _draggedElement = element;
            border.CaptureMouse();
            e.Handled = true;
        };

        border.MouseMove += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                var canvasPos = e.GetPosition(SceneCanvas);
                var adjustedX = canvasPos.X - _dragOffset.X;
                var adjustedY = canvasPos.Y - _dragOffset.Y;

                var maxTileX = (int)(SceneCanvas.Width / 8) - 1;
                var maxTileY = (int)(SceneCanvas.Height / 8) - 1;

                var tileX = Math.Clamp((int)adjustedX / 8, 0, maxTileX);
                var tileY = Math.Clamp((int)adjustedY / 8, 0, maxTileY);

                Canvas.SetLeft(border, tileX * 8);
                Canvas.SetTop(border, tileY * 8);

                element.TileX = tileX;
                element.TileY = tileY;

                UpdateModulePosition(element.Module, tileX, tileY);

                if (_selectedElement == element)
                    BuildPropertiesPanel(element);
            }
        };

        border.MouseLeftButtonUp += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                _isDragging = false;
                _draggedElement = null;
                border.ReleaseMouseCapture();

                var endTileX = element.TileX;
                var endTileY = element.TileY;

                // Only register a move command if position actually changed
                if (endTileX != _dragStartTileX || endTileY != _dragStartTileY)
                {
                    var startX = _dragStartTileX;
                    var startY = _dragStartTileY;

                    var displayName = element.Module is IModule m ? m.DisplayName : element.ModuleId;

                    // Push without re-executing — element already moved via MouseMove
                    var cmd = new MoveElementCommand(
                        description: $"Move {displayName}",
                        moveTo: (tx, ty) =>
                        {
                            element.TileX = tx;
                            element.TileY = ty;
                            Canvas.SetLeft(border, tx * 8);
                            Canvas.SetTop(border, ty * 8);
                            UpdateModulePosition(element.Module, tx, ty);
                            SyncProjectModules();
                        },
                        prevX: startX, prevY: startY,
                        newX: endTileX, newY: endTileY);

                    // Add to stack without calling Execute() — already done by drag
                    _undoRedo.PushWithoutExecute(cmd);
                }

                SyncProjectModules();
                e.Handled = true;
            }
        };

        // Right-click context menu
        var contextMenu = new ContextMenu();
        var menuEdit = new MenuItem { Header = "Edit" };
        var menuDelete = new MenuItem { Header = "Delete" };

        menuEdit.Click += (_, _) => OpenVisualToolForElement(element);
        menuDelete.Click += (_, _) => RemoveElement(element);

        // Only show Edit if module has a visual tool
        if (element.Module is IModule mod && !string.IsNullOrEmpty(mod.VisualToolId))
            contextMenu.Items.Add(menuEdit);

        contextMenu.Items.Add(menuDelete);
        border.ContextMenu = contextMenu;

        element.CanvasVisual = border;
        return border;
    }

    // ===== EVENTS PANEL =====

    private void LoadEvents()
    {
        EventsPanel.Children.Clear();

        var triggers = new[] { "OnStart", "OnVBlank", "OnInput_Button1", "OnInput_Button2" };

        foreach (var trigger in triggers)
            AddEventBlock(trigger);
    }

    private void AddEventBlock(string trigger)
    {
        var loc = LocalizationService.Instance;
        var block = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 4),
            Tag = trigger
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var whenLabel = new TextBlock
        {
            Text = loc.Get("scene.event.when"),
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var triggerLabel = new TextBlock
        {
            Text = trigger,
            FontSize = 10,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var actionsPanel = new StackPanel
        {
            Tag = $"actions_{trigger}"
        };

        var addAction = new TextBlock
        {
            Text = loc.Get("scene.event.add_action"),
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 0)
        };
        addAction.MouseLeftButtonDown += (s, e) => ShowModulePicker(trigger);

        Grid.SetColumn(whenLabel, 0);
        Grid.SetColumn(triggerLabel, 1);

        grid.Children.Add(whenLabel);
        grid.Children.Add(triggerLabel);

        var outerPanel = new StackPanel();
        outerPanel.Children.Add(grid);
        outerPanel.Children.Add(actionsPanel);
        outerPanel.Children.Add(addAction);

        block.Child = outerPanel;

        // Allow drop from module palette
        block.AllowDrop = true;
        block.Drop += (s, e) =>
        {
            if (e.Data.GetData(typeof(string)) is string moduleId)
                AddModuleToEvent(trigger, moduleId);
        };

        EventsPanel.Children.Add(block);
    }

    private void ShowModulePicker(string trigger)
    {
        // For now, directly add text.display — will become a picker dialog
        AddModuleToEvent(trigger, "text.display");
    }

    /// <summary>
    /// Adds a module action to an event block.
    /// </summary>
    private void AddModuleToEvent(string trigger, string moduleId,
        SceneElement? existingElement = null)
    {
        var element = existingElement ?? CreateSceneElement(moduleId, 0, 0);
        element.Trigger = trigger;

        if (existingElement is null)
            _elements.Add(element);

        // Find the actions panel for this trigger
        foreach (Border block in EventsPanel.Children)
        {
            if (block.Tag?.ToString() != trigger) continue;
            if (block.Child is not StackPanel outer) continue;

            var actionsPanel = outer.Children.OfType<StackPanel>()
                .FirstOrDefault(p => p.Tag?.ToString() == $"actions_{trigger}");

            if (actionsPanel is null) continue;

            var action = BuildEventAction(element);
            actionsPanel.Children.Add(action);
            break;
        }

        SelectElement(element);

        // Only mark dirty if this is a new element, not during load
        if (existingElement is null)
            SyncProjectModules();
    }

    private Border BuildEventAction(SceneElement element)
    {
        var action = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 2, 0, 2),
            Cursor = Cursors.Hand,
            Tag = element
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = element.DisplayLabel,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var remove = new TextBlock
        {
            Text = "✕",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        remove.MouseLeftButtonDown += (s, e) =>
        {
            RemoveElement(element);
            e.Handled = true;
        };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(remove, 1);
        grid.Children.Add(label);
        grid.Children.Add(remove);
        action.Child = grid;

        action.MouseLeftButtonDown += (s, e) =>
        {
            SelectElement(element);
            e.Handled = true;
        };

        element.EventVisual = action;
        return action;
    }

    // ===== SELECTION & PROPERTIES =====

    private void SelectElement(SceneElement? element)
    {
        // Deselect previous
        if (_selectedElement is not null)
        {
            if (_selectedElement.CanvasVisual is Border cv)
            {
                cv.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
                cv.BorderThickness = new Thickness(1);
            }
            if (_selectedElement.EventVisual is Border ev)
                ev.Background = new SolidColorBrush(
                    Color.FromRgb(0x26, 0x26, 0x26));
        }

        _selectedElement = element;

        if (element is null)
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            PropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        // Highlight selected with thicker yellow border
        if (element.CanvasVisual is Border cvSelected)
        {
            cvSelected.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
            cvSelected.BorderThickness = new Thickness(2);
        }
        if (element.EventVisual is Border evSelected)
            evSelected.Background = new SolidColorBrush(
                Color.FromRgb(0x1E, 0x1E, 0x2E));

        TxtNoSelection.Visibility = Visibility.Collapsed;
        PropertiesPanel.Visibility = Visibility.Visible;

        // Don't build properties panel during project load
        if (!_isLoadingProject)
            BuildPropertiesPanel(element);

        // Ensure keyboard focus for Delete key
        Focus();
    }

    private void BuildPropertiesPanel(SceneElement element)
    {
        _isUpdatingUI = true;
        PropertiesPanel.Children.Clear();

        if (element.Module is not IModule module)
        {
            _isUpdatingUI = false;
            return;
        }

        ModuleManifest? manifest = module switch
        {
            ILogicModule lm => lm.GetManifest(),
            IGraphicModule gm => gm.GetManifest(),
            IAudioModule am => am.GetManifest(),
            _ => null
        };

        if (manifest is null)
        {
            _isUpdatingUI = false;
            return;
        }

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = manifest.ModuleId.ToUpper().Replace(".", " "),
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(0, 0, 0, 16)
        });

        // User ID field (required)
        AddUserIdField(element);

        foreach (var param in manifest.Parameters)
        {
            AddParameterField(element, module, param);
        }

        _isUpdatingUI = false;
    }

    private void AddUserIdField(SceneElement element)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "USER ID *",
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var input = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(0),
            Height = 32,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var textBox = new TextBox
        {
            Text = element.UserId,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var validationText = new TextBlock
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };

        string valueOnFocus = textBox.Text;
        textBox.GotFocus += (s, e) => valueOnFocus = textBox.Text;

        textBox.LostFocus += (s, e) =>
        {
            if (_isUpdatingUI) return;

            var newValue = textBox.Text.Trim();
            var previousValue = valueOnFocus;

            if (previousValue == newValue) return;

            // Validate
            var validation = ValidateUserId(newValue, element.ElementId);
            if (!validation.IsValid)
            {
                validationText.Text = validation.Error;
                textBox.Text = previousValue;
                element.UserId = previousValue;
                return;
            }

            validationText.Text = string.Empty;

            _undoRedo.Push(new ChangePropertyCommand(
                description: $"Change User ID",
                apply: val =>
                {
                    _isUpdatingUI = true;
                    textBox.Text = val;
                    _isUpdatingUI = false;
                    element.UserId = val;
                    UpdateElementLabel(element);
                    SyncProjectModules();
                },
                previousValue: previousValue,
                newValue: newValue));
        };

        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
        PropertiesPanel.Children.Add(validationText);
    }

    private (bool IsValid, string Error) ValidateUserId(string userId, string currentElementId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return (false, "User ID is required");

        if (userId.Length < 2)
            return (false, "User ID must be at least 2 characters");

        if (!char.IsLetter(userId[0]))
            return (false, "User ID must start with a letter");

        if (!userId.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return (false, "User ID can only contain letters, numbers, and underscores");

        // Check uniqueness across ALL scenes in project
        if (_project is not null)
        {
            foreach (var scene in _project.Scenes)
            {
                foreach (var elem in scene.Elements)
                {
                    if (elem.ElementId != currentElementId &&
                        elem.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"User ID '{userId}' already exists in scene '{scene.SceneName}'");
                    }
                }
            }
        }

        return (true, string.Empty);
    }

    private void AddParameterField(SceneElement element, IModule module, ParameterDefinition param)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = param.DisplayName,
            Style = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var input = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(0),
            Height = 32,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var currentValue = GetModuleParameterValue(module, param.Name);
        var textBox = new TextBox
        {
            Text = currentValue?.ToString() ?? param.DefaultValue?.ToString() ?? "",
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (param.Type == ParameterType.String)
            textBox.Loaded += (s, e) => textBox.Focus();

        // Capture value when the TextBox gains focus — this is the "before" value
        string valueOnFocus = textBox.Text;
        textBox.GotFocus += (s, e) => valueOnFocus = textBox.Text;

        textBox.LostFocus += (s, e) =>
        {
            if (_isUpdatingUI) return;

            var previousValue = valueOnFocus;
            var newValue = textBox.Text;

            if (previousValue == newValue) return;

            var displayName = module.DisplayName;
            _undoRedo.Push(new ChangePropertyCommand(
                description: $"Change {param.DisplayName} on {displayName}",
                apply: val =>
                {
                    _isUpdatingUI = true;
                    textBox.Text = val;
                    _isUpdatingUI = false;

                    SetModuleParameterValue(module, param.Name, val, param.Type);

                    if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var x))
                    {
                        element.TileX = x;
                        UpdateElementPosition(element);
                    }
                    else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var y))
                    {
                        element.TileY = y;
                        UpdateElementPosition(element);
                    }

                    UpdateElementLabel(element);
                    RefreshElementVisual(element);
                    SyncProjectModules();
                },
                previousValue: previousValue,
                newValue: newValue));
        };

        textBox.TextChanged += (s, e) =>
        {
            if (!_isUpdatingUI)
            {
                SetModuleParameterValue(module, param.Name, textBox.Text, param.Type);

                if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(textBox.Text, out var x))
                {
                    element.TileX = x;
                    UpdateElementPosition(element);
                }
                else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(textBox.Text, out var y))
                {
                    element.TileY = y;
                    UpdateElementPosition(element);
                }
                RefreshElementVisual(element);
                SyncProjectModules();
            }
        };

        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
    }

    // ===== HELPERS =====

    /// <summary>
    /// Tries to build an Image visual from the asset referenced by the module.
    /// Returns null if the module has no asset reference or the asset file is missing —
    /// the caller falls back to the gray box label in that case.
    /// </summary>
    private UIElement? TryBuildAssetVisual(SceneElement element)
    {
        if (_project is null || element.Module is not IModule module) return null;

        // Extract tilesAssetId from the module JSON
        var assetId = ExtractAssetId(module);
        if (string.IsNullOrEmpty(assetId)) return null;

        // Find asset entry in project
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
        if (asset is null) return null;

        // Resolve absolute path
        var absPath = Path.Combine(_project.ProjectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absPath)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(absPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            var img = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Width = asset.SourceWidth,
                Height = asset.SourceHeight,
                Stretch = System.Windows.Media.Stretch.None,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch
        {
            return null; // Any I/O or decode failure → fall back to gray box
        }
    }

    /// <summary>
    /// Extracts the tilesAssetId from a module's serialized JSON.
    /// Works for TilemapModule and SpriteModule which both use "tilesAssetId".
    /// </summary>
    private static string? ExtractAssetId(IModule module)
    {
        try
        {
            var json = module.Serialize();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tilesAssetId", out var prop))
                return prop.GetString();
        }
        catch { /* ignore */ }

        return null;
    }

    private SceneElement CreateSceneElement(string moduleId, int tileX, int tileY)
    {
        if (_moduleRegistry is null)
            throw new InvalidOperationException("ModuleRegistry not initialized");

        IModule? moduleTemplate = null;

        if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
            moduleTemplate = am;

        if (moduleTemplate is null)
            throw new InvalidOperationException($"Module not found: {moduleId}");

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;

        UpdateModulePosition(module, tileX, tileY);

        return new SceneElement
        {
            ElementId = Guid.NewGuid().ToString(),
            ModuleId = moduleId,
            Module = module,
            TileX = tileX,
            TileY = tileY
        };
    }

    private void UpdateElementLabel(SceneElement element)
    {
        var label = GetElementDisplayLabel(element);

        if (element.CanvasVisual is Border cv && cv.Child is TextBlock ctb)
            ctb.Text = label;
        if (element.EventVisual is Border ev && ev.Child is Grid g &&
            g.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock etb)
            etb.Text = label;
    }

    /// <summary>
    /// Refreshes the canvas visual for an element after its module state changes.
    /// Used when tilesAssetId is updated in the properties panel — switches from
    /// gray box to real asset image (or back if the asset is removed).
    /// </summary>
    private void RefreshElementVisual(SceneElement element)
    {
        if (element.CanvasVisual is not Border border) return;

        var assetVisual = TryBuildAssetVisual(element);
        if (assetVisual is not null)
        {
            border.Padding = new Thickness(0);
            border.Child = assetVisual;
        }
        else
        {
            border.Padding = new Thickness(4, 2, 4, 2);
            border.Child = new TextBlock
            {
                Text = element.DisplayLabel,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71))
            };
        }
    }

    private string GetElementDisplayLabel(SceneElement element)
    {
        if (element.Module is not IModule module)
            return element.ModuleId;

        var textValue = GetModuleParameterValue(module, "text");
        if (textValue is not null)
            return $"[{element.ModuleId.ToUpper()}] {textValue}";

        return element.ModuleId.ToUpper();
    }

    private void UpdateElementPosition(SceneElement element)
    {
        if (element.CanvasVisual is Border border)
        {
            Canvas.SetLeft(border, element.TileX * 8);
            Canvas.SetTop(border, element.TileY * 8);
        }
    }

    private void RemoveElement(SceneElement element)
    {
        var displayName = element.Module is IModule m ? m.DisplayName : element.ModuleId;

        // Capture event panel reference before removal
        Border? eventBlock = null;
        foreach (Border block in EventsPanel.Children)
        {
            if (block.Child is not StackPanel outer) continue;
            var actionsPanel = outer.Children.OfType<StackPanel>().FirstOrDefault();
            if (actionsPanel?.Children.Contains(element.EventVisual) == true)
            {
                eventBlock = block;
                break;
            }
        }

        _undoRedo.Push(new RemoveElementCommand(
            description: $"Remove {displayName}",
            remove: () =>
            {
                RemoveElementCore(element);
                SyncProjectModules();
                RefreshModulePalette();
            },
            restore: () =>
            {
                _elements.Add(element);

                if (element.CanvasVisual is not null)
                {
                    Canvas.SetLeft(element.CanvasVisual, element.TileX * 8);
                    Canvas.SetTop(element.CanvasVisual, element.TileY * 8);
                    SceneCanvas.Children.Add(element.CanvasVisual);
                }

                if (element.EventVisual is not null && eventBlock?.Child is StackPanel sp)
                {
                    var ap = sp.Children.OfType<StackPanel>().FirstOrDefault();
                    ap?.Children.Add(element.EventVisual);
                }

                SyncProjectModules();
                RefreshModulePalette();
            }
        ));
    }

    /// <summary>
    /// Core removal logic — removes from all collections without creating an undo entry.
    /// Called by both RemoveElement (via command) and Undo of AddElementCommand.
    /// </summary>
    private void RemoveElementCore(SceneElement element)
    {
        _elements.Remove(element);

        if (element.CanvasVisual is not null)
            SceneCanvas.Children.Remove(element.CanvasVisual);

        foreach (Border block in EventsPanel.Children)
        {
            if (block.Child is not StackPanel outer) continue;
            var actionsPanel = outer.Children.OfType<StackPanel>().FirstOrDefault();
            if (element.EventVisual is not null)
                actionsPanel?.Children.Remove(element.EventVisual);
        }

        if (_selectedElement == element)
            SelectElement(null);
    }

    /// <summary>
    /// Syncs the current scene elements back to the project model.
    /// </summary>
    private void SyncProjectModules()
    {
        if (_project is null || _currentScene is null) return;

        _currentScene.Elements = _elements.Select(e => new SceneElementData
        {
            ElementId = e.ElementId,
            UserId = e.UserId,
            ModuleId = e.ModuleId,
            TileX = e.TileX,
            TileY = e.TileY,
            Trigger = e.Trigger,
            ModuleState = e.Module is Retruxel.Core.Interfaces.IModule module
                ? module.Serialize()
                : string.Empty
        }).ToList();

        _project.DefaultModules = _elements
            .Select(e => e.ModuleId)
            .Distinct()
            .ToList();

        _projectManager?.MarkDirty();
    }

    private void GenerateRom_Click(object sender, RoutedEventArgs e)
    {
        SyncProjectModules();
        if (_project is not null)
            OnGenerateRomRequested?.Invoke(_project);
    }

    /// <summary>
    /// Loads scene elements from the project and reconstructs the UI.
    /// </summary>
    private void LoadFromProject()
    {
        if (_currentScene is null || _currentScene.Elements.Count == 0)
            return;

        _isLoadingProject = true;

        foreach (var elementData in _currentScene.Elements)
        {
            try
            {
                var module = DeserializeModule(elementData.ModuleId, elementData.ModuleState);
                if (module is null) continue;

                var element = new SceneElement
                {
                    ElementId = elementData.ElementId,
                    UserId = elementData.UserId,
                    ModuleId = elementData.ModuleId,
                    Module = module,
                    TileX = elementData.TileX,
                    TileY = elementData.TileY,
                    Trigger = elementData.Trigger
                };

                UpdateModulePosition(module, elementData.TileX, elementData.TileY);

                _elements.Add(element);

                // Add to canvas if it has position
                if (elementData.TileX >= 0 && elementData.TileY >= 0)
                {
                    var visual = BuildCanvasElement(element);
                    Canvas.SetLeft(visual, element.TileX * 8);
                    Canvas.SetTop(visual, element.TileY * 8);
                    SceneCanvas.Children.Add(visual);
                }

                // Add to event panel
                if (!string.IsNullOrEmpty(elementData.Trigger))
                {
                    AddModuleToEvent(elementData.Trigger, elementData.ModuleId, element);
                }
            }
            catch
            {
                // Skip corrupted elements
            }
        }

        _isLoadingProject = false;

        // If there are elements, select the first one and show properties
        if (_elements.Count > 0)
        {
            SelectElement(_elements[0]);
            BuildPropertiesPanel(_elements[0]);
        }
    }

    /// <summary>
    /// Deserializes a module from its JSON state.
    /// </summary>
    private IModule? DeserializeModule(string moduleId, string moduleState)
    {
        if (_moduleRegistry is null) return null;

        IModule? moduleTemplate = null;

        if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
            moduleTemplate = am;

        if (moduleTemplate is null) return null;

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;

        if (!string.IsNullOrEmpty(moduleState))
            module.Deserialize(moduleState);

        return module;
    }

    private void UpdateModulePosition(object? module, int x, int y)
    {
        if (module is not IModule imodule) return;
        SetModuleParameterValue(imodule, "x", x.ToString(), ParameterType.Int);
        SetModuleParameterValue(imodule, "y", y.ToString(), ParameterType.Int);
    }

    private object? GetModuleParameterValue(IModule module, string paramName)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        return prop?.GetValue(module);
    }

    private void SetModuleParameterValue(IModule module, string paramName, string value, ParameterType type)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        if (prop is null) return;

        try
        {
            object? convertedValue = type switch
            {
                ParameterType.Int => int.TryParse(value, out var i) ? i : null,
                ParameterType.Float => float.TryParse(value, out var f) ? f : null,
                ParameterType.Bool => bool.TryParse(value, out var b) ? b : null,
                ParameterType.String => value,
                _ => value
            };

            if (convertedValue is not null)
                prop.SetValue(module, convertedValue);
        }
        catch
        {
            // Ignore conversion errors
        }
    }

    /// <summary>
    /// Opens the visual tool for an element if the module has one.
    /// </summary>
    private void OpenVisualToolForElement(SceneElement element)
    {
        if (_project is null || _target is null || _currentScene is null) return;
        if (element.Module is not IModule module) return;
        if (string.IsNullOrEmpty(module.VisualToolId)) return;

        // Find corresponding SceneElementData
        var elementData = _currentScene.Elements.FirstOrDefault(e => e.ElementId == element.ElementId);

        VisualToolInvoker.OpenVisualTool(
            module,
            _target,
            _project,
            _project.ProjectPath,
            _currentScene,
            elementData);

        // Refresh UI after tool closes
        RefreshElementVisual(element);
        if (_selectedElement == element)
            BuildPropertiesPanel(element);
        SyncProjectModules();
    }
}

/// <summary>
/// Represents an element placed in the scene — a module instance with position and visuals.
/// </summary>
public class SceneElement
{
    public string ElementId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public object? Module { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Trigger { get; set; } = "OnStart";
    public UIElement? CanvasVisual { get; set; }
    public UIElement? EventVisual { get; set; }

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(UserId))
                return $"[{UserId}] {ModuleId.ToUpper()}";

            if (Module is IModule module)
            {
                var textValue = module.GetType().GetProperty("Text",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase)?.GetValue(module);
                if (textValue is not null)
                    return $"[{ModuleId.ToUpper()}] {textValue}";
            }
            return ModuleId.ToUpper();
        }
    }
}