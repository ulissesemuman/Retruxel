using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Retruxel.Tool.SpriteEditor.Helpers;

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

        int snappedX = (int)(dropPosition.X / 8) * 8;
        int snappedY = (int)(dropPosition.Y / 8) * 8;

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

        foreach (var tile in currentFrame.Tiles)
        {
            var tileImage = ExtractTile(tile.TileIndex);
            
            var image = new Image
            {
                Source = tileImage,
                Width = 8,
                Height = 8,
                Stretch = Stretch.None
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            var border = new Border
            {
                Width = 8,
                Height = 8,
                Child = image,
                Tag = tile,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(border, tile.OffsetX);
            Canvas.SetTop(border, tile.OffsetY);

            border.MouseLeftButtonDown += CanvasTile_MouseDown;
            border.MouseMove += CanvasTile_MouseMove;
            border.MouseLeftButtonUp += CanvasTile_MouseUp;

            CompositionCanvas.Children.Add(border);
        }

        DrawGrid();
    }

    private void DrawGrid()
    {
        for (int x = 0; x <= CompositionCanvas.Width; x += 8)
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

        for (int y = 0; y <= CompositionCanvas.Height; y += 8)
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
            
            int snappedX = (int)(position.X / 8) * 8;
            int snappedY = (int)(position.Y / 8) * 8;

            _draggingTile.OffsetX = snappedX;
            _draggingTile.OffsetY = snappedY;

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
}
