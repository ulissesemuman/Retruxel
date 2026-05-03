using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void RenderCanvas()
    {
        TilemapCanvas.Children.Clear();

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);
        int tileSize = _target.Specs.TileWidth;

        double scaledTileSize = tileSize * _canvasZoom;

        TilemapCanvas.Width = width * scaledTileSize;
        TilemapCanvas.Height = height * scaledTileSize;

        if (_tilesetRenderer.Image != null && _tilemapData.LayerCount > _currentLayerIndex)
        {
            var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
            int expectedSize = width * height;

            if (currentLayer.Length != expectedSize)
            {
                _tilemapData.Resize(width, height);
                currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
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
                        int tileId = currentLayer[index];
                        if (tileId >= 0)
                        {
                            if (tileId > maxTileId)
                                outOfRangeCount++;

                            RenderTileAt(x, y, tileId, scaledTileSize);
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
                Canvas.SetTop(warningLabel, 8);
                TilemapCanvas.Children.Add(warningLabel);
            }
        }

        for (int x = 0; x <= width; x++)
        {
            var line = new Line
            {
                X1 = x * scaledTileSize,
                Y1 = 0,
                X2 = x * scaledTileSize,
                Y2 = height * scaledTileSize,
                Stroke = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                StrokeThickness = 1
            };
            TilemapCanvas.Children.Add(line);
        }

        for (int y = 0; y <= height; y++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y * scaledTileSize,
                X2 = width * scaledTileSize,
                Y2 = y * scaledTileSize,
                Stroke = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                StrokeThickness = 1
            };
            TilemapCanvas.Children.Add(line);
        }

        DrawViewportOverlay(scaledTileSize);
    }

    private void DrawViewportOverlay(double scaledTileSize)
    {
        var specs = _target.Specs.Tilemap;
        int viewportWidth = specs.DefaultWidth;
        int viewportHeight = specs.DefaultHeight;

        double rectWidth = viewportWidth * scaledTileSize;
        double rectHeight = viewportHeight * scaledTileSize;

        // Calculate offset position in pixels
        double offsetX = _mapOffsetX * scaledTileSize;
        double offsetY = _mapOffsetY * scaledTileSize;

        var viewportRect = new System.Windows.Shapes.Rectangle
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
        TilemapCanvas.Children.Add(viewportRect);

        // Update footer info
        TxtViewportInfo.Text = $"VIEWPORT: {viewportWidth}×{viewportHeight} | Offset: {_mapOffsetX},{_mapOffsetY}";
    }

    private void RenderTileAt(int x, int y, int tileId, double scaledTileSize)
    {
        if (_tilesetRenderer.Image == null || tileId < 0) return;

        var tileImage = _tilesetRenderer.ExtractTile(tileId);
        if (tileImage == null) return;

        var image = new Image
        {
            Width = scaledTileSize,
            Height = scaledTileSize,
            Source = tileImage,
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        Canvas.SetLeft(image, x * scaledTileSize);
        Canvas.SetTop(image, y * scaledTileSize);
        TilemapCanvas.Children.Add(image);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPainting = true;
        Point position = e.GetPosition(TilemapCanvas);
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        // Paint block if multiple tiles selected, otherwise single tile
        if (_selectedTileIds.Count > 1)
            PlaceTileBlock(tileX, tileY);
        else
            PaintTile(position);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        Point position = e.GetPosition(TilemapCanvas);
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);

        // Show paint preview if mouse is within bounds
        if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
            ShowPaintPreview(tileX, tileY);
        else
            HidePaintPreview();

        // Paint if mouse button is pressed
        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            PaintTile(position);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPainting = false;
    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {
        HidePaintPreview();
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        int previousTileId = _selectedTileId;
        _selectedTileId = -1;
        PaintTile(e.GetPosition(TilemapCanvas));
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

        if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height)
            return;

        _tilemapData.SetTile(_currentLayerIndex, tileX, tileY, _selectedTileId);
        RenderCanvas();
    }

    private void BtnZoomFit_Click(object sender, RoutedEventArgs e)
    {
        _canvasZoom = 1.0;
        TxtZoomLevel.Text = "100%";
        RenderCanvas();
    }

    private void BtnZoom100_Click(object sender, RoutedEventArgs e)
    {
        _canvasZoom = 1.0;
        TxtZoomLevel.Text = "100%";
        RenderCanvas();
    }
}
