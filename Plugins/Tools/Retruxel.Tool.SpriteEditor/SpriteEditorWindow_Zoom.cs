using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private double _tilesetZoom = 1.0;
    private double _canvasZoom = 2.0;

    private void InitializeZoomControls()
    {
        // Tileset zoom buttons
        BtnTilesetZoom50.Click += (s, e) => SetTilesetZoom(0.5);
        BtnTilesetZoom100.Click += (s, e) => SetTilesetZoom(1.0);
        BtnTilesetZoom200.Click += (s, e) => SetTilesetZoom(2.0);

        // Canvas zoom buttons
        BtnCanvasZoom50.Click += (s, e) => SetCanvasZoom(0.5);
        BtnCanvasZoom100.Click += (s, e) => SetCanvasZoom(1.0);
        BtnCanvasZoom200.Click += (s, e) => SetCanvasZoom(2.0);
        BtnCanvasZoom400.Click += (s, e) => SetCanvasZoom(4.0);

        // Set initial zoom
        SetTilesetZoom(_tilesetZoom);
        SetCanvasZoom(_canvasZoom);
    }

    private void SetTilesetZoom(double zoom)
    {
        _tilesetZoom = zoom;

        // Update button styles
        BtnTilesetZoom50.Style = (Style)FindResource(zoom == 0.5 ? "ButtonPrimary" : "ButtonSecondary");
        BtnTilesetZoom100.Style = (Style)FindResource(zoom == 1.0 ? "ButtonPrimary" : "ButtonSecondary");
        BtnTilesetZoom200.Style = (Style)FindResource(zoom == 2.0 ? "ButtonPrimary" : "ButtonSecondary");

        RefreshTilesetPreview();
    }

    private void SetCanvasZoom(double zoom)
    {
        _canvasZoom = zoom;

        // Update button styles
        BtnCanvasZoom50.Style = (Style)FindResource(zoom == 0.5 ? "ButtonPrimary" : "ButtonSecondary");
        BtnCanvasZoom100.Style = (Style)FindResource(zoom == 1.0 ? "ButtonPrimary" : "ButtonSecondary");
        BtnCanvasZoom200.Style = (Style)FindResource(zoom == 2.0 ? "ButtonPrimary" : "ButtonSecondary");
        BtnCanvasZoom400.Style = (Style)FindResource(zoom == 4.0 ? "ButtonPrimary" : "ButtonSecondary");

        // Update canvas size
        var baseSize = 256;
        var scaledSize = baseSize * _canvasZoom;
        CompositionCanvas.Width = scaledSize;
        CompositionCanvas.Height = scaledSize;

        RenderCanvas();
    }

    private int GetTileDisplaySize()
    {
        return (int)(8 * _tilesetZoom);
    }

    private int GetCanvasDisplaySize()
    {
        return (int)(256 * _canvasZoom);
    }

    private int GetCanvasZoom()
    {
        return (int)_canvasZoom;
    }

    private void RefreshTilesetPreview()
    {
        RefreshTilesetWithPalette();
    }
}
