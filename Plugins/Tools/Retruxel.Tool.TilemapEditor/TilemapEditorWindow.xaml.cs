using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.TilemapEditor.Helpers;
using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Tilemap Editor Window - 100% target-agnostic.
/// Generates UI dynamically based on target.Specs.Tilemap.
/// </summary>
public partial class TilemapEditorWindow : Window
{
    private readonly ITarget _target;
    private readonly RetruxelProject _project;
    private readonly string _projectPath;
    private readonly ToolRegistry? _toolRegistry;
    private readonly Func<System.Threading.Tasks.Task>? _saveProjectCallback;
    private readonly object? _sceneEditor;

    private readonly TilemapData _tilemapData = new();
    private readonly TilesetRenderer _tilesetRenderer = new();

    private int _selectedTileId = 0;
    private int _currentLayerIndex = 0;
    private double _canvasZoom = 1.0;
    private double _tileZoomLevel = 1.0;
    private bool _isPainting = false;
    private bool _isInitializing = true;
    private int _mapOffsetX = 0;
    private int _mapOffsetY = 0;

    public Dictionary<string, object>? ModuleData { get; private set; }

    public TilemapEditorWindow(ITarget target, RetruxelProject project, string projectPath, ToolRegistry? toolRegistry = null, Func<System.Threading.Tasks.Task>? saveProjectCallback = null, object? sceneEditor = null)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;
        _toolRegistry = toolRegistry;
        _saveProjectCallback = saveProjectCallback;
        _sceneEditor = sceneEditor;

        TxtTargetLabel.Text = target.DisplayName.ToUpper();

        InitializeSelection();
        InitializeUI();
        LoadAssets();
    }
}
