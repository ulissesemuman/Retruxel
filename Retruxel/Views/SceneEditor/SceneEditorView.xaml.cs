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

    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private readonly List<SceneElement> _selectedElements = [];

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



    private void BtnUndo_Click(object sender, RoutedEventArgs e) => _undoRedo.Undo();
    private void BtnRedo_Click(object sender, RoutedEventArgs e) => _undoRedo.Redo();

    // ── Sidebar tab switching ─────────────────────────────────────────────────

    private void BtnTabStructure_Click(object sender, RoutedEventArgs e)
    {
        PanelStructure.Visibility = Visibility.Visible;
        PanelModules.Visibility = Visibility.Collapsed;
        BtnTabStructure.Tag = "active";
        BtnTabModules.Tag = null;
        RefreshStructurePanel();
    }

    private void BtnTabModules_Click(object sender, RoutedEventArgs e)
    {
        PanelStructure.Visibility = Visibility.Collapsed;
        PanelModules.Visibility = Visibility.Visible;
        BtnTabStructure.Tag = null;
        BtnTabModules.Tag = "active";
    }

    // ── Asset panel ───────────────────────────────────────────────────────────








    














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
        LoadFromProject();
        RefreshStructurePanel();
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









    private void Documentation_Click(object sender, RoutedEventArgs e)
        => OnAboutRequested?.Invoke();





    // ===== EVENTS PANEL =====

































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
        
        RefreshStructurePanel();
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
    public SceneElementData Data { get; set; } = new();

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