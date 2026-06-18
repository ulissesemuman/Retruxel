using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.TilesetHelpers;
using Retruxel.Tool.TilemapEditor.Helpers;
using TilemapEditorData = Retruxel.Tool.TilemapEditor.Helpers.PlaneInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

public enum ToolMode
{
    Paint,
    Navigate,
    MetatilePaint,
    BrushPaint
}

    public class Metatile
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int[,] TileIndices { get; set; } // [y, x]
    }
    
    public class Brush
    {
        public string Name { get; set; } = "";
        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public List<BrushTile> Tiles { get; set; } = new();
    }
    
    public class BrushTile
    {
        public int TileIndex { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public bool FlipH { get; set; }
        public bool FlipV { get; set; }
        public int Rotation { get; set; } // 0, 90, 180, 270
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
    private readonly MapIndexService _indexedPngService = new();

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

    private SceneData? _currentScene;

    // ── Auto-tiling ────────────────────────────────────────────────────────────
    private bool _isAutoTilingEnabled = false;
    private int _autoTileBaseIndex = 0;

    // ── Metatiles ─────────────────────────────────────────────────────────────
    private readonly List<Metatile> _metatiles = new();
    private int _selectedMetatileIndex = -1;

    // ── Brushes ────────────────────────────────────────────────────────────────
    private readonly List<Brush> _brushes = new();
    private int _selectedBrushIndex = -1;

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
        InitializeMetatilesAndBrushes();
        InitializeUI();
        LoadAssets();
    }

    // ── Auto-tiling ────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the correct tile from an auto-tiling set based on the 4-neighbor mask.
    /// The expected tileset order is:
    /// 0 = full/interior, 1 = top edge, 2 = top-right corner, 3 = right edge,
    /// 4 = bottom-right corner, 5 = bottom edge, 6 = bottom-left corner,
    /// 7 = left edge, 8 = top-left corner, 9 = isolated.
    /// </summary>
    private int ResolveAutoTileIndex(int x, int y)
    {
        if (!_isAutoTilingEnabled)
            return _selectedTileId;

        int mask = 0;

        if (IsAutoTileMatch(_currentLayerIndex, x, y - 1)) mask |= 1;    // up
        if (IsAutoTileMatch(_currentLayerIndex, x + 1, y)) mask |= 2;    // right
        if (IsAutoTileMatch(_currentLayerIndex, x, y + 1)) mask |= 4;    // down
        if (IsAutoTileMatch(_currentLayerIndex, x - 1, y)) mask |= 8;    // left

        return _autoTileBaseIndex + mask switch
        {
            0b1111 => 0,
            0b1110 => 1,
            0b1010 => 3,
            0b0010 => 3,
            0b0110 => 4,
            0b0100 => 5,
            0b0101 => 6,
            0b0001 => 7,
            0b1101 => 8,
            0b1001 => 8,
            0b1000 => 7,
            _        => 9
        };
    }

    private bool IsAutoTileMatch(int layerIndex, int x, int y)
    {
        if (x < 0 || y < 0) return false;

        var neighbor = _planeData.GetTile(layerIndex, x, y);
        if (neighbor.IsEmpty) return false;

        // Same tile family: either the exact selected tile, or any tile in the
        // auto-tiling range. This lets the user paint with the base tile and still
        // connect to existing auto-tile pieces.
        return neighbor.TileIndex == _selectedTileId
            || (neighbor.TileIndex >= _autoTileBaseIndex && neighbor.TileIndex < _autoTileBaseIndex + 10);
    }

    // ── Metatiles and brushes ─────────────────────────────────────────────────

    private void InitializeMetatilesAndBrushes()
    {
        _metatiles.Clear();
        _brushes.Clear();

        _metatiles.Add(new Metatile
        {
            Name = "Solid 2×2",
            Width = 2,
            Height = 2,
            TileIndices = new int[2, 2]
        });
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
            _metatiles[0].TileIndices[y, x] = 0;

        _metatiles.Add(new Metatile
        {
            Name = "Platform 4×2",
            Width = 4,
            Height = 2,
            TileIndices = new int[2, 4]
        });
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 4; x++)
            _metatiles[1].TileIndices[y, x] = 1;

        _brushes.Add(new Brush
        {
            Name = "Single tile",
            OriginX = 0,
            OriginY = 0,
            Tiles = new List<BrushTile>
            {
                new() { TileIndex = 0, OffsetX = 0, OffsetY = 0 }
            }
        });
    }

    private Metatile CreateMetatileFromSelection()
    {
        if (_selectedTileIds.Count == 0)
            return null!;

        var metatile = new Metatile
        {
            Name = $"Metatile {_metatiles.Count + 1}",
            Width = _selectionWidth,
            Height = _selectionHeight,
            TileIndices = new int[_selectionHeight, _selectionWidth]
        };

        int i = 0;
        for (int y = 0; y < _selectionHeight; y++)
        for (int x = 0; x < _selectionWidth; x++)
            metatile.TileIndices[y, x] = _selectedTileIds[i++];

        _metatiles.Add(metatile);
        _selectedMetatileIndex = _metatiles.Count - 1;
        return metatile;
    }

    private Brush CreateBrushFromSelection()
    {
        if (_selectedTileIds.Count == 0)
            return null!;

        var brush = new Brush
        {
            Name = $"Brush {_brushes.Count + 1}",
            OriginX = 0,
            OriginY = 0,
            Tiles = new List<BrushTile>()
        };

        for (int i = 0; i < _selectedTileIds.Count; i++)
        {
            int x = i % _selectionWidth;
            int y = i / _selectionWidth;
            brush.Tiles.Add(new BrushTile
            {
                TileIndex = _selectedTileIds[i],
                OffsetX = x,
                OffsetY = y
            });
        }

        _brushes.Add(brush);
        _selectedBrushIndex = _brushes.Count - 1;
        return brush;
    }
}
