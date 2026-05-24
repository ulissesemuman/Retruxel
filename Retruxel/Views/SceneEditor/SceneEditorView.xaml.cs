using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Retruxel.Views;

public partial class SceneEditorView : UserControl
{
    // ── Core state ─────────────────────────────────────────────────────────────
    private RetruxelProject? _project;
    private ITarget?         _target;
    private SceneData?       _currentScene;

    private ProjectManager?  _projectManager;
    private ModuleRegistry?  _moduleRegistry;
    private ModuleRenderer?  _moduleRenderer;
    private StateManager?    _stateManager;

    private readonly UndoRedoStack _undoRedo = new();

    // ── Selection state ────────────────────────────────────────────────────────
    // Selection now points to typed model objects, not a generic SceneElement.
    private object?   _selectedItem;       // PlaneLayerData | EntityData | PaletteSlotData
    private SceneData? _selectedScene;     // kept for properties panel context

    private bool _isUpdatingUI;
    private bool _isLoadingProject;

    // ── Preview zoom / pan ─────────────────────────────────────────────────────
    private const double PreviewZoomMin  = 0.25;
    private const double PreviewZoomMax  = 8.0;
    private const double PreviewZoomStep = 1.2;

    private double _previewZoom     = 1.0;
    private double _previewOffsetX  = 0;
    private double _previewOffsetY  = 0;

    private bool   _isPanning;
    private Point  _panStartMouse;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    // ── Public events ──────────────────────────────────────────────────────────
    public event Action<RetruxelProject>? OnGenerateRomRequested;
    public event Action?                  OnAboutRequested;

    // ──────────────────────────────────────────────────────────────────────────

    public SceneEditorView()
    {
        InitializeComponent();
        KeyDown     += SceneEditorView_KeyDown;
        Focusable    = true;
        SizeChanged += (_, _) => ApplyPreviewTransform();
    }

    // ── Dependency injection ───────────────────────────────────────────────────

    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;

