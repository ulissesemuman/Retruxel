using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Retruxel.Tool.SpriteEditor;

/// <summary>
/// Zoom and pan logic for both the tileset panel and the composition canvas.
/// Mirrors the TilemapEditor zoom model: preset-based zoom via a dropdown button,
/// Ctrl+Wheel, and keyboard shortcuts (Ctrl++/-, Ctrl+0, Ctrl+Shift+0).
///
/// Zoom values are constrained to integer-friendly presets so that tile
/// coordinates (multiples of 8px) always snap cleanly to screen pixels.
/// Free fractional zoom is intentionally not supported here.
/// </summary>
public partial class SpriteEditorWindow
{
    // ── Presets ────────────────────────────────────────────────────────────
    // Must be kept as powers of 0.5 so 8*zoom is always a whole number.

    private static readonly double[] TilesetPresets = { 0.5, 1.0, 2.0, 4.0 };
    private static readonly double[] CanvasPresets  = { 0.5, 1.0, 2.0, 4.0 };

    private const double ZoomMin = 0.5;
    private const double ZoomMax = 4.0;

    // ── Zoom button click handlers ─────────────────────────────────────────

    private void BtnTilesetZoom_Click(object sender, RoutedEventArgs e)
        => ShowZoomMenu(BtnTilesetZoom, isTileset: true);

    private void BtnCanvasZoom_Click(object sender, RoutedEventArgs e)
        => ShowZoomMenu(BtnCanvasZoom, isTileset: false);

    private void ShowZoomMenu(Button anchor, bool isTileset)
    {
        var menu = new ContextMenu { PlacementTarget = anchor, Placement = PlacementMode.Bottom };

        var presets = isTileset ? TilesetPresets : CanvasPresets;
        foreach (var z in presets)
            AddMenuItem(menu, $"{z * 100:F0}%", null, () => SetZoom(z, isTileset));

        menu.Items.Add(new Separator());
        AddMenuItem(menu, "Zoom In",       "Ctrl++",        () => StepZoom(+1, isTileset));
        AddMenuItem(menu, "Zoom Out",      "Ctrl+-",        () => StepZoom(-1, isTileset));
        AddMenuItem(menu, "Reset",         "Ctrl+0",        () => SetZoom(isTileset ? 1.0 : 2.0, isTileset));
        AddMenuItem(menu, "Fit to Window", "Ctrl+Shift+0",  () => ZoomToFit(isTileset));

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
        // Snap to nearest valid preset (avoids floating-point drift)
        var presets = isTileset ? TilesetPresets : CanvasPresets;
        zoom = SnapToPreset(zoom, presets);

        if (isTileset)
        {
            _tilesetZoom = zoom;
            _tileZoomLevel = zoom; // _tileZoomLevel is used by ExtractTile / RenderTileset
            UpdateZoomLabel(BtnTilesetZoom, zoom);
            RefreshTilesetWithPalette();
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
        double current = isTileset ? _tilesetZoom : _canvasZoom;

        int idx = FindPresetIndex(current, presets);
        int next = Math.Clamp(idx + direction, 0, presets.Length - 1);
        SetZoom(presets[next], isTileset);
    }

    private void ZoomToFit(bool isTileset)
    {
        if (isTileset)
        {
            // Tileset panel is 240px wide; 16px margin each side → 208px usable
            double availW  = TilesetScrollViewer.ActualWidth > 0 ? TilesetScrollViewer.ActualWidth : 208;
            int    tileSize = _target?.Specs.TileWidth ?? 8;
            // Tileset WrapPanel is 208px / tileColumns; just snap to best preset
            double fit = availW / (tileSize * 16.0);
            SetZoom(fit, isTileset: true);
        }
        else
        {
            double availW = CanvasScrollViewer.ActualWidth  > 0 ? CanvasScrollViewer.ActualWidth  : 512;
            double availH = CanvasScrollViewer.ActualHeight > 0 ? CanvasScrollViewer.ActualHeight : 512;
            double fit = Math.Min(availW / CompositionCanvas.Width,
                                  availH / CompositionCanvas.Height);
            SetZoom(fit, isTileset: false);
        }
    }

    /// <summary>
    /// Resizes CompositionCanvas to match the current canvas zoom.
    /// Canvas is always 256×256 logical pixels (32×32 tiles of 8px each).
    /// </summary>
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

        bool tileset = TilesetScrollViewer.IsMouseOver || TilesetItemsControl.IsMouseOver;
        bool canvas  = CanvasScrollViewer.IsMouseOver  || CompositionCanvas.IsMouseOver;
        bool any     = tileset || canvas;

        if (!any) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key is Key.OemPlus or Key.Add)
                { StepZoom(+1, tileset); e.Handled = true; }

            if (e.Key is Key.OemMinus or Key.Subtract)
                { StepZoom(-1, tileset); e.Handled = true; }

            if (e.Key is Key.D0 or Key.NumPad0)
                { SetZoom(tileset ? 1.0 : 2.0, tileset); e.Handled = true; }
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (e.Key is Key.D0 or Key.NumPad0)
                { ZoomToFit(tileset); e.Handled = true; }
        }
    }

    // ── Mousewheel (Ctrl = zoom, else scroll) ──────────────────────────────

    private void TilesetScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            StepZoom(e.Delta > 0 ? +1 : -1, isTileset: true);
            e.Handled = true;
        }
        // plain scroll: let ScrollViewer handle naturally
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
        double best = presets[0];
        double bestDist = Math.Abs(zoom - best);
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
            if (Math.Abs(presets[i] - zoom) < 0.01)
                return i;
        return 0;
    }

    // ── Accessor used by Canvas and Tileset render code ────────────────────

    /// <summary>Returns canvas zoom as int (e.g. 2 for 200%). Used for tile snapping.</summary>
    private int GetCanvasZoom() => (int)_canvasZoom;

    /// <summary>Returns tileset tile display size in pixels.</summary>
    private int GetTileDisplaySize() => (int)(8 * _tilesetZoom);

    // ── Initialization ─────────────────────────────────────────────────────

    private void InitializeZoomControls()
    {
        // Wire mousewheel handlers
        TilesetScrollViewer.PreviewMouseWheel += TilesetScrollViewer_PreviewMouseWheel;
        CanvasScrollViewer.PreviewMouseWheel  += CanvasScrollViewer_PreviewMouseWheel;

        // Apply initial zoom values (set as field defaults: _tilesetZoom=1.0, _canvasZoom=2.0)
        UpdateZoomLabel(BtnTilesetZoom, _tilesetZoom);
        UpdateZoomLabel(BtnCanvasZoom,  _canvasZoom);
        ApplyCanvasZoom();
    }
}
