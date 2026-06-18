using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow
{
    private void RenderEditorCanvas()
    {
        EditorCanvas.Children.Clear();

        int ts  = _tileSize;
        int z   = _editorZoom;
        double w = ts * z;
        double h = ts * z;

        EditorCanvas.Width  = w;
        EditorCanvas.Height = h;

        // Checkerboard background
        for (int py = 0; py < ts; py++)
        for (int px = 0; px < ts; px++)
        {
            var bg = (px + py) % 2 == 0
                ? Color.FromRgb(45, 45, 45)
                : Color.FromRgb(55, 55, 55);

            var cell = new Rectangle
            {
                Width = z, Height = z,
                Fill = new SolidColorBrush(bg),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(cell, px * z);
            Canvas.SetTop(cell,  py * z);
            EditorCanvas.Children.Add(cell);
        }

        // Pixel colors
        for (int py = 0; py < ts; py++)
        for (int px = 0; px < ts; px++)
        {
            var col = GetColorAt(_pixels[px, py]);
            if (col.A == 0) continue;

            var rect = new Rectangle
            {
                Width = z, Height = z,
                Fill = new SolidColorBrush(col),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, px * z);
            Canvas.SetTop(rect,  py * z);
            EditorCanvas.Children.Add(rect);
        }

        // Grid
        var gridBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        gridBrush.Freeze();
        for (int x = 0; x <= ts; x++)
            EditorCanvas.Children.Add(new Line { X1 = x*z, Y1 = 0, X2 = x*z, Y2 = h, Stroke = gridBrush, StrokeThickness = 1 });
        for (int y = 0; y <= ts; y++)
            EditorCanvas.Children.Add(new Line { X1 = 0, Y1 = y*z, X2 = w, Y2 = y*z, Stroke = gridBrush, StrokeThickness = 1 });

        TxtViewportInfo.Text = $"Tile {_selectedTileIndex} · zoom {z}x";
        UpdateTilePreview();
    }

    // ── Mouse events ──────────────────────────────────────────────────────────

    private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPainting = true;
        _lastPx = _lastPy = -1;
        EditorCanvas.CaptureMouse();
        ApplyToolAt(e.GetPosition(EditorCanvas));
    }

    private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(EditorCanvas);
        int px  = (int)(pos.X / _editorZoom);
        int py  = (int)(pos.Y / _editorZoom);

        TxtPixelCoord.Text = $"({px}, {py})";

        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
        {
            // Interpolate between last and current pixel to avoid skipping
            if (_lastPx >= 0)
            {
                foreach (var (ix, iy) in Bresenham(_lastPx, _lastPy, px, py))
                    PaintPixel(ix, iy);
            }
            else
            {
                ApplyToolAt(pos);
            }
            _lastPx = px; _lastPy = py;
        }
    }

    private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPainting = false;
        _lastPx = _lastPy = -1;
        EditorCanvas.ReleaseMouseCapture();
        WritePixelsBackToAsset();
    }

    // ── Tool dispatch ─────────────────────────────────────────────────────────

    private void ApplyToolAt(Point pos)
    {
        int px = (int)(pos.X / _editorZoom);
        int py = (int)(pos.Y / _editorZoom);

        switch (_activeTool)
        {
            case PixelTool.Pencil:
            case PixelTool.Mirror:
                PaintPixel(px, py);
                break;
            case PixelTool.Bucket:
                FloodFill(px, py, _pixels[Math.Clamp(px,0,_tileSize-1), Math.Clamp(py,0,_tileSize-1)]);
                RenderEditorCanvas();
                break;
        }
        _lastPx = px; _lastPy = py;
    }

    private void PaintPixel(int px, int py)
    {
        int ts = _tileSize;
        if (px < 0 || px >= ts || py < 0 || py >= ts) return;

        _pixels[px, py] = (byte)_selectedColorIndex;

        if (_mirrorX)
        {
            int mx = ts - 1 - px;
            if (mx >= 0 && mx < ts)
                _pixels[mx, py] = (byte)_selectedColorIndex;
        }

        RenderEditorCanvas();
    }

    // ── Flood fill (BFS) ──────────────────────────────────────────────────────

    private void FloodFill(int startX, int startY, byte targetColor)
    {
        int ts = _tileSize;
        if (startX < 0 || startX >= ts || startY < 0 || startY >= ts) return;
        if (targetColor == _selectedColorIndex) return;

        var queue = new System.Collections.Generic.Queue<(int, int)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (x < 0 || x >= ts || y < 0 || y >= ts) continue;
            if (_pixels[x, y] != targetColor) continue;

            _pixels[x, y] = (byte)_selectedColorIndex;
            queue.Enqueue((x+1, y));
            queue.Enqueue((x-1, y));
            queue.Enqueue((x, y+1));
            queue.Enqueue((x, y-1));
        }
    }

    // ── Bresenham line interpolation (prevents skipped pixels on fast drag) ───

    private static System.Collections.Generic.IEnumerable<(int x, int y)> Bresenham(
        int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return (x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }
}
