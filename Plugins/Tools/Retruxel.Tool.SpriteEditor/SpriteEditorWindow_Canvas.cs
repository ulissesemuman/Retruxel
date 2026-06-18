using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.SpriteEditor.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Retruxel.Core.Models;
using Retruxel.Tool.SpriteEditor.Models;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private SpriteTile? _draggingTile;
    private Image?      _draggingImage;

    // ── Click on CompositionCanvas — place selected tile ──────────────────

    private void CompositionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // If a tile is selected in the tileset, place it at the clicked position.
        // This is an alternative to drag-and-drop for quick placement.
        if (_selectedTileIndex < 0) return;

        var pos      = e.GetPosition(CompositionCanvas);
        int zoom     = GetCanvasZoom();
        int tileSize = _target?.Specs.TileWidth ?? 8;
        int gridSize = tileSize * zoom;

        int snappedX = (int)(pos.X / gridSize) * gridSize / zoom;
        int snappedY = (int)(pos.Y / gridSize) * gridSize / zoom;

        AddTileToCurrentFrame(_selectedTileIndex, snappedX, snappedY);
        e.Handled = true;
    }

    // ── Drag-and-drop from tileset ─────────────────────────────────────────

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
        if (!e.Data.GetDataPresent("TileIndex")) return;

        int tileIndex = (int)e.Data.GetData("TileIndex");
        var pos       = e.GetPosition(CompositionCanvas);
        int zoom      = GetCanvasZoom();
        int gridSize  = 8 * zoom;

        int snappedX = (int)(pos.X / gridSize) * gridSize / zoom;
        int snappedY = (int)(pos.Y / gridSize) * gridSize / zoom;

        AddTileToCurrentFrame(tileIndex, snappedX, snappedY);
    }

    private void AddTileToCurrentFrame(int tileIndex, int x, int y)
    {
        if (_state.Frames.Count == 0) return;

        _state.Frames[_state.CurrentFrameIndex].Tiles.Add(
            new SpriteTile(tileIndex, x, y));

        OnSpriteChanged();
    }

    // ── Composition canvas render ──────────────────────────────────────────

    private void RenderCanvas()
    {
        CompositionCanvas.Children.Clear();

        if (_state.Frames.Count == 0 || _tilesetRenderer.Image == null)
            return;

        var frame = _state.Frames[_state.CurrentFrameIndex];
        int zoom  = GetCanvasZoom();
        int tileSize = _target?.Specs.TileWidth ?? 8;

        RenderOnionSkinFrame(_state.CurrentFrameIndex - 1, Brushes.DeepSkyBlue, _state.OnionSkinPrevious, zoom, tileSize);
        RenderOnionSkinFrame(_state.CurrentFrameIndex + 1, Brushes.Orange, _state.OnionSkinNext, zoom, tileSize);
        RenderFrameTiles(frame, zoom, tileSize, 1.0, null, interactive: true);

        DrawGrid();
        DrawHitboxes();
        UpdateStatusBar();
    }

    private void RenderOnionSkinFrame(int frameIndex, Brush tint, bool enabled, int zoom, int tileSize)
    {
        if (!enabled || frameIndex < 0 || frameIndex >= _state.Frames.Count)
            return;

        RenderFrameTiles(_state.Frames[frameIndex], zoom, tileSize, _state.OnionSkinOpacity, tint, interactive: false);
    }

    private void RenderFrameTiles(SpriteFrame frame, int zoom, int tileSize, double opacity, Brush? tint, bool interactive)
    {
        foreach (var tile in frame.Tiles)
        {
            var source = _tilesetRenderer.ExtractTile(
                new TileEntry { TileIndex = tile.TileIndex });

            if (source == null) continue;

            var image = new Image
            {
                Source  = source,
                Width   = tileSize * zoom,
                Height  = tileSize * zoom,
                Stretch = Stretch.Fill,
                Opacity = opacity
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            FrameworkElement visual = image;
            if (tint is not null)
            {
                var grid = new Grid
                {
                    Width = tileSize * zoom,
                    Height = tileSize * zoom,
                    Opacity = opacity
                };

                image.Opacity = 1.0;
                grid.Children.Add(image);
                grid.Children.Add(new Rectangle
                {
                    Fill = tint,
                    Opacity = 0.32,
                    IsHitTestVisible = false
                });
                visual = grid;
            }

            var border = new Border
            {
                Width  = tileSize * zoom,
                Height = tileSize * zoom,
                Child  = visual,
                Tag    = tile,
                Cursor = interactive ? Cursors.Hand : Cursors.Arrow,
                IsHitTestVisible = interactive
            };

            Canvas.SetLeft(border, tile.OffsetX * zoom);
            Canvas.SetTop (border, tile.OffsetY * zoom);

            if (interactive)
            {
                border.MouseLeftButtonDown += CanvasTile_MouseDown;
                border.MouseMove           += CanvasTile_MouseMove;
                border.MouseLeftButtonUp   += CanvasTile_MouseUp;
            }

            CompositionCanvas.Children.Add(border);
        }
    }

    private void DrawGrid()
    {
        int zoom     = GetCanvasZoom();
        int tileSize = _target?.Specs.TileWidth ?? 8;
        int gridSize = tileSize * zoom;

        var brush = new SolidColorBrush(Color.FromArgb(77, 255, 255, 255));
        brush.Freeze();

        for (double x = 0; x <= CompositionCanvas.Width; x += gridSize)
            CompositionCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = CompositionCanvas.Height,
                Stroke = brush, StrokeThickness = 1
            });

        for (double y = 0; y <= CompositionCanvas.Height; y += gridSize)
            CompositionCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = CompositionCanvas.Width, Y2 = y,
                Stroke = brush, StrokeThickness = 1
            });
    }

    // ── Tile drag on canvas ────────────────────────────────────────────────

    private void CanvasTile_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.Tag is not SpriteTile tile) return;

        if (e.RightButton == MouseButtonState.Pressed)
        {
            RemoveTileFromCurrentFrame(tile);
            e.Handled = true;
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            _draggingTile  = tile;
            _draggingImage = border.Child as Image;
            border.CaptureMouse();
            e.Handled = true;
        }
    }

    private void CanvasTile_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingTile == null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos      = e.GetPosition(CompositionCanvas);
        int zoom     = GetCanvasZoom();
        int tileSize = _target?.Specs.TileWidth ?? 8;
        int gridSize = tileSize * zoom;

        _draggingTile.OffsetX = (int)(pos.X / gridSize) * gridSize / zoom;
        _draggingTile.OffsetY = (int)(pos.Y / gridSize) * gridSize / zoom;

        OnSpriteChanged();
    }

    private void CanvasTile_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingTile == null) return;
        _draggingTile  = null;
        _draggingImage = null;
        (sender as Border)?.ReleaseMouseCapture();
    }

    private void RemoveTileFromCurrentFrame(SpriteTile tile)
    {
        if (_state.Frames.Count == 0) return;
        _state.Frames[_state.CurrentFrameIndex].Tiles.Remove(tile);
        OnSpriteChanged();
    }

    // ── Status bar ─────────────────────────────────────────────────────────

    private void UpdateStatusBar()
    {
        if (_state.Frames.Count == 0)
        {
            TxtFrameInfo.Text  = "Frame: 0/0";
            TxtSpriteSize.Text = "Size: 0×0";
            return;
        }

        var frame = _state.Frames[_state.CurrentFrameIndex];
        TxtFrameInfo.Text = $"Frame: {_state.CurrentFrameIndex + 1}/{_state.Frames.Count}";

        if (frame.Tiles.Count == 0)
        {
            TxtSpriteSize.Text = "Size: 0×0";
            return;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        int ts   = _target?.Specs.TileWidth ?? 8;

        foreach (var t in frame.Tiles)
        {
            if (t.OffsetX      < minX) minX = t.OffsetX;
            if (t.OffsetY      < minY) minY = t.OffsetY;
            if (t.OffsetX + ts > maxX) maxX = t.OffsetX + ts;
            if (t.OffsetY + ts > maxY) maxY = t.OffsetY + ts;
        }

        TxtSpriteSize.Text = $"Size: {maxX - minX}×{maxY - minY}";
    }
}
