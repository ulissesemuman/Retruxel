using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow : Window
{
    // ── Context ───────────────────────────────────────────────────────────────
    private readonly ITarget _target;
    private readonly RetruxelProject _project;
    private readonly SceneData? _currentScene;

    // ── Tile editing state ────────────────────────────────────────────────────
    private AssetEntry? _currentAsset;
    private int _selectedTileIndex = 0;
    private int _tileSize = 8;

    // Pixel buffer for the currently edited tile: [x, y] = palette color index
    private byte[,] _pixels = new byte[8, 8];

    // ── Palette ───────────────────────────────────────────────────────────────
    private IReadOnlyList<HardwareColor> _hardwarePalette = [];
    private IReadOnlyList<string> _paletteColors = [];   // hex strings from scene slot
    private int _selectedColorIndex = 1;                  // index 0 is usually transparent

    // ── Tools ─────────────────────────────────────────────────────────────────
    private enum PixelTool { Pencil, Bucket, Select, Mirror }
    private PixelTool _activeTool = PixelTool.Pencil;
    private bool _mirrorX = false;

    // ── Zoom / rendering ──────────────────────────────────────────────────────
    private int _editorZoom   = 16;   // pixels per tile pixel on the editor canvas
    private double _tileZoom  = 2.0;  // scale factor for the tileset panel
    private bool _isPainting  = false;
    private int _lastPx = -1, _lastPy = -1;

    // ── Result ────────────────────────────────────────────────────────────────
    /// <summary>Modified asset after save. Caller should update project.Assets.</summary>
    public AssetEntry? SavedAsset { get; private set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>Opens the editor with full project context.</summary>
    public PixelArtEditorWindow(
        ITarget target,
        RetruxelProject project,
        SceneData? currentScene = null,
        string? initialAssetId = null)
    {
        _target       = target;
        _project      = project;
        _currentScene = currentScene;
        _tileSize     = target.Specs.TileWidth;
        _pixels       = new byte[_tileSize, _tileSize];

        InitializeComponent();

        TxtTargetLabel.Text = target.DisplayName.ToUpper();

        InitializePalette();
        LoadAssets(initialAssetId);
        UpdateToolButtons();
    }

    /// <summary>Fallback: opens without project context (preview only).</summary>
    public PixelArtEditorWindow()
    {
        // Minimal stub so XAML designer doesn't crash
        _target       = null!;
        _project      = null!;
        _currentScene = null;
        _pixels       = new byte[8, 8];
        _hardwarePalette = [];
        _paletteColors   = [];

        InitializeComponent();
        TxtModeInfo.Text = "PENCIL";
    }

    // ── Window chrome ─────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        catch { }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)   => Close();
    private void BtnCancel_Click(object sender, RoutedEventArgs e)  => Close();

    private void BtnCanvasZoom_Click(object sender, RoutedEventArgs e)
    {
        _editorZoom = _editorZoom switch { <= 8 => 12, <= 12 => 16, <= 16 => 24, _ => 8 };
        BtnCanvasZoom.Content = $"{_editorZoom * (_tileSize == 0 ? 1 : _tileSize / 8)}x";
        RenderEditorCanvas();
    }

    private void BtnTileZoom_Click(object sender, RoutedEventArgs e)
    {
        _tileZoom = _tileZoom switch { <= 1.0 => 2.0, <= 2.0 => 3.0, _ => 1.0 };
        BtnTileZoom.Content = $"{(int)(_tileZoom * 100)}%";
        RenderTilesetCanvas();
    }

    private void EditorCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            _editorZoom = Math.Clamp(_editorZoom + (e.Delta > 0 ? 2 : -2), 4, 48);
            RenderEditorCanvas();
            e.Handled = true;
        }
    }

    // ── Tool buttons ──────────────────────────────────────────────────────────

    private void BtnToolPencil_Click(object sender, RoutedEventArgs e)
        => SetTool(PixelTool.Pencil);

    private void BtnToolBucket_Click(object sender, RoutedEventArgs e)
        => SetTool(PixelTool.Bucket);

    private void BtnToolSelect_Click(object sender, RoutedEventArgs e)
        => SetTool(PixelTool.Select);

    private void BtnToolMirror_Click(object sender, RoutedEventArgs e)
    {
        _mirrorX = !_mirrorX;
        SetTool(PixelTool.Mirror);
    }

    private void SetTool(PixelTool tool)
    {
        _activeTool = tool;
        TxtModeInfo.Text = tool switch
        {
            PixelTool.Pencil => "PENCIL",
            PixelTool.Bucket => "BUCKET",
            PixelTool.Select => "SELECT",
            PixelTool.Mirror => _mirrorX ? "MIRROR ON" : "MIRROR OFF",
            _ => "—"
        };
        UpdateToolButtons();
    }

    private void UpdateToolButtons()
    {
        if (BtnToolPencil == null) return;
        void Set(System.Windows.Controls.Button b, bool active) => b.Opacity = active ? 1.0 : 0.5;
        Set(BtnToolPencil, _activeTool == PixelTool.Pencil);
        Set(BtnToolBucket, _activeTool == PixelTool.Bucket);
        Set(BtnToolSelect, _activeTool == PixelTool.Select);
        Set(BtnToolMirror, _mirrorX);
    }

    // ── Placeholder handlers (implemented in partial classes) ─────────────────

    private void TilesetCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => HandleTilesetClick(e.GetPosition(TilesetCanvas));

    private void TilesetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
    private void TilesetCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { }
    private void TilesetCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) { }

    private void CmbTilesetAsset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem is string assetId)
            LoadAsset(assetId);
    }
}
