using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.SpriteEditor.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private SpriteTile? _draggingTile;
    private Image? _draggingImage;

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("TileIndex"))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("TileIndex"))
            return;

        int tileIndex = (int)e.Data.GetData("TileIndex");
        Point dropPosition = e.GetPosition(CompositionCanvas);
        int zoom = GetCanvasZoom();
        int gridSize = 8 * zoom;

        int snappedX = (int)(dropPosition.X / gridSize) * gridSize / zoom;
        int snappedY = (int)(dropPosition.Y / gridSize) * gridSize / zoom;

        AddTileToCurrentFrame(tileIndex, snappedX, snappedY);
    }

    private void AddTileToCurrentFrame(int tileIndex, int x, int y)
    {
        if (_state.Frames.Count == 0)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];

        var newTile = new SpriteTile
        {
            TileIndex = tileIndex,
            OffsetX = x,
            OffsetY = y
        };

        currentFrame.Tiles.Add(newTile);
        OnSpriteChanged();
    }

    private void RenderCanvas()
    {
        CompositionCanvas.Children.Clear();

        if (_state.Frames.Count == 0 || _tilesetImage == null)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        int zoom = GetCanvasZoom();

        foreach (var tile in currentFrame.Tiles)
        {
            var tileImage = ExtractTile(tile.TileIndex);

            var image = new Image
            {
                Source = ImageProcessing.ConvertSkBitmapToBitmapSource(tileImage),
                Width = 8 * zoom,
                Height = 8 * zoom,
                Stretch = Stretch.None
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            var border = new Border
            {
                Width = 8 * zoom,
                Height = 8 * zoom,
                Child = image,
                Tag = tile,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(border, tile.OffsetX * zoom);
            Canvas.SetTop(border, tile.OffsetY * zoom);

            border.MouseLeftButtonDown += CanvasTile_MouseDown;
            border.MouseMove += CanvasTile_MouseMove;
            border.MouseLeftButtonUp += CanvasTile_MouseUp;

            CompositionCanvas.Children.Add(border);
        }

        DrawGrid();
        DrawHitboxes();
        UpdateStatusBar();
    }

    private void DrawGrid()
    {
        int zoom = GetCanvasZoom();
        int gridSize = 8 * zoom;

        for (int x = 0; x <= CompositionCanvas.Width; x += gridSize)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = CompositionCanvas.Height,
                Stroke = (Brush)FindResource("BrushOnSurfaceVariant"),
                StrokeThickness = 1,
                Opacity = 0.3
            };
            CompositionCanvas.Children.Add(line);
        }

        for (int y = 0; y <= CompositionCanvas.Height; y += gridSize)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = CompositionCanvas.Width,
                Y2 = y,
                Stroke = (Brush)FindResource("BrushOnSurfaceVariant"),
                StrokeThickness = 1,
                Opacity = 0.3
            };
            CompositionCanvas.Children.Add(line);
        }
    }

    private void CanvasTile_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is SpriteTile tile)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                RemoveTileFromCurrentFrame(tile);
                e.Handled = true;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                _draggingTile = tile;
                _draggingImage = border.Child as Image;
                border.CaptureMouse();
                e.Handled = true;
            }
        }
    }

    private void CanvasTile_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingTile != null && e.LeftButton == MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(CompositionCanvas);
            int zoom = GetCanvasZoom();
            int gridSize = 8 * zoom;

            int snappedX = (int)(position.X / gridSize) * gridSize;
            int snappedY = (int)(position.Y / gridSize) * gridSize;

            _draggingTile.OffsetX = snappedX / zoom;
            _draggingTile.OffsetY = snappedY / zoom;

            OnSpriteChanged();
        }
    }

    private void CanvasTile_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingTile != null)
        {
            _draggingTile = null;
            _draggingImage = null;

            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
            }
        }
    }

    private void RemoveTileFromCurrentFrame(SpriteTile tile)
    {
        if (_state.Frames.Count == 0)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        currentFrame.Tiles.Remove(tile);
        OnSpriteChanged();
    }

    private void UpdateStatusBar()
    {
        if (_state.Frames.Count == 0)
        {
            TxtFrameInfo.Text = "Frame: 0/0";
            TxtSpriteSize.Text = "Size: 0×0";
            return;
        }

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        TxtFrameInfo.Text = $"Frame: {_state.CurrentFrameIndex + 1}/{_state.Frames.Count}";

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var tile in currentFrame.Tiles)
        {
            if (tile.OffsetX < minX) minX = tile.OffsetX;
            if (tile.OffsetY < minY) minY = tile.OffsetY;
            if (tile.OffsetX + 8 > maxX) maxX = tile.OffsetX + 8;
            if (tile.OffsetY + 8 > maxY) maxY = tile.OffsetY + 8;
        }

        if (currentFrame.Tiles.Count > 0)
        {
            int width = maxX - minX;
            int height = maxY - minY;
            TxtSpriteSize.Text = $"Size: {width}×{height}";
        }
        else
        {
            TxtSpriteSize.Text = "Size: 0×0";
        }
    }
}