        if (_stateManager == null)
        {
            _stateManager = new StateManager(manager, _undoRedo);
            _stateManager.SavingStateChanged += OnSavingStateChanged;
            _stateManager.StateChanged       += () => _projectManager?.MarkDirty();
        }
    }

    public void SetModuleRegistry(ModuleRegistry registry) => _moduleRegistry = registry;

    // ── Save indicator ─────────────────────────────────────────────────────────

    private void OnSavingStateChanged(bool isSaving)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("TxtSaveIndicator") is not TextBlock indicator) return;
            var anim = new DoubleAnimation
            {
                From     = isSaving ? 0.3 : 1.0,
                To       = isSaving ? 1.0 : 0.3,
                Duration = TimeSpan.FromMilliseconds(isSaving ? 200 : 400)
            };
            indicator.BeginAnimation(UIElement.OpacityProperty, anim);
        });
    }

    // ── Undo / redo ────────────────────────────────────────────────────────────

    private void BtnUndo_Click(object sender, RoutedEventArgs e) => _undoRedo.Undo();
    private void BtnRedo_Click(object sender, RoutedEventArgs e) => _undoRedo.Redo();

    private void UpdateUndoRedoButtons()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (FindName("BtnUndo") is Button u)
            {
                u.IsEnabled = _undoRedo.CanUndo;
                u.ToolTip   = _undoRedo.CanUndo ? $"Undo: {_undoRedo.NextUndoDescription}" : "Nothing to undo";
            }
            if (FindName("BtnRedo") is Button r)
            {
                r.IsEnabled = _undoRedo.CanRedo;
                r.ToolTip   = _undoRedo.CanRedo ? $"Redo: {_undoRedo.NextRedoDescription}" : "Nothing to redo";
            }
        });
    }

    // ── Initialize ─────────────────────────────────────────────────────────────

    public void Initialize(RetruxelProject project, ITarget target)
    {
        _project = project;
        _target  = target;

        var pluginsPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
            "plugins");
        _moduleRenderer = new ModuleRenderer(pluginsPath, target.GetType().Assembly);

        _selectedItem = null;
        SceneCanvas.Children.Clear();

        var settings = SettingsService.Load();
        _undoRedo.MaxHistorySize = Math.Clamp(settings.General.UndoHistoryLimit, 20, 100);
        _undoRedo.Clear();
        _undoRedo.StateChanged += UpdateUndoRedoButtons;

        // Ensure at least one scene exists
        _currentScene = project.Scenes.FirstOrDefault();
        if (_currentScene is null)
        {
            _currentScene = new SceneData
            {
                SceneId   = Guid.NewGuid().ToString(),
                SceneName = "Main"
            };
            InitializePaletteSlots(_currentScene, target);
            EnsureDefaultPlanes(_currentScene, target);
            project.Scenes.Add(_currentScene);
        }
        else
        {
            MigrateScene(_currentScene, target);
        }

        ApplyTargetSpecs(target);
        RebuildSceneTabs();
        RebuildProjectTree();
        RefreshPreview();
    }

    public void Cleanup()
    {
        _stateManager?.Dispose();
        _stateManager = null;
    }

    // ── Target specs ───────────────────────────────────────────────────────────

    private void ApplyTargetSpecs(ITarget target)
    {
        var specs = target.Specs;
        TxtCanvasSize.Text  = $"{specs.ScreenWidth} × {specs.ScreenHeight} px";
        SceneCanvas.Width   = specs.ScreenWidth;
        SceneCanvas.Height  = specs.ScreenHeight;
        ApplyPreviewTransform();
    }

    // ── Scene / palette / plane initialization ───────────────────────────────

    private static void InitializePaletteSlots(SceneData scene, ITarget target)
    {
        scene.PaletteSlots.Clear();
        for (int i = 0; i < target.GetPaletteSlotCount(); i++)
        {
            scene.PaletteSlots.Add(new PaletteSlotData
            {
                SlotIndex = i,
                Label     = target.GetPaletteSlotType(i).ToString(),
                Colors    = Enumerable.Repeat("#000000", target.GetColorsPerSlot()).ToList()
            });
        }
    }

    /// <summary>
    /// Ensures one PlaneData exists per PlaneSpecs defined in the target.
    /// SMS = 1 plane ("bg"), SNES = 4 ("bg1"–"bg4"), etc.
    /// Each starts with zero layers — user adds layers via the tree.
    /// Safe to call on existing scenes: only adds planes that are missing.
    /// </summary>
    private static void EnsureDefaultPlanes(SceneData scene, ITarget target)
    {
        foreach (var planeSpecs in target.Specs.Planes)
        {
            if (scene.Planes.Any(p => p.PlaneId == planeSpecs.Id)) continue;

            scene.Planes.Add(new PlaneData
            {
                PlaneId = planeSpecs.Id
            });
        }

        // Fallback for targets that haven't defined Planes yet
        if (scene.Planes.Count == 0)
        {
            scene.Planes.Add(new PlaneData { PlaneId = "bg" });
        }
    }

    private static void MigrateScene(SceneData scene, ITarget target)
    {
        if (scene.PaletteSlots.Count == 0)
            InitializePaletteSlots(scene, target);

        // Migrate legacy flat Elements into typed Planes/Entities if needed
        if (scene.Elements.Count > 0 && scene.Planes.Count == 0)
            MigrateLegacyElements(scene);

        // Ensure all hardware planes are present (handles projects saved before
        // EnsureDefaultPlanes was introduced, or targets with new planes added later).
        EnsureDefaultPlanes(scene, target);
    }

    /// <summary>
    /// One-time migration from the old flat SceneElementData model to the new typed hierarchy.
    /// Runs only when loading a pre-refactor project file.
    /// </summary>
    private static void MigrateLegacyElements(SceneData scene)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[SceneEditor] Migrating {scene.Elements.Count} legacy elements in scene '{scene.SceneName}'");

        // Create a default plane to receive migrated tilemap elements
        var defaultPlane = new PlaneData { PlaneId = "bg" };

        foreach (var elem in scene.Elements)
        {
            if (elem.ModuleId.Contains("plane", StringComparison.OrdinalIgnoreCase))
            {
                var layer = new PlaneLayerData
                {
                    LayerId   = elem.ElementId,
                    LayerName = elem.UserId ?? $"Layer {defaultPlane.Layers.Count}",
                    AssetId   = TryGetAssetId(elem),
                    Visible   = true
                };
                defaultPlane.Layers.Add(layer);
            }
            else if (elem.ModuleId is "entity" or "enemy" or "sprite" or "player")
            {
                scene.Entities.Add(new EntityData
                {
                    EntityId    = elem.ElementId,
                    Label       = elem.UserId ?? elem.ModuleId,
                    EntityType  = elem.ModuleId,
                    SpriteAssetId = TryGetAssetId(elem),
                    StartTileX  = elem.TileX,
                    StartTileY  = elem.TileY
                });
            }
        }

        if (defaultPlane.Layers.Count > 0)
            scene.Planes.Add(defaultPlane);

        // Keep Elements for backward compat serialization but mark as migrated
        scene.Elements.Clear();
    }

    private static string TryGetAssetId(SceneElementData elem)
    {
        try
        {
            if (elem.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Object &&
                elem.ModuleState.TryGetProperty("tilesAssetId", out var prop))
                return prop.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    // ── Selection ──────────────────────────────────────────────────────────────

    internal void SelectItem(object? item)
    {
        _selectedItem = item;

        if (item is null)
        {
            ShowPropertiesEmpty();
            return;
        }

        if (!_isLoadingProject)
            ShowPropertiesForItem(item);

        Focus();
    }

    private void ShowPropertiesEmpty()
    {
        TxtNoSelection.Visibility  = Visibility.Visible;
        PropertiesPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowPropertiesForItem(object item)
    {
        TxtNoSelection.Visibility  = Visibility.Collapsed;
        PropertiesPanel.Visibility = Visibility.Visible;
        BuildPropertiesPanel(item);
    }

    // ── Generate ROM ───────────────────────────────────────────────────────────

    private async void GenerateRom_Click(object sender, RoutedEventArgs e)
    {
        await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
        if (_project is not null)
            OnGenerateRomRequested?.Invoke(_project);
    }

    // ── Keyboard ───────────────────────────────────────────────────────────────

    private void SceneEditorView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _selectedItem is not null)
        {
            DeleteSelectedItem();
            e.Handled = true;
        }

        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = _stateManager?.SaveNowAsync();
            e.Handled = true;
        }

        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _undoRedo.Undo();
            e.Handled = true;
        }

        if ((e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) ||
            (e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
        {
            _undoRedo.Redo();
            e.Handled = true;
        }
    }

    private void DeleteSelectedItem()
    {
        if (_selectedItem is PlaneLayerData layer)
            RemovePlaneLayer(layer);
        else if (_selectedItem is EntityData entity)
            RemoveEntity(entity);
    }

    private void Documentation_Click(object sender, RoutedEventArgs e)
        => OnAboutRequested?.Invoke();

    // ── Sidebar tab switching ──────────────────────────────────────────────────

    private void BtnImportAsset_Click(object sender, RoutedEventArgs e)
        => OpenAssetImporter("bg");
}
