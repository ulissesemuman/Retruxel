using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Zoom and pan logic for both the tilemap canvas and the tileset canvas.
/// Each canvas has independent zoom state. Both use the same Tiled-style
/// multiplicative zoom step (factor 1.2 per mousewheel tick).
/// </summary>
public partial class TilemapEditorWindow
{
    // Constants 

    private const double ZoomStep = 1.2;   // Tiled factor per wheel tick
    private const double ZoomMin = 0.25;
    private const double ZoomMax = 8.0;
    private const double TilesetZoomMin = 0.25;
    private const double TilesetZoomMax = 8.0;

    // Pan state — canvas 

    private bool _isPanning;
    private Point _panStartMouse;
    private double _panStartScrollH;
    private double _panStartScrollV;

    // Pan state — tileset 

    private bool _isTilesetPanning;
    private Point _tilesetPanStartMouse;
    private double _tilesetPanStartScrollH;
    private double _tilesetPanStartScrollV;

    // Zoom button menu — canvas 

    private void BtnCanvasZoom_Click(object sender, RoutedEventArgs e)
 => ShowZoomMenu(BtnCanvasZoom, isCanvas: true);

    private void BtnTilesetZoom_Click(object sender, RoutedEventArgs e)
 => ShowZoomMenu(BtnTilesetZoom, isCanvas: false);

    private void ShowZoomMenu(Button anchor, bool isCanvas)
    {
        var menu = new ContextMenu { PlacementTarget = anchor, Placement = PlacementMode.Bottom };

        void AddPreset(string label, double zoom) =>
            AddMenuItem(menu, label, null, () => SetZoom(zoom, isCanvas));

        AddPreset("25%", 0.25);
        AddPreset("50%", 0.50);
        AddPreset("100%", 1.00);
        AddPreset("200%", 2.00);
        AddPreset("400%", 4.00);
        menu.Items.Add(new Separator());
        AddMenuItem(menu, "Zoom In", "Ctrl++", () => StepZoom(+1, isCanvas));
        AddMenuItem(menu, "Zoom Out", "Ctrl+-", () => StepZoom(-1, isCanvas));
        AddMenuItem(menu, "Reset", "Ctrl+0", () => SetZoom(1.0, isCanvas));
        AddMenuItem(menu, "Fit to Window", "Ctrl+Shift+0", () => ZoomToFit(isCanvas));

        menu.IsOpen = true;
    }

