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
    private StateManager? _stateManager;
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

    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
        
        // Initialize StateManager when ProjectManager is set
        if (_stateManager == null)
        {
            _stateManager = new StateManager(manager, _undoRedo);
            _stateManager.SavingStateChanged += OnSavingStateChanged;
            _stateManager.StateChanged += () => SyncProjectModules();
        }
    }

    /// <summary>
    /// Animates the save indicator when saving.
    /// </summary>
    private void OnSavingStateChanged(bool isSaving)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("TxtSaveIndicator") is not TextBlock indicator) return;

            if (isSaving)
            {
                // Pulse animation: fade to full opacity
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.3,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    AutoReverse = false
                };
                indicator.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            else
            {
                // Fade back to dim
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.3,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = false
                };
                indicator.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        });
    }
    public void SetModuleRegistry(ModuleRegistry registry) => _moduleRegistry = registry;

    /// <summary>
    /// Adds a module element to the current scene from external tools (e.g., TilemapEditor creating a palette).
    /// This method deserializes the module, creates the visual element, and adds it to the scene.
    /// </summary>
    public void AddElementFromData(SceneElementData elementData)
    {
        if (_currentScene is null || _moduleRegistry is null) return;

        try
        {
            var module = DeserializeModule(elementData.ModuleId, elementData.ModuleState);
            if (module is null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize module: {elementData.ModuleId}");
                return;
            }

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

            System.Diagnostics.Debug.WriteLine($"Added element: {elementData.ModuleId}, Trigger: {elementData.Trigger}, TileX: {elementData.TileX}, TileY: {elementData.TileY}");

            // Determine if module should be on canvas or only in events
            // Graphic modules (tilemap, sprite) go on canvas
            // Logic modules without visual representation (palette, text) only go in events
            bool isGraphicModule = _moduleRegistry.GraphicModules.ContainsKey(elementData.ModuleId);
            bool hasCanvasPosition = elementData.TileX >= 0 && elementData.TileY >= 0;

            if (isGraphicModule && hasCanvasPosition)
            {
                var visual = BuildCanvasElement(element);
                Canvas.SetLeft(visual, element.TileX * 8);
                Canvas.SetTop(visual, element.TileY * 8);
                SceneCanvas.Children.Add(visual);
                element.CanvasVisual = visual;
            }

            // Add to event panel if it has a trigger
            if (!string.IsNullOrEmpty(elementData.Trigger))
            {
                AddModuleToEvent(elementData.Trigger, elementData.ModuleId, element);
            }

            RefreshModulePalette();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in AddElementFromData: {ex.Message}");
        }
    }

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
            FontSize = 10,
            Margin = new Thickness(0, 0, 8, 0)
        };
        count.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        Grid.SetColumn(count, 2);
        
        // Delete button
        var deleteBtn = new TextBlock
        {
            Text = "✕",
            FontSize = 12,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Delete asset"
        };
        deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        deleteBtn.MouseEnter += (s, e) => deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
        deleteBtn.MouseLeave += (s, e) => deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        deleteBtn.MouseLeftButtonDown += (s, e) =>
        {
            e.Handled = true;
            DeleteAsset(asset);
        };
        Grid.SetColumn(deleteBtn, 3);

        grid.Children.Add(indicator);
        grid.Children.Add(name);
        grid.Children.Add(count);
        grid.Children.Add(deleteBtn);
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

            var asset = window.ImportedAsset;

            // Create state change for importing asset (Large change — auto-saves)
            var change = new StateChange
            {
                Description = $"Import asset '{asset.Id}'",
                Type = ChangeType.Large,
                Execute = () =>
                {
                    _project.Assets.Add(asset);
                    RefreshAssetPanel();
                },
                IsUndoable = false // Asset import is not undoable (file already copied)
            };

            _stateManager?.ApplyChange(change);
        }
    }
    
    /// <summary>
    /// Deletes an asset from the project after confirmation.
    /// Checks if the asset is in use by any modules before deletion.
    /// </summary>
    private void DeleteAsset(AssetEntry asset)
    {
        if (_project is null) return;
        
        // Check if asset is in use
        var usedBy = new List<string>();
        foreach (var scene in _project.Scenes)
        {
            foreach (var element in scene.Elements)
            {
                if (element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Null)
                    continue;
                    
                if (element.ModuleState.TryGetProperty("tilesAssetId", out var assetId))
                {
                    if (assetId.GetString() == asset.Id)
                    {
                        var label = !string.IsNullOrEmpty(element.UserId) ? element.UserId : element.ElementId[..8];
                        usedBy.Add($"{scene.SceneName}/{label}");
                    }
                }
            }
        }
        
        if (usedBy.Count > 0)
        {
            var modules = string.Join("\n  • ", usedBy);
            var result = MessageBox.Show(
                $"Asset '{asset.Id}' is currently used by:\n  • {modules}\n\nDelete anyway?",
                "Asset In Use",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        else
        {
            var result = MessageBox.Show(
                $"Delete asset '{asset.Id}'?\n\nThis action cannot be undone.",
                "Delete Asset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        
        // Create state change for deleting asset (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Delete asset '{asset.Id}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Assets.Remove(asset);
                RefreshAssetPanel();
            },
            IsUndoable = false // Asset deletion not undoable (file operations)
        };

        _stateManager?.ApplyChange(change);
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

        // Create state change for new scene (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Create scene '{newName}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Scenes.Add(scene);
                RebuildSceneTabs();
                ActivateScene(scene);
            },
            IsUndoable = false // Scene creation not undoable (would need to track all elements)
        };

        _stateManager?.ApplyChange(change);
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

            var oldName = scene.SceneName;

            // Create state change for renaming scene (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = $"Rename scene '{oldName}' to '{newName}'",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    scene.SceneName = newName;
                    RebuildSceneTabs();
                },
                IsUndoable = false // Scene rename not undoable (low priority)
            };

            _stateManager?.ApplyChange(change);
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

        // Create state change for deleting scene (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Delete scene '{scene.SceneName}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Scenes.Remove(scene);

                if (_currentScene?.SceneId == scene.SceneId)
                    ActivateScene(_project.Scenes[0]);
                else
                    RebuildSceneTabs();
            },
            IsUndoable = false // Scene deletion not undoable
        };

        _stateManager?.ApplyChange(change);
    }

    private void SceneEditorView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _selectedElement is not null)
        {
            RemoveElement(_selectedElement);
            e.Handled = true;
        }

        // Ctrl+S: Save project
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = _stateManager?.SaveNowAsync();
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

    /// <summary>
    /// Cleanup when closing the editor.
    /// </summary>
    public void Cleanup()
    {
        _stateManager?.Dispose();
        _stateManager = null;
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
    /// Rebuilds the module palette sidebar, disabling modules
    /// that have reached their instance limit (singleton or MaxInstances).
    /// Called on scene load and whenever an element is added or removed.
    /// </summary>
    private void RefreshModulePalette()
    {
        ModulePalettePanel.Children.Clear();

        if (_moduleRegistry is null) return;

        // Count instances of each module on the canvas
        var instanceCounts = _elements
            .GroupBy(e => e.ModuleId)
            .ToDictionary(g => g.Key, g => g.Count());

        var modules = new List<(string Category, string ModuleId, string DisplayName, bool IsDisabled)>();

        foreach (var (moduleId, module) in _moduleRegistry.LogicModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.GraphicModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.AudioModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
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
                var item = BuildModulePaletteItem(module.ModuleId, module.DisplayName, module.IsDisabled);
                ModulePalettePanel.Children.Add(item);
            }
        }
    }

    private Border BuildModulePaletteItem(string moduleId, string displayName, bool isDisabled)
    {
        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(8, 0, 8, 4),
            Cursor = isDisabled ? Cursors.Arrow : Cursors.Hand,
            Tag = moduleId,
            Opacity = isDisabled ? 0.5 : 1.0
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        // Red square for disabled, green for enabled
        panel.Children.Add(new TextBlock
        {
            Text = "⬛ ",
            Foreground = new SolidColorBrush(isDisabled 
                ? Color.FromRgb(0xFF, 0x52, 0x52)  // Red
                : Color.FromRgb(0x8E, 0xFF, 0x71)), // Green
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = displayName,
            Style = (Style)FindResource("TextBody"),
            Foreground = isDisabled 
                ? new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75))  // Gray
                : Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        item.Child = panel;

        // Only enable drag if not disabled
        if (!isDisabled)
        {
            item.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragDrop.DoDragDrop(item, moduleId, DragDropEffects.Copy);
                }
            };
        }

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
    /// Uses StateManager for centralized state management.
    /// </summary>
    private void AddModuleToCanvas(string moduleId, int tileX, int tileY)
    {
        var element = CreateSceneElement(moduleId, tileX, tileY);
        var displayName = element.Module is IModule m ? m.DisplayName : moduleId;

        // Create state change for adding element (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Add {displayName}",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _elements.Add(element);

                var visual = BuildCanvasElement(element);
                Canvas.SetLeft(visual, tileX * 8);
                Canvas.SetTop(visual, tileY * 8);
                SceneCanvas.Children.Add(visual);
                element.CanvasVisual = visual;

                AddModuleToEvent("OnStart", moduleId, element);
                SelectElement(element);
                RefreshModulePalette();
                
                // Open visual tool if available
                if (element.Module is IModule mod && !string.IsNullOrEmpty(mod.VisualToolId))
                {
                    Dispatcher.InvokeAsync(() => OpenVisualToolForElement(element), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            },
            IsUndoable = true,
            UndoCommand = new AddElementCommand(
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
                    RefreshModulePalette();
                    if (element.Module is IModule mod && !string.IsNullOrEmpty(mod.VisualToolId))
                    {
                        Dispatcher.InvokeAsync(() => OpenVisualToolForElement(element), 
                            System.Windows.Threading.DispatcherPriority.Background);
                    }
                },
                remove: () =>
                {
                    RemoveElementCore(element);
                    RefreshModulePalette();
                }
            )
        };

        _stateManager?.ApplyChange(change);
    }

    private Border BuildCanvasElement(SceneElement element)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Tag = element
        };

        // Try to render real asset — fallback to label if not available
        var assetVisual = TryBuildAssetVisual(element);
        if (assetVisual is not null)
        {
            border.Child = assetVisual;
        }
        else
        {
            border.Padding = new Thickness(4, 2, 4, 2);
            border.Child = BuildModuleLabel(element);
        }
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                // Double click → cancel any drag and open visual tool
                _isDragging = false;
                _draggedElement = null;
                border.ReleaseMouseCapture();
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

                // Logic modules can be placed anywhere (even outside canvas)
                // Graphic modules are clamped to canvas bounds
                bool isLogicModule = _moduleRegistry?.LogicModules.ContainsKey(element.ModuleId) ?? false;
                
                int tileX, tileY;
                if (isLogicModule)
                {
                    // Allow negative positions and positions beyond canvas
                    tileX = (int)(adjustedX / 8);
                    tileY = (int)(adjustedY / 8);
                }
                else
                {
                    // Clamp graphic modules to canvas bounds
                    var maxTileX = (int)(SceneCanvas.Width / 8) - 1;
                    var maxTileY = (int)(SceneCanvas.Height / 8) - 1;
                    tileX = Math.Clamp((int)(adjustedX / 8), 0, maxTileX);
                    tileY = Math.Clamp((int)(adjustedY / 8), 0, maxTileY);
                }

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

                    // Create state change for moving element (Small change — marks dirty only)
                    var change = new StateChange
                    {
                        Description = $"Move {displayName}",
                        Type = ChangeType.Small,
                        Execute = () => { }, // Already executed by drag
                        IsUndoable = true,
                        UndoCommand = new MoveElementCommand(
                            description: $"Move {displayName}",
                            moveTo: (tx, ty) =>
                            {
                                element.TileX = tx;
                                element.TileY = ty;
                                Canvas.SetLeft(border, tx * 8);
                                Canvas.SetTop(border, ty * 8);
                                UpdateModulePosition(element.Module, tx, ty);
                            },
                            prevX: startX, prevY: startY,
                            newX: endTileX, newY: endTileY)
                    };

                    _stateManager?.RegisterChange(change);
                }

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
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Category icon
        var icon = new TextBlock
        {
            Text = "⬛",
            FontSize = 10,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(GetCategoryColor(element))
        };

        var label = new TextBlock
        {
            Text = GetModuleDisplayText(element),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
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

        Grid.SetColumn(icon, 0);
        Grid.SetColumn(label, 1);
        Grid.SetColumn(remove, 2);
        grid.Children.Add(icon);
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
            Text = "USER ID",
            Style = (Style)FindResource("TextLabel"),
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
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "placeholder"
        };

        // Show placeholder when empty
        if (string.IsNullOrEmpty(element.UserId))
        {
            textBox.Text = $"(auto: {element.ElementId[..8]}...)";
            textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
        }

        var validationText = new TextBlock
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };

        string valueOnFocus = element.UserId;

        textBox.GotFocus += (s, e) =>
        {
            valueOnFocus = element.UserId;
            if (string.IsNullOrEmpty(element.UserId))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.White;
            }
        };

        textBox.LostFocus += (s, e) =>
        {
            if (_isUpdatingUI) return;

            var newValue = textBox.Text.Trim();
            var previousValue = valueOnFocus;

            // Restore placeholder if empty
            if (string.IsNullOrEmpty(newValue))
            {
                textBox.Text = $"(auto: {element.ElementId[..8]}...)";
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                element.UserId = string.Empty;
                validationText.Text = string.Empty;
                UpdateElementLabel(element);
                SyncProjectModules();
                return;
            }

            if (previousValue == newValue) return;

            // Validate
            var validation = ValidateUserId(newValue, element.ElementId);
            if (!validation.IsValid)
            {
                validationText.Text = validation.Error;
                textBox.Text = string.IsNullOrEmpty(previousValue) ? string.Empty : previousValue;
                if (string.IsNullOrEmpty(previousValue))
                {
                    textBox.Text = $"(auto: {element.ElementId[..8]}...)";
                    textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                }
                element.UserId = previousValue;
                return;
            }

            validationText.Text = string.Empty;

            // Create state change for User ID change (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = "Change User ID",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    _isUpdatingUI = true;
                    element.UserId = newValue;
                    if (string.IsNullOrEmpty(newValue))
                    {
                        textBox.Text = $"(auto: {element.ElementId[..8]}...)";
                        textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                    }
                    else
                    {
                        textBox.Text = newValue;
                        textBox.Foreground = Brushes.White;
                    }
                    _isUpdatingUI = false;
                    UpdateElementLabel(element);
                },
                IsUndoable = true,
                UndoCommand = new ChangePropertyCommand(
                    description: "Change User ID",
                    apply: val =>
                    {
                        _isUpdatingUI = true;
                        element.UserId = val;
                        if (string.IsNullOrEmpty(val))
                        {
                            textBox.Text = $"(auto: {element.ElementId[..8]}...)";
                            textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                        }
                        else
                        {
                            textBox.Text = val;
                            textBox.Foreground = Brushes.White;
                        }
                        _isUpdatingUI = false;
                        UpdateElementLabel(element);
                    },
                    previousValue: previousValue,
                    newValue: newValue)
            };

            _stateManager?.ApplyChange(change);
        };

        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
        PropertiesPanel.Children.Add(validationText);
    }

    private (bool IsValid, string Error) ValidateUserId(string userId, string currentElementId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return (true, string.Empty); // Optional field

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
                        !string.IsNullOrEmpty(elem.UserId) &&
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
            
            // Create state change for parameter change (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = $"Change {param.DisplayName} on {displayName}",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    _isUpdatingUI = true;
                    textBox.Text = newValue;
                    _isUpdatingUI = false;

                    SetModuleParameterValue(module, param.Name, newValue, param.Type);

                    if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(newValue, out var x))
                    {
                        element.TileX = x;
                        UpdateElementPosition(element);
                    }
                    else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(newValue, out var y))
                    {
                        element.TileY = y;
                        UpdateElementPosition(element);
                    }

                    UpdateElementLabel(element);
                    RefreshElementVisual(element);
                },
                IsUndoable = true,
                UndoCommand = new ChangePropertyCommand(
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
                    },
                    previousValue: previousValue,
                    newValue: newValue)
            };

            _stateManager?.ApplyChange(change);
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
                
                // Mark dirty on text change (real-time feedback)
                _projectManager?.MarkDirty();
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
    /// 
    /// For tilemap modules, renders the actual tile map instead of showing the raw asset.
    /// </summary>
    private UIElement? TryBuildAssetVisual(SceneElement element)
    {
        if (_project is null || element.Module is not IModule module) return null;

        // Special handling for tilemaps — render the actual map
        if (element.ModuleId.Contains("tilemap", StringComparison.OrdinalIgnoreCase))
            return TryBuildTilemapVisual(element);

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
    /// Renders a tilemap module by assembling tiles from the tileset.
    /// Returns null if tilemap data or asset is missing.
    /// </summary>
    private UIElement? TryBuildTilemapVisual(SceneElement element)
    {
        if (_project is null || _target is null || element.Module is not IModule module) return null;

        try
        {
            var json = module.Serialize();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract tilemap properties - try both naming conventions
            if (!root.TryGetProperty("tilesAssetId", out var assetIdProp)) return null;
            var assetId = assetIdProp.GetString();
            if (string.IsNullOrEmpty(assetId)) return null;
            
            int width, height;
            if (root.TryGetProperty("mapWidth", out var mapWidthProp))
                width = mapWidthProp.GetInt32();
            else if (root.TryGetProperty("width", out var widthProp))
                width = widthProp.GetInt32();
            else
                return null;

            if (root.TryGetProperty("mapHeight", out var mapHeightProp))
                height = mapHeightProp.GetInt32();
            else if (root.TryGetProperty("height", out var heightProp))
                height = heightProp.GetInt32();
            else
                return null;

            string? dataBase64 = null;
            int[]? mapDataArray = null;
            
            if (root.TryGetProperty("mapData", out var mapDataProp))
            {
                if (mapDataProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // int[] format (TilemapModule)
                    mapDataArray = mapDataProp.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                }
                else if (mapDataProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // Base64 string format (legacy)
                    dataBase64 = mapDataProp.GetString();
                }
            }
            else if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                dataBase64 = dataProp.GetString();
            }
            
            byte[] tileData;
            if (mapDataArray != null)
            {
                // Convert int[] to byte[]
                tileData = new byte[mapDataArray.Length];
                for (int i = 0; i < mapDataArray.Length; i++)
                {
                    tileData[i] = mapDataArray[i] < 0 ? (byte)255 : (byte)Math.Min(254, mapDataArray[i]);
                }
            }
            else if (!string.IsNullOrEmpty(dataBase64))
            {
                // Decode Base64
                tileData = Convert.FromBase64String(dataBase64);
            }
            else
            {
                return null;
            }

            // Find asset
            var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset is null) return null;

            var absPath = Path.Combine(_project.ProjectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absPath)) return null;

            // Load tileset
            var tileset = new BitmapImage();
            tileset.BeginInit();
            tileset.UriSource = new Uri(absPath);
            tileset.CacheOption = BitmapCacheOption.OnLoad;
            tileset.EndInit();
            tileset.Freeze();

            var tileSize = _target.Specs.TileWidth;
            var tilesetColumns = asset.SourceWidth / tileSize;

            // Create render target
            var renderWidth = width * tileSize;
            var renderHeight = height * tileSize;
            var renderTarget = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        if (index >= tileData.Length) continue;

                        int tileId = tileData[index];
                        // 255 is empty marker, skip it
                        if (tileId < 0 || tileId == 255) continue;

                        // Calculate source rect in tileset
                        int srcX = (tileId % tilesetColumns) * tileSize;
                        int srcY = (tileId / tilesetColumns) * tileSize;

                        var sourceRect = new Int32Rect(srcX, srcY, tileSize, tileSize);
                        var croppedTile = new CroppedBitmap(tileset, sourceRect);

                        // Draw tile at position
                        var destRect = new Rect(x * tileSize, y * tileSize, tileSize, tileSize);
                        dc.DrawImage(croppedTile, destRect);
                    }
                }
            }

            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();

            var img = new System.Windows.Controls.Image
            {
                Source = renderTarget,
                Width = renderWidth,
                Height = renderHeight,
                Stretch = System.Windows.Media.Stretch.None,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch
        {
            return null; // Any error → fall back to gray box
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
        // Refresh canvas visual
        if (element.CanvasVisual is Border cv && cv.Child is not System.Windows.Controls.Image)
        {
            cv.Padding = new Thickness(4, 2, 4, 2);
            cv.Child = BuildModuleLabel(element);
        }

        // Refresh event panel label
        if (element.EventVisual is Border ev && ev.Child is Grid g)
        {
            var label = g.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
            if (label is not null)
                label.Text = GetModuleDisplayText(element);
        }
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
            border.Child = BuildModuleLabel(element);
        }

        // Refresh event panel label too
        if (element.EventVisual is Border ev && ev.Child is Grid g)
        {
            var label = g.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
            if (label is not null)
                label.Text = GetModuleDisplayText(element);
        }
    }

    private string GetElementDisplayLabel(SceneElement element)
    {
        if (!string.IsNullOrEmpty(element.UserId))
            return $"[{element.UserId}]";

        // Fallback to ElementId (first 8 chars)
        var shortId = element.ElementId.Length > 8 ? element.ElementId[..8] : element.ElementId;
        return $"[{shortId}...]";
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

        // Create state change for removing element (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Remove {displayName}",
            Type = ChangeType.Large,
            Execute = () =>
            {
                RemoveElementCore(element);
                RefreshModulePalette();
            },
            IsUndoable = true,
            UndoCommand = new RemoveElementCommand(
                description: $"Remove {displayName}",
                remove: () =>
                {
                    RemoveElementCore(element);
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

                    RefreshModulePalette();
                }
            )
        };

        _stateManager?.ApplyChange(change);
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
    /// Updates existing elements instead of recreating the list to avoid duplication.
    /// </summary>
    private void SyncProjectModules()
    {
        if (_project is null || _currentScene is null) return;

        // Update existing elements or add new ones
        foreach (var element in _elements)
        {
            var moduleJson = element.Module is Retruxel.Core.Interfaces.IModule module
                ? module.Serialize()
                : "{}";

            var existingData = _currentScene.Elements.FirstOrDefault(e => e.ElementId == element.ElementId);
            
            if (existingData is not null)
            {
                // Update existing element
                existingData.UserId = element.UserId;
                existingData.ModuleId = element.ModuleId;
                existingData.TileX = element.TileX;
                existingData.TileY = element.TileY;
                existingData.Trigger = element.Trigger;
                existingData.ModuleState = System.Text.Json.JsonDocument.Parse(moduleJson).RootElement.Clone();
            }
            else
            {
                // Add new element
                _currentScene.Elements.Add(new SceneElementData
                {
                    ElementId = element.ElementId,
                    UserId = element.UserId,
                    ModuleId = element.ModuleId,
                    TileX = element.TileX,
                    TileY = element.TileY,
                    Trigger = element.Trigger,
                    ModuleState = System.Text.Json.JsonDocument.Parse(moduleJson).RootElement.Clone()
                });
            }
        }

        // Remove elements that no longer exist in memory
        var elementIds = _elements.Select(e => e.ElementId).ToHashSet();
        _currentScene.Elements.RemoveAll(e => !elementIds.Contains(e.ElementId));

        _project.DefaultModules = _elements
            .Select(e => e.ModuleId)
            .Distinct()
            .ToList();

        _projectManager?.MarkDirty();
    }

    private async void GenerateRom_Click(object sender, RoutedEventArgs e)
    {
        // Save before building ROM
        await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
        
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
            AddElementFromData(elementData);
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
    private IModule? DeserializeModule(string moduleId, System.Text.Json.JsonElement moduleState)
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

        if (moduleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
            moduleState.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            var jsonString = moduleState.GetRawText();
            module.Deserialize(jsonString);
        }

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
    /// Passes the actual module instance from memory, ensuring changes persist.
    /// </summary>
    private void OpenVisualToolForElement(SceneElement element)
    {
        if (_project is null || _target is null || _currentScene is null) return;
        if (element.Module is not IModule module) return;
        if (string.IsNullOrEmpty(module.VisualToolId)) return;

        // Find corresponding SceneElementData
        var elementData = _currentScene.Elements.FirstOrDefault(e => e.ElementId == element.ElementId);

        // Pass the actual module instance from memory and a callback that marks dirty and saves
        var toolResult = VisualToolInvoker.OpenVisualTool(
            module,  // This is the real instance in _elements[].Module
            _target,
            _project,
            _project.ProjectPath,
            _currentScene,
            elementData,
            async () =>
            {
                // Mark project as dirty and trigger save
                _projectManager?.MarkDirty();
                await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
            },
            this);
        
        // If tool was opened and changes were made, register as a Large change
        if (toolResult)
        {
            var displayName = module.DisplayName;
            var change = new StateChange
            {
                Description = $"Edit {displayName}",
                Type = ChangeType.Large,
                Execute = () => { }, // Already executed by visual tool
                IsUndoable = false // Visual tool changes not undoable
            };
            _stateManager?.RegisterChange(change);
        }

        // Refresh UI after tool closes
        RefreshElementVisual(element);
        if (_selectedElement == element)
            BuildPropertiesPanel(element);
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
                return $"[{UserId}]";

            // Fallback to ElementId (first 8 chars)
            var shortId = ElementId.Length > 8 ? ElementId[..8] : ElementId;
            return $"[{shortId}...]";
        }
    }
}