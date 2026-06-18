using Retruxel.Core.Models;
using SkiaSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    // Minimum canvas size in tiles when the plane is empty.
    // Shows a generous work area even before the first tile is placed.
    private const int MinCanvasTiles = 4;

    private BitmapSource? GetTileSource(TileEntry entry)
        => _tilesetRenderer.ExtractTile(entry);

    private void RenderCanvas()
    {
        PlaneCanvas.Children.Clear();

        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        // Canvas size = bounding box of all placed tiles + comfortable margin,
        // but at minimum covers the viewport rectangle (so it's always visible).
        int viewportW = _planeSpecs.DefaultWidth;
        int viewportH = _planeSpecs.DefaultHeight;

        int extentX = _planeData.Width;
        int extentY = _planeData.Height;

        // Ensure the canvas is large enough to show the viewport at its current offset,
        // plus a margin of one screen beyond the extent.
        int canvasW = System.Math.Max(extentX + viewportW, _mapOffsetX + viewportW + MinCanvasTiles);
        int canvasH = System.Math.Max(extentY + viewportH, _mapOffsetY + viewportH + MinCanvasTiles);

        // Never shrink below one viewport size.
        canvasW = System.Math.Max(canvasW, viewportW + MinCanvasTiles);
        canvasH = System.Math.Max(canvasH, viewportH + MinCanvasTiles);

        PlaneCanvas.Width  = canvasW * scaledTileSize;
        PlaneCanvas.Height = canvasH * scaledTileSize;

        if (_tilesetRenderer.Image != null && _planeData.LayerCount > _currentLayerIndex)
        {
            int maxTileId = _tilesetRenderer.TotalTiles - 1;
            int outOfRangeCount = 0;

            // Sparse iteration — only visit placed tiles.
            foreach (var (x, y, entry) in _planeData.GetLayerSparse(_currentLayerIndex))
            {
                if (x < 0 || y < 0 || x >= canvasW || y >= canvasH) continue;

                if (!entry.IsEmpty)
                {
                    if (entry.TileIndex > maxTileId)
                        outOfRangeCount++;

                    RenderTileAt(x, y, entry, scaledTileSize);
                }
            }

            if (outOfRangeCount > 0)
            {
                var warningLabel = new TextBlock
                {
                    Text = $"⚠ {outOfRangeCount} tile(s) out of range (shown as black)",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    Padding = new Thickness(4, 2, 4, 2),
                    IsHitTestVisible = false
                };

                Canvas.SetRight(warningLabel, 8);
                Canvas.SetTop  (warningLabel, 8);
                PlaneCanvas.Children.Add(warningLabel);
            }
        }

        // Grid lines across the full canvas.
        var gridBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
        gridBrush.Freeze();

        for (int x = 0; x <= canvasW; x++)
        {
            PlaneCanvas.Children.Add(new Line
            {
                X1 = x * scaledTileSize, Y1 = 0,
                X2 = x * scaledTileSize, Y2 = canvasH * scaledTileSize,
                Stroke = gridBrush, StrokeThickness = 1
            });
        }

        for (int y = 0; y <= canvasH; y++)
        {
            PlaneCanvas.Children.Add(new Line
            {
                X1 = 0,                     Y1 = y * scaledTileSize,
                X2 = canvasW * scaledTileSize, Y2 = y * scaledTileSize,
                Stroke = gridBrush, StrokeThickness = 1
            });
        }

        DrawViewportOverlay(scaledTileSize);
        DrawCollisionOverlay();

        // Keep dimension display in sync with actual bounding box.
        int displayW = _planeData.Width  > 0 ? _planeData.Width  : viewportW;
        int displayH = _planeData.Height > 0 ? _planeData.Height : viewportH;
        TxtWidth.Text  = displayW.ToString();
        TxtHeight.Text = displayH.ToString();
    }

    private void DrawViewportOverlay(double scaledTileSize)
    {
        int viewportWidth  = _planeSpecs.DefaultWidth;
        int viewportHeight = _planeSpecs.DefaultHeight;

        double rectWidth  = viewportWidth  * scaledTileSize;
        double rectHeight = viewportHeight * scaledTileSize;
        double offsetX    = _mapOffsetX * scaledTileSize;
        double offsetY    = _mapOffsetY * scaledTileSize;

        var viewportRect = new Rectangle
        {
            Width           = rectWidth,
            Height          = rectHeight,
            Stroke          = new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D)),
            StrokeThickness = 2,
            Fill            = null,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(viewportRect, offsetX);
        Canvas.SetTop (viewportRect, offsetY);
        PlaneCanvas.Children.Add(viewportRect);

        TxtViewportInfo.Text = $"VIEWPORT: {viewportWidth}×{viewportHeight} | Offset: {_mapOffsetX},{_mapOffsetY}";
    }

    private void RenderTileAt(int x, int y, TileEntry entry, double scaledTileSize)
    {
        if (_tilesetRenderer.Image == null || entry.IsEmpty) return;

        var source = GetTileSource(entry);
        if (source == null) return;

        var image = new Image
        {
            Width  = scaledTileSize,
            Height = scaledTileSize,
            Source = source,
            Stretch = System.Windows.Media.Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        Canvas.SetLeft(image, x * scaledTileSize);
        Canvas.SetTop (image, y * scaledTileSize);
        PlaneCanvas.Children.Add(image);
    }

    // ── Mouse events ──────────────────────────────────────────────────────────

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleCanvasPanStart(e);
            return;
        }

        _isPainting = true;
        Point position = e.GetPosition(PlaneCanvas);
        PaintAt(position);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleCanvasPanMove(e);
            return;
        }

        Point position = e.GetPosition(PlaneCanvas);
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        // Show preview for any non-negative canvas position.
        if (tileX >= 0 && tileY >= 0)
            ShowPaintPreview(tileX, tileY);
        else
            HidePaintPreview();

        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            PaintAt(position);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleCanvasPanEnd();
            return;
        }

        _isPainting = false;
    }

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        => HandleCanvasMouseWheel(e);

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            HandleCanvasPanStart(e);
            e.Handled = true;
        }
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            HandleCanvasPanEnd();
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {
        HidePaintPreview();
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        int previousTileId = _selectedTileId;
        _selectedTileId = -1;
        PaintTile(e.GetPosition(PlaneCanvas));
        _selectedTileId = previousTileId;
    }

    private void PaintTile(Point position)
    {
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        // No hard boundary — any non-negative position is valid.
        if (tileX < 0 || tileY < 0) return;

        var entry = new TileEntry
        {
            TileIndex = _selectedTileId,
            FlipH     = _selectedFlipH,
            FlipV     = _selectedFlipV
        };

        _planeData.SetTile(_currentLayerIndex, tileX, tileY, entry);
        RenderCanvas();
    }

    private void PaintAt(Point position)
    {
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        // No hard boundary — any non-negative position is valid.
        if (tileX < 0 || tileY < 0) return;

        switch (_currentToolMode)
        {
            case ToolMode.Paint:
                PaintSingleTile(tileX, tileY);
                break;
            case ToolMode.MetatilePaint:
                if (_selectedMetatileIndex >= 0 && _selectedMetatileIndex < _metatiles.Count)
                    PlaceMetatile(tileX, tileY, _metatiles[_selectedMetatileIndex]);
                break;
            case ToolMode.BrushPaint:
                if (_selectedBrushIndex >= 0 && _selectedBrushIndex < _brushes.Count)
                    PlaceBrush(tileX, tileY, _brushes[_selectedBrushIndex]);
                break;
        }
    }

    private void PaintSingleTile(int tileX, int tileY)
    {
        int tileIndex = _isAutoTilingEnabled ? ResolveAutoTileIndex(tileX, tileY) : _selectedTileId;
        
        var entry = new TileEntry
        {
            TileIndex = tileIndex,
            FlipH     = _selectedFlipH,
            FlipV     = _selectedFlipV
        };

        _planeData.SetTile(_currentLayerIndex, tileX, tileY, entry);
        RenderCanvas();
    }

    private void PlaceMetatile(int startX, int startY, Metatile metatile)
    {
        for (int y = 0; y < metatile.Height; y++)
        {
            for (int x = 0; x < metatile.Width; x++)
            {
                int tileIndex = metatile.TileIndices[y, x];
                if (tileIndex >= 0)
                {
                    var entry = new TileEntry
                    {
                        TileIndex = tileIndex,
                        FlipH     = _selectedFlipH,
                        FlipV     = _selectedFlipV
                    };
                    _planeData.SetTile(_currentLayerIndex, startX + x, startY + y, entry);
                }
            }
        }
        RenderCanvas();
    }

    private void PlaceBrush(int startX, int startY, Brush brush)
    {
        foreach (var brushTile in brush.Tiles)
        {
            int tileX = startX + brushTile.OffsetX;
            int tileY = startY + brushTile.OffsetY;

            var entry = new TileEntry
            {
                TileIndex = brushTile.TileIndex,
                FlipH     = brushTile.FlipH,
                FlipV     = brushTile.FlipV,
                Rotation  = brushTile.Rotation
            };
            _planeData.SetTile(_currentLayerIndex, tileX, tileY, entry);
        }
        RenderCanvas();
    }
}
