using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.SpriteEditor.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private readonly SpriteState _state = new();
    private SKBitmap _tilesetImage;
    private bool _isInitializing = true;
    private string _tilesetAssetId = string.Empty;
    private SceneData? _currentScene;
    private RetruxelProject? _project;
    private ITarget? _target;
    private string? _projectPath;
    private Core.Services.ToolRegistry? _toolRegistry;
    private Func<Task>? _saveProjectCallback;
    private DispatcherTimer? _animationTimer;
    private int _animationFrameIndex;
    private IndexedPngData? _indexedData;
    private readonly IndexedPngService _indexedPngService = new();
    private int _selectedTileIndex = -1;

    public Dictionary<string, object>? ModuleData { get; set; }

    public SpriteEditorWindow(
        ITarget target,
        RetruxelProject project,
        string projectPath,
        Core.Services.ToolRegistry? toolRegistry,
        Func<Task>? saveProjectCallback,
        object? sceneEditor)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;
        _toolRegistry = toolRegistry;
        _saveProjectCallback = saveProjectCallback;

        // Extract _currentScene from sceneEditor via reflection
        if (sceneEditor != null)
        {
            var sceneField = sceneEditor.GetType().GetField("_currentScene",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _currentScene = sceneField?.GetValue(sceneEditor) as SceneData;
        }

        InitializeAssetSelector();
        InitializePaletteSlotSelector();
        InitializeZoomControls();

        // Initialize with one empty frame
        _state.Frames.Add(new SpriteFrame { Name = "Frame 1" });

        _isInitializing = false;
        InitializeUI();
    }

    private void OnSpriteChanged()
    {
        RenderCanvas();
        RenderPreview();
    }
}
