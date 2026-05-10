using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Controls;

/// <summary>
/// Reusable tileset viewer and selector.
///
/// Displays an indexed tileset as a grid of selectable tiles.
/// Rendering is driven by MapIndex + palette — no PNG path required at runtime.
/// Used by TilemapEditorWindow and SpriteEditorWindow.
///
/// Key design decisions:
/// - The full tileset is rendered as a single WriteableBitmap (one render per palette change).
/// - Grid and selection overlay are Canvas elements drawn on top.
/// - Columns is configurable (default 16 for SMS tilesets, fewer for sprite sheets).
/// - Zoom is handled externally via the ZoomFactor property.
/// </summary>
public partial class IndexedTilesetControl : UserControl
{
    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(int), typeof(IndexedTilesetControl),
            new PropertyMetadata(16, OnLayoutChanged));

    public static readonly DependencyProperty TileSizeProperty =
        DependencyProperty.Register(nameof(TileSize), typeof(int), typeof(IndexedTilesetControl),
            new PropertyMetadata(8, OnLayoutChanged));

    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(IndexedTilesetControl),
            new PropertyMetadata(1.0, OnZoomChanged));

    public static readonly DependencyProperty GridVisibleProperty =
        DependencyProperty.Register(nameof(GridVisible), typeof(bool), typeof(IndexedTilesetControl),
            new PropertyMetadata(true, OnLayoutChanged));

    public static readonly DependencyProperty GridColorProperty =
        DependencyProperty.Register(nameof(GridColor), typeof(Color), typeof(IndexedTilesetControl),
            new PropertyMetadata(Color.FromArgb(60, 255, 255, 255), OnLayoutChanged));

    /// <summary>Number of tile columns in the grid. Default 16.</summary>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>Tile size in pixels (width and height). Default 8.</summary>
    public int TileSize
    {
        get => (int)GetValue(TileSizeProperty);
        set => SetValue(TileSizeProperty, value);
    }

    /// <summary>Display zoom factor. Does not affect MapIndex or palette. Default 1.0.</summary>
    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    /// <summary>Whether to show the tile grid overlay. Default true.</summary>
    public bool GridVisible
    {
        get => (bool)GetValue(GridVisibleProperty);
        set => SetValue(GridVisibleProperty, value);
    }

    /// <summary>Grid line color. Default white at 24% opacity.</summary>
    public Color GridColor
    {
        get => (Color)GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the user changes the tile selection.</summary>
    public event EventHandler<TileSelectionChangedEventArgs>? TileSelectionChanged;

    // ── State ─────────────────────────────────────────────────────────────────

    private byte[]? _mapIndex;
    private IReadOnlyList<HardwareColor>? _palette;
    private int _totalTiles;
    private WriteableBitmap? _tilesetBitmap;

    // Selection
    private List<int> _selectedTileIds = [0];
    private int _selectionWidth  = 1;
    private int _selectionHeight = 1;
    private bool _isSelecting;
    private Point _selectionStartTile;

    // Overlay elements — reused, not recreated on zoom
    private readonly Rectangle _selectionRect = new()
    {
        Stroke          = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
        StrokeThickness = 1,
        Fill            = new SolidColorBrush(Color.FromArgb(40, 0x8E, 0xFF, 0x71)),
        IsHitTestVisible = false
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a tileset from a MapIndex and palette.
    /// Call once when the asset is loaded or changed.
    /// </summary>
    public void Load(byte[] mapIndex, IReadOnlyList<HardwareColor> palette, int totalTiles)
    {
        _mapIndex   = mapIndex;
        _palette    = palette;
        _totalTiles = totalTiles;

        RebuildBitmap();
        Render();
    }

    /// <summary>
    /// Updates the palette and re-renders.
    /// Call when the user changes the palette slot or edits colors externally.
    /// Does not reload MapIndex — only the color lookup table changes.
    /// </summary>
    public void Refresh(IReadOnlyList<HardwareColor> newPalette)
    {
        if (_mapIndex == null) return;
        _palette = newPalette;
        RebuildBitmap();
        Render();
    }

    /// <summary>Currently selected tile IDs (read-only snapshot).</summary>
    public IReadOnlyList<int> SelectedTileIds => _selectedTileIds.AsReadOnly();

    /// <summary>Width of the current selection block in tiles.</summary>
    public int SelectionWidth => _selectionWidth;

    /// <summary>Height of the current selection block in tiles.</summary>
    public int SelectionHeight => _selectionHeight;

    // ── Bitmap rendering ──────────────────────────────────────────────────────

    private void RebuildBitmap()
    {
        if (_mapIndex == null || _palette == null || _totalTiles == 0) return;

        int rows        = (int)Math.Ceiling(_totalTiles / (double)Columns);
        int bitmapW     = Columns * TileSize;
        int bitmapH     = rows   * TileSize;

        // ImageWidthInPixels for IndexedBitmapRenderer is the full tileset width
        _tilesetBitmap = IndexedBitmapRenderer.Render(
            _mapIndex, _palette,
            bitmapW, bitmapH,
            _tilesetBitmap); // reuse existing if dimensions match
    }

    // ── Canvas rendering ──────────────────────────────────────────────────────

    private void Render()
    {
        if (_tilesetBitmap == null) return;

        TilesetCanvas.Children.Clear();

        double scaledTile = TileSize * ZoomFactor;
        int rows = (int)Math.Ceiling(_totalTiles / (double)Columns);

        TilesetCanvas.Width  = Columns * scaledTile;
        TilesetCanvas.Height = rows    * scaledTile;

        // Tileset image — scaled via Width/Height, NearestNeighbor
        var img = new Image
        {
            Width   = TilesetCanvas.Width,
            Height  = TilesetCanvas.Height,
            Source  = _tilesetBitmap,
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        TilesetCanvas.Children.Add(img);

        // Grid overlay — 1px logical, does not scale
        if (GridVisible)
            DrawGrid(scaledTile, rows);

        // Selection overlay
        TilesetCanvas.Children.Add(_selectionRect);
        UpdateSelectionOverlay();
    }

    private void DrawGrid(double scaledTile, int rows)
    {
        var brush = new SolidColorBrush(GridColor);
        brush.Freeze();

        for (int x = 0; x <= Columns; x++)
        {
            TilesetCanvas.Children.Add(new Line
            {
                X1 = x * scaledTile, Y1 = 0,
                X2 = x * scaledTile, Y2 = rows * Columns * scaledTile / Columns,
                Stroke = brush, StrokeThickness = 1,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            });
        }

        for (int y = 0; y <= rows; y++)
        {
            TilesetCanvas.Children.Add(new Line
            {
                X1 = 0,                  Y1 = y * scaledTile,
                X2 = Columns * scaledTile, Y2 = y * scaledTile,
                Stroke = brush, StrokeThickness = 1,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            });
        }
    }

    private void UpdateSelectionOverlay()
    {
        if (_selectedTileIds.Count == 0)
        {
            _selectionRect.Visibility = Visibility.Collapsed;
            return;
        }

        double scaledTile = TileSize * ZoomFactor;
        int minCol = _selectedTileIds.Min(id => id % Columns);
        int minRow = _selectedTileIds.Min(id => id / Columns);

        _selectionRect.Width      = _selectionWidth  * scaledTile;
        _selectionRect.Height     = _selectionHeight * scaledTile;
        _selectionRect.Visibility = Visibility.Visible;

        Canvas.SetLeft(_selectionRect, minCol * scaledTile);
        Canvas.SetTop(_selectionRect,  minRow * scaledTile);
        Canvas.SetZIndex(_selectionRect, 100);
    }

    // ── Mouse handlers ────────────────────────────────────────────────────────

    private void TilesetCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        int tileId = HitTest(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            // Start rectangular selection
            _isSelecting = true;
            _selectionStartTile = TilePosition(tileId);
            _selectedTileIds = [tileId];
            _selectionWidth = _selectionHeight = 1;
        }
        else
        {
            // Single tile
            _isSelecting = false;
            _selectedTileIds = [tileId];
            _selectionWidth = _selectionHeight = 1;
        }

        TilesetCanvas.CaptureMouse();
        UpdateSelectionOverlay();
        FireSelectionChanged();
    }

    private void TilesetCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting || e.LeftButton != MouseButtonState.Pressed) return;

        int tileId = HitTest(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        UpdateRectangularSelection(_selectionStartTile, TilePosition(tileId));
    }

    private void TilesetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = false;
        TilesetCanvas.ReleaseMouseCapture();
    }

    // ── Selection logic ───────────────────────────────────────────────────────

    private void UpdateRectangularSelection(Point start, Point end)
    {
        int minCol = (int)Math.Min(start.X, end.X);
        int maxCol = (int)Math.Max(start.X, end.X);
        int minRow = (int)Math.Min(start.Y, end.Y);
        int maxRow = (int)Math.Max(start.Y, end.Y);

        _selectionWidth  = maxCol - minCol + 1;
        _selectionHeight = maxRow - minRow + 1;
        _selectedTileIds.Clear();

        for (int row = minRow; row <= maxRow; row++)
        {
            for (int col = minCol; col <= maxCol; col++)
            {
                int id = row * Columns + col;
                if (id < _totalTiles)
                    _selectedTileIds.Add(id);
            }
        }

        UpdateSelectionOverlay();
        FireSelectionChanged();
    }

    private void FireSelectionChanged()
    {
        TileSelectionChanged?.Invoke(this, new TileSelectionChangedEventArgs(
            _selectedTileIds.AsReadOnly(),
            _selectionWidth,
            _selectionHeight));
    }

    // ── Hit testing ───────────────────────────────────────────────────────────

    private int HitTest(Point canvasPos)
    {
        double scaledTile = TileSize * ZoomFactor;
        int col = (int)(canvasPos.X / scaledTile);
        int row = (int)(canvasPos.Y / scaledTile);

        if (col < 0 || col >= Columns) return -1;

        int id = row * Columns + col;
        return id < _totalTiles ? id : -1;
    }

    private Point TilePosition(int tileId)
        => new(tileId % Columns, tileId / Columns);

    // ── Dependency property callbacks ─────────────────────────────────────────

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IndexedTilesetControl ctrl && ctrl._tilesetBitmap != null)
            ctrl.Render();
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IndexedTilesetControl ctrl && ctrl._tilesetBitmap != null)
            ctrl.Render(); // zoom only rescales — no bitmap rebuild needed
    }
}

/// <summary>
/// Event args for tile selection changes in IndexedTilesetControl.
/// </summary>
public class TileSelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<int> SelectedTileIds { get; }
    public int SelectionWidth  { get; }
    public int SelectionHeight { get; }

    public TileSelectionChangedEventArgs(
        IReadOnlyList<int> selectedTileIds,
        int selectionWidth,
        int selectionHeight)
    {
        SelectedTileIds = selectedTileIds;
        SelectionWidth  = selectionWidth;
        SelectionHeight = selectionHeight;
    }
}
