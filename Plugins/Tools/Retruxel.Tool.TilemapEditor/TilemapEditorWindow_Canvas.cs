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
    private BitmapSource? GetTileSource(TileEntry entry)
        => _tilesetRenderer.ExtractTile(entry);

    private void RenderCanvas()
    {
        PlaneCanvas.Children.Clear();

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);
        int tileSize = _target.Specs.TileWidth;

        double scaledTileSize = tileSize * _canvasZoom;

        PlaneCanvas.Width = width * scaledTileSize;
        PlaneCanvas.Height = height * scaledTileSize;

        if (_tilesetRenderer.Image != null && _planeData.LayerCount > _currentLayerIndex)
        {
            var currentLayer = _planeData.GetLayer(_currentLayerIndex);
            int expectedSize = width * height;

            if (currentLayer.Length != expectedSize)
            {
                _planeData.Resize(width, height);
                currentLayer = _planeData.GetLayer(_currentLayerIndex);
            }

            int maxTileId = _tilesetRenderer.TotalTiles - 1;
            int outOfRangeCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index < currentLayer.Length)
                    {
                        var entry = currentLayer[index];
                        if (!entry.IsEmpty)
                        {
                            if (entry.TileIndex > maxTileId)
                                outOfRangeCount++;

                            RenderTileAt(x, y, entry, scaledTileSize);
                        }
                    }
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

        // Grid lines
        var gridBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
        gridBrush.Freeze();

        for (int x = 0; x <= width; x++)
        {
            PlaneCanvas.Children.Add(new Line
            {
                X1 = x * scaledTileSize, Y1 = 0,
                X2 = x * scaledTileSize, Y2 = height * scaledTileSize,
                Stroke = gridBrush, StrokeThickness = 1
            });
        }

        for (int y = 0; y <= height; y++)
        {
            PlaneCanvas.Children.Add(new Line
            {
                X1 = 0,                    Y1 = y * scaledTileSize,
                X2 = width * scaledTileSize, Y2 = y * scaledTileSize,
                Stroke = gridBrush, StrokeThickness = 1
            });
        }

        DrawViewportOverlay(scaledTileSize);
    }

    private void DrawViewportOverlay(double scaledTileSize)
    {
        int viewportWidth  = _planeSpecs.DefaultWidth;
        int viewportHeight = _planeSpecs.DefaultHeight;

        double rectWidth = viewportWidth * scaledTileSize;
        double rectHeight = viewportHeight * scaledTileSize;
        double offsetX = _mapOffsetX * scaledTileSize;
        double offsetY = _mapOffsetY * scaledTileSize;

        var viewportRect = new Rectangle
        {
            Width = rectWidth,
            Height = rectHeight,
            Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D)),
            StrokeThickness = 2,
            Fill = null,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(viewportRect, offsetX);
        Canvas.SetTop(viewportRect, offsetY);
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
            Width = scaledTileSize,
            Height = scaledTileSize,
            Source = source,
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        Canvas.SetLeft(image, x * scaledTileSize);
        Canvas.SetTop (image, y * scaledTileSize);
        PlaneCanvas.Children.Add(image);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleCanvasPanStart(e);
            return;
        }

        _isPainting = true;
        Point position = e.GetPosition(PlaneCanvas);
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        if (_selectedTileIds.Count > 1)
            PlaceTileBlock(tileX, tileY);
        else
            PaintTile(position);
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

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);

        if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
            ShowPaintPreview(tileX, tileY);
        else
            HidePaintPreview();

        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            PaintTile(position);
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

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);

        if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height) return;

        var entry = new TileEntry
        {
            TileIndex = _selectedTileId,
            FlipH = _selectedFlipH,
            FlipV = _selectedFlipV
        };

        _planeData.SetTile(_currentLayerIndex, tileX, tileY, entry);
        RenderCanvas();
    }
}