    private static void AddMenuItem(ContextMenu menu, string header, string? gesture, Action action)
    {
        var item = new MenuItem { Header = header };
        if (gesture != null) item.InputGestureText = gesture;
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    // Zoom application 

    private void SetZoom(double zoom, bool isCanvas)
    {
        if (isCanvas)
        {
            _canvasZoom = Math.Clamp(zoom, ZoomMin, ZoomMax);
            UpdateCanvasZoomLabel();
            RenderCanvas();
        }
        else
        {
            _tileZoomLevel = Math.Clamp(zoom, TilesetZoomMin, TilesetZoomMax);
            UpdateTilesetZoomLabel();
            RenderTilesetCanvas();
        }
    }

    private void StepZoom(int direction, bool isCanvas)
    {
        double current = isCanvas ? _canvasZoom : _tileZoomLevel;
        double next = direction > 0 ? current * ZoomStep : current / ZoomStep;
        SetZoom(next, isCanvas);
    }

    private void ZoomToFit(bool isCanvas)
    {
        if (isCanvas)
        {
            double availW = CanvasScrollViewer.ActualWidth;
            double availH = CanvasScrollViewer.ActualHeight;
            int tileSize = _target.Specs.TileWidth;
            int mapW = int.Parse(TxtWidth.Text);
            int mapH = int.Parse(TxtHeight.Text);

            double fitZoom = Math.Min(availW / (mapW * tileSize), availH / (mapH * tileSize));
            SetZoom(fitZoom, isCanvas: true);
        }
        else
        {
            double availW = TilesetScrollViewer.ActualWidth;
            double availH = TilesetScrollViewer.ActualHeight;
            int tileSize = _target.Specs.TileWidth;

            // Tileset is always 16 columns wide
            double fitZoom = availW / (16 * tileSize);
            SetZoom(fitZoom, isCanvas: false);
        }
    }

    private void UpdateCanvasZoomLabel()
 => BtnCanvasZoom.Content = $"{_canvasZoom * 100:F0}%";

    private void UpdateTilesetZoomLabel()
 => BtnTilesetZoom.Content = $"{_tileZoomLevel * 100:F0}%";

    // Keyboard shortcuts 

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool canvas = TilemapCanvas.IsMouseOver || CanvasScrollViewer.IsMouseOver;
        bool tileset = TilesetCanvas.IsMouseOver || TilesetScrollViewer.IsMouseOver;
        bool any = canvas || tileset;

        if (!any) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.OemPlus || e.Key == Key.Add) { StepZoom(+1, canvas); e.Handled = true; }
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract) { StepZoom(-1, canvas); e.Handled = true; }
            if (e.Key == Key.D0 || e.Key == Key.NumPad0) { SetZoom(1.0, canvas); e.Handled = true; }
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (e.Key == Key.D0 || e.Key == Key.NumPad0) { ZoomToFit(canvas); e.Handled = true; }
        }

        // Numpad zoom (no modifier required — Tiled convention)
        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            if (e.Key == Key.Add || e.Key == Key.OemPlus) { StepZoom(+1, canvas); e.Handled = true; }
            if (e.Key == Key.Subtract || e.Key == Key.OemMinus) { StepZoom(-1, canvas); e.Handled = true; }
            if (e.Key == Key.NumPad0) { SetZoom(1.0, canvas); e.Handled = true; }
            if (e.Key == Key.Home && canvas) { ScrollToOrigin(CanvasScrollViewer); e.Handled = true; }
            if (e.Key == Key.Home && tileset) { ScrollToOrigin(TilesetScrollViewer); e.Handled = true; }

            // Arrow pan
            double panStep = 32;
            if (canvas)
            {
                if (e.Key == Key.Left) { CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset - panStep); e.Handled = true; }
                if (e.Key == Key.Right) { CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + panStep); e.Handled = true; }
                if (e.Key == Key.Up) { CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset - panStep); e.Handled = true; }
                if (e.Key == Key.Down) { CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + panStep); e.Handled = true; }
            }
        }
    }

    private static void ScrollToOrigin(ScrollViewer sv)
    {
        sv.ScrollToHorizontalOffset(0);
        sv.ScrollToVerticalOffset(0);
    }

    // Canvas mousewheel 

    /// <summary>
    /// Called from Canvas_MouseWheel (registered in TilemapEditorWindow_Canvas.cs).
    /// Handles zoom (Ctrl), horizontal scroll (Shift), and vertical scroll (none).
    /// </summary>
    internal void HandleCanvasMouseWheel(MouseWheelEventArgs e)
    {
        int ticks = e.Delta / 120; // positive = up = zoom in / scroll up

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Zoom centered on mouse position
            Point mousePos = e.GetPosition(TilemapCanvas);
            ZoomCanvasAtPoint(ticks > 0 ? _canvasZoom * ZoomStep : _canvasZoom / ZoomStep, mousePos);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            // Horizontal scroll
            CanvasScrollViewer.ScrollToHorizontalOffset(
                CanvasScrollViewer.HorizontalOffset - e.Delta * 0.5);
            e.Handled = true;
        }
        else
        {
            // Vertical scroll — let ScrollViewer handle naturally
            // (don't mark as handled so WPF default scrolls)
        }
    }

    private void ZoomCanvasAtPoint(double newZoom, Point mouseCanvas)
    {
        newZoom = Math.Clamp(newZoom, ZoomMin, ZoomMax);

        // Compute scroll offset so the tile under the mouse stays fixed
        double ratio = newZoom / _canvasZoom;
        double newScrollH = (CanvasScrollViewer.HorizontalOffset + mouseCanvas.X) * ratio - mouseCanvas.X;
        double newScrollV = (CanvasScrollViewer.VerticalOffset + mouseCanvas.Y) * ratio - mouseCanvas.Y;

        _canvasZoom = newZoom;
        UpdateCanvasZoomLabel();
        RenderCanvas();

        // Apply scroll after render (canvas size updated)
        CanvasScrollViewer.Dispatcher.InvokeAsync(() =>
        {
            CanvasScrollViewer.ScrollToHorizontalOffset(Math.Max(0, newScrollH));
            CanvasScrollViewer.ScrollToVerticalOffset(Math.Max(0, newScrollV));
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // Canvas pan (left button or middle button in Navigate mode) 

    internal void HandleCanvasPanStart(MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panStartMouse = e.GetPosition(CanvasScrollViewer);
        _panStartScrollH = CanvasScrollViewer.HorizontalOffset;
        _panStartScrollV = CanvasScrollViewer.VerticalOffset;
        TilemapCanvas.CaptureMouse();
        TilemapCanvas.Cursor = Cursors.SizeAll;
    }

    internal void HandleCanvasPanMove(MouseEventArgs e)
    {
        if (!_isPanning) return;
        Point cur = e.GetPosition(CanvasScrollViewer);
        double dx = _panStartMouse.X - cur.X;
        double dy = _panStartMouse.Y - cur.Y;
        CanvasScrollViewer.ScrollToHorizontalOffset(_panStartScrollH + dx);
        CanvasScrollViewer.ScrollToVerticalOffset(_panStartScrollV + dy);
    }

    internal void HandleCanvasPanEnd()
    {
        if (!_isPanning) return;
        _isPanning = false;
        TilemapCanvas.ReleaseMouseCapture();
        TilemapCanvas.Cursor = Cursors.Arrow;
    }

    // Tileset mousewheel 

    internal void HandleTilesetMouseWheel(MouseWheelEventArgs e)
    {
        int ticks = e.Delta / 120;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            Point mousePos = e.GetPosition(TilesetCanvas);
            ZoomTilesetAtPoint(ticks > 0 ? _tileZoomLevel * ZoomStep : _tileZoomLevel / ZoomStep, mousePos);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            TilesetScrollViewer.ScrollToHorizontalOffset(
                TilesetScrollViewer.HorizontalOffset - e.Delta * 0.5);
            e.Handled = true;
        }
        // plain scroll: let ScrollViewer handle
    }

    private void ZoomTilesetAtPoint(double newZoom, Point mouseTileset)
    {
        newZoom = Math.Clamp(newZoom, TilesetZoomMin, TilesetZoomMax);

        double ratio = newZoom / _tileZoomLevel;
        double newScrollH = (TilesetScrollViewer.HorizontalOffset + mouseTileset.X) * ratio - mouseTileset.X;
        double newScrollV = (TilesetScrollViewer.VerticalOffset + mouseTileset.Y) * ratio - mouseTileset.Y;

        _tileZoomLevel = newZoom;
        UpdateTilesetZoomLabel();
        RenderTilesetCanvas();

        TilesetScrollViewer.Dispatcher.InvokeAsync(() =>
        {
            TilesetScrollViewer.ScrollToHorizontalOffset(Math.Max(0, newScrollH));
            TilesetScrollViewer.ScrollToVerticalOffset(Math.Max(0, newScrollV));
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // Tileset pan 

    internal void HandleTilesetPanStart(MouseButtonEventArgs e)
    {
        _isTilesetPanning = true;
        _tilesetPanStartMouse = e.GetPosition(TilesetScrollViewer);
        _tilesetPanStartScrollH = TilesetScrollViewer.HorizontalOffset;
        _tilesetPanStartScrollV = TilesetScrollViewer.VerticalOffset;
        TilesetCanvas.CaptureMouse();
        TilesetCanvas.Cursor = Cursors.SizeAll;
    }

    internal void HandleTilesetPanMove(MouseEventArgs e)
    {
        if (!_isTilesetPanning) return;
        Point cur = e.GetPosition(TilesetScrollViewer);
        double dx = _tilesetPanStartMouse.X - cur.X;
        double dy = _tilesetPanStartMouse.Y - cur.Y;
        TilesetScrollViewer.ScrollToHorizontalOffset(_tilesetPanStartScrollH + dx);
        TilesetScrollViewer.ScrollToVerticalOffset(_tilesetPanStartScrollV + dy);
    }

    internal void HandleTilesetPanEnd()
    {
        if (!_isTilesetPanning) return;
        _isTilesetPanning = false;
        TilesetCanvas.ReleaseMouseCapture();
        TilesetCanvas.Cursor = Cursors.Arrow;
    }
}
