using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.TilemapEditor.Helpers;
using TilemapEditorData = Retruxel.Tool.TilemapEditor.Helpers.PlaneInfo;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

public enum ToolMode
{
    Paint,
    Navigate
}

/// <summary>
/// Tilemap Editor Window - 100% target-agnostic.
/// Generates UI dynamically based on target.Specs.Plane.
/// </summary>
public partial class TilemapEditorWindow : Window
{
    private readonly ITarget _target;
    private readonly RetruxelProject _project;
    private readonly string _projectPath;
    private readonly ToolRegistry? _toolRegistry;
    private readonly Func<System.Threading.Tasks.Task>? _saveProjectCallback;
    private readonly object? _sceneEditor;
    private readonly PlaneSpecs _planeSpecs;  // specs of the plane being edited

    private readonly TilemapEditorData _planeData = new();
    private readonly TilesetRenderer _tilesetRenderer = new();
    private readonly IndexedPngService _indexedPngService = new();

    private int _selectedTileId = 0;
    private bool _selectedFlipH = false;
    private bool _selectedFlipV = false;
    private int _currentLayerIndex = 0;
    private double _canvasZoom = 1.0;
    private double _tileZoomLevel = 1.0;
    private bool _isPainting = false;
    private bool _isInitializing = true;
    private int _mapOffsetX = 0;
    private int _mapOffsetY = 0;
    private ToolMode _currentToolMode = ToolMode.Paint;

    private IndexedPngData? _indexedData;
    private SceneData? _currentScene;

    public Dictionary<string, object>? ModuleData { get; private set; }

    public TilemapEditorWindow(ITarget target, RetruxelProject project, string projectPath, ToolRegistry? toolRegistry = null, Func<System.Threading.Tasks.Task>? saveProjectCallback = null, object? sceneEditor = null, string? planeId = null)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;
        _toolRegistry = toolRegistry;
        _saveProjectCallback = saveProjectCallback;
        _sceneEditor = sceneEditor;

        // Try to get current scene from sceneEditor
        if (sceneEditor != null)
        {
            var sceneField = sceneEditor.GetType().GetField("_currentScene",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _currentScene = sceneField?.GetValue(sceneEditor) as SceneData;
        }

        // Resolve PlaneSpecs for the plane being edited.
        // If planeId is provided (from VisualToolInvoker via planeData), use it.
        // Otherwise fall back to the first plane defined by the target.
        _planeSpecs = (planeId is not null
            ? target.Specs.Planes.FirstOrDefault(p => p.Id == planeId)
            : null)
            ?? target.Specs.Planes.FirstOrDefault()
            ?? new PlaneSpecs();

        TxtTargetLabel.Text = target.DisplayName.ToUpper();

        InitializeSelection();
        InitializeUI();
        LoadAssets();
    }
}
