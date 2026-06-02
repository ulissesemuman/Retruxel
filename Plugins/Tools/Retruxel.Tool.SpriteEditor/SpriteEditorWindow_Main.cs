using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.SpriteEditor.Models;
using Retruxel.Lib.TilesetHelpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    // ── Core state ─────────────────────────────────────────────────────────
    private readonly SpriteState _state = new();
    private bool _isInitializing = true;
    private string _tilesetAssetId = string.Empty;
    private SceneData? _currentScene;
    private AssetEntry? _currentAsset;
    private RetruxelProject? _project;
    private ITarget? _target;
    private string? _projectPath;
    private Core.Services.ToolRegistry? _toolRegistry;
    private Func<Task>? _saveProjectCallback;

    // ── Tileset rendering (same helpers as TilemapEditor) ──────────────────
    private readonly TilesetRenderer _tilesetRenderer = new();
    private SKBitmap? _tilesetBitmap;          // full tileset grid bitmap (scaled)
    private double _tileZoomLevel = 1.0;       // tileset zoom (matches _tilesetZoom)

    // ── Animation ──────────────────────────────────────────────────────────
    private DispatcherTimer? _animationTimer;
    private int _animationFrameIndex;

    // ── Selection ──────────────────────────────────────────────────────────
    private int _selectedTileIndex = -1;

    // ── Output ─────────────────────────────────────────────────────────────
    public Dictionary<string, object>? ModuleData { get; set; }

    // ── Constructor ────────────────────────────────────────────────────────

    public SpriteEditorWindow(
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData? scene,
        Core.Services.ToolRegistry? toolRegistry,
        Func<Task>? saveProjectCallback,
        object? sceneEditor)
    {
        InitializeComponent();

        _target      = target;
        _project     = project;
        _projectPath = projectPath;
        _toolRegistry        = toolRegistry;
        _saveProjectCallback = saveProjectCallback;

        // Prefer the explicitly passed scene; fall back to reflection on sceneEditor
        if (scene != null)
        {
            _currentScene = scene;
        }
        else if (sceneEditor != null)
        {
            var sceneField = sceneEditor.GetType().GetField("_currentScene",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _currentScene = sceneField?.GetValue(sceneEditor) as SceneData;
        }

        InitializeAssetSelector();
        InitializePaletteSlotSelector();
        InitializeZoomControls();

        // Start with one empty frame
        _state.Frames.Add(new SpriteFrame { Name = "Frame 1" });

        _isInitializing = false;
        InitializeUI();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void OnSpriteChanged()
    {
        RenderCanvas();
        RenderPreview();
    }
}
