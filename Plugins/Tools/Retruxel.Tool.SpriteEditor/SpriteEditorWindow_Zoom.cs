using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Retruxel.Tool.SpriteEditor;

/// <summary>
/// Zoom logic for the tileset canvas and the composition canvas.
/// Mirrors TilemapEditor zoom model exactly.
/// </summary>
public partial class SpriteEditorWindow
{
    private static readonly double[] TilesetPresets = { 0.5, 1.0, 2.0, 4.0 };
    private static readonly double[] CanvasPresets  = { 0.5, 1.0, 2.0, 4.0 };

    private const double ZoomMin = 0.5;
    private const double ZoomMax = 4.0;

    // ── Zoom button handlers ───────────────────────────────────────────────

    private void BtnTilesetZoom_Click(object sender, RoutedEventArgs e)
        => ShowZoomMenu(BtnTilesetZoom, isTileset: true);

    private void BtnCanvasZoom_Click(object sender, RoutedEventArgs e)
        => ShowZoomMenu(BtnCanvasZoom, isTileset: false);

    private void ShowZoomMenu(Button anchor, bool isTileset)
    {
        var menu = new ContextMenu { PlacementTarget = anchor, Placement = PlacementMode.Bottom };

        foreach (var z in (isTileset ? TilesetPresets : CanvasPresets))
            AddMenuItem(menu, $"{z * 100:F0}%", null, () => SetZoom(z, isTileset));

        menu.Items.Add(new Separator());
        AddMenuItem(menu, "Zoom In",       "Ctrl++",       () => StepZoom(+1, isTileset));
        AddMenuItem(menu, "Zoom Out",      "Ctrl+-",       () => StepZoom(-1, isTileset));
        AddMenuItem(menu, "Reset",         "Ctrl+0",       () => SetZoom(isTileset ? 1.0 : 2.0, isTileset));
        AddMenuItem(menu, "Fit to Window", "Ctrl+Shift+0", () => ZoomToFit(isTileset));

        menu.IsOpen = true;
    }

    private static void AddMenuItem(ContextMenu menu, string header, string? gesture, Action action)
    {
        var item = new MenuItem { Header = header };
        if (gesture != null) item.InputGestureText = gesture;
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    // ── Zoom application ───────────────────────────────────────────────────

    private void SetZoom(double zoom, bool isTileset)
    {
        zoom = SnapToPreset(zoom, isTileset ? TilesetPresets : CanvasPresets);

        if (isTileset)
        {
            _tilesetZoom   = zoom;
            _tileZoomLevel = zoom;
            UpdateZoomLabel(BtnTilesetZoom, zoom);
            // Re-render tileset canvas at new zoom (same as TilemapEditor.SetZoom)
            RebuildTilesetBitmap();
            RenderTilesetCanvas();
            UpdateTilesetSelectionOverlay();
        }
        else
        {
            _canvasZoom = zoom;
            UpdateZoomLabel(BtnCanvasZoom, zoom);
            ApplyCanvasZoom();
            RenderCanvas();
        }
    }

    private void StepZoom(int direction, bool isTileset)
    {
        var presets = isTileset ? TilesetPresets : CanvasPresets;
        double cur  = isTileset ? _tilesetZoom : _canvasZoom;
        int idx     = FindPresetIndex(cur, presets);
        SetZoom(presets[Math.Clamp(idx + direction, 0, presets.Length - 1)], isTileset);
    }

    private void ZoomToFit(bool isTileset)
    {
        if (isTileset)
        {
            double avail   = TilesetScrollViewer.ActualWidth > 0 ? TilesetScrollViewer.ActualWidth : 208;
            int    tileSize = _target?.Specs.TileWidth ?? 8;
            SetZoom(avail / (tileSize * 16.0), isTileset: true);
        }
        else
        {
            double availW = CanvasScrollViewer.ActualWidth  > 0 ? CanvasScrollViewer.ActualWidth  : 512;
            double availH = CanvasScrollViewer.ActualHeight > 0 ? CanvasScrollViewer.ActualHeight : 512;
            SetZoom(Math.Min(availW / CompositionCanvas.Width, availH / CompositionCanvas.Height), isTileset: false);
        }
    }

    private void ApplyCanvasZoom()
    {
        double size = 256 * _canvasZoom;
        CompositionCanvas.Width  = size;
        CompositionCanvas.Height = size;
    }

    private static void UpdateZoomLabel(Button btn, double zoom)
        => btn.Content = $"{zoom * 100:F0}%";

    // ── Keyboard shortcuts ─────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool tileset = TilesetScrollViewer.IsMouseOver || TilesetCanvas.IsMouseOver;
        bool canvas  = CanvasScrollViewer.IsMouseOver  || CompositionCanvas.IsMouseOver;
        if (!tileset && !canvas) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key is Key.OemPlus  or Key.Add)      { StepZoom(+1, tileset); e.Handled = true; }
            if (e.Key is Key.OemMinus or Key.Subtract)  { StepZoom(-1, tileset); e.Handled = true; }
            if (e.Key is Key.D0       or Key.NumPad0)   { SetZoom(tileset ? 1.0 : 2.0, tileset); e.Handled = true; }
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (e.Key is Key.D0 or Key.NumPad0) { ZoomToFit(tileset); e.Handled = true; }
        }
    }

    // ── Mousewheel ─────────────────────────────────────────────────────────

    private void TilesetScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            StepZoom(e.Delta > 0 ? +1 : -1, isTileset: true);
            e.Handled = true;
        }
    }

    private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            StepZoom(e.Delta > 0 ? +1 : -1, isTileset: false);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            CanvasScrollViewer.ScrollToHorizontalOffset(
                CanvasScrollViewer.HorizontalOffset - e.Delta * 0.5);
            e.Handled = true;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static double SnapToPreset(double zoom, double[] presets)
    {
        double best = presets[0], bestDist = Math.Abs(zoom - best);
        foreach (var p in presets)
        {
            double d = Math.Abs(zoom - p);
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return Math.Clamp(best, ZoomMin, ZoomMax);
    }

    private static int FindPresetIndex(double zoom, double[] presets)
    {
        for (int i = 0; i < presets.Length; i++)
            if (Math.Abs(presets[i] - zoom) < 0.01) return i;
        return 0;
    }

    private int GetCanvasZoom() => (int)_canvasZoom;

    private void InitializeZoomControls()
    {
        TilesetScrollViewer.PreviewMouseWheel += TilesetScrollViewer_PreviewMouseWheel;
        CanvasScrollViewer.PreviewMouseWheel  += CanvasScrollViewer_PreviewMouseWheel;

        UpdateZoomLabel(BtnTilesetZoom, _tilesetZoom);
        UpdateZoomLabel(BtnCanvasZoom,  _canvasZoom);
        ApplyCanvasZoom();
    }
}
