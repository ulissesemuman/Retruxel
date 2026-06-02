using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.PaletteHelpers;
using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Lib.TilesetHelpers;
using SkiaSharp;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    // ── Tileset state ──────────────────────────────────────────────────────
    private int _activePaletteSlot = 1;   // sprites default to slot 1

    // Selection overlay (same pattern as TilemapEditor)
    private readonly Rectangle _tilesetSelectionRect = new()
    {
        Stroke            = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
        StrokeThickness   = 1,
        Fill              = new SolidColorBrush(Color.FromArgb(40, 0x8E, 0xFF, 0x71)),
        IsHitTestVisible  = false
    };

    private const int TilesetColumns = 16;

    // ── Refresh (called on asset change or palette change) ─────────────────

    /// <summary>
    /// Rebuilds the tileset bitmap from the current asset's MapIndex using the
    /// active palette slot — identical pipeline to TilemapEditor.RefreshTilesetFromAsset().
    /// </summary>
    private void RefreshTilesetFromAsset()
    {
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        IReadOnlyList<HardwareColor> colors;

        if (_currentScene != null && _currentScene.PaletteSlots.Count > 0)
        {
            if (_activePaletteSlot >= _currentScene.PaletteSlots.Count)
                _activePaletteSlot = 0;

            var slot = _currentScene.PaletteSlots[_activePaletteSlot];
            colors = PaletteHelpers.ResolvePaletteColors(slot, _target!);
        }
        else
        {
            // No scene — fall back to raw hardware palette so tiles are still visible
            colors = _target!.GetHardwarePalette();
        }

        var gp = _currentAsset.GenerationParams;
        var skBitmap = IndexedBitmapRenderer.Render(
            gp.MapIndex, colors, gp.OptimizedWidth, gp.OptimizedHeight);

        _tilesetRenderer.LoadFromBitmap(skBitmap, _target!.Specs.TileWidth);
        RebuildTilesetBitmap();
        RenderTilesetCanvas();
        UpdateTilesetSelectionOverlay();
        UpdateVramInfo();
    }

    // ── Bitmap rebuild (same as TilemapEditor.RebuildTilesetBitmap) ────────

    private void RebuildTilesetBitmap()
    {
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        // The asset MapIndex already encodes the correct tile layout (OptimizedWidth x OptimizedHeight).
        // We use it directly as the tileset bitmap — no redistribution needed.
        // _tilesetRenderer was loaded from this same bitmap in RefreshTilesetFromAsset().
        if (_tilesetRenderer.Image == null) return;

        // Scale the existing renderer image to the current zoom level.
        int tileSize    = _target!.Specs.TileWidth;
        var gp          = _currentAsset.GenerationParams;
        int srcW        = gp.OptimizedWidth;
        int srcH        = gp.OptimizedHeight;

        _tilesetBitmap = new SKBitmap(
            (int)(srcW * _tileZoomLevel),
            (int)(srcH * _tileZoomLevel),
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        using var canvas = new SKCanvas(_tilesetBitmap);
        canvas.Clear(SKColors.Transparent);

        var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.None  // nearest-neighbor for pixel art
        };
        canvas.DrawBitmap(
            _tilesetRenderer.Image,
            SKRect.Create(0, 0, srcW, srcH),
            SKRect.Create(0, 0, _tilesetBitmap.Width, _tilesetBitmap.Height),
            paint);
    }

    // ── Canvas render (same as TilemapEditor.RenderTilesetCanvas) ──────────

    private void RenderTilesetCanvas()
    {
        if (_tilesetBitmap == null) return;

        TilesetCanvas.Children.Clear();

        double scaledW = _tilesetBitmap.Width  * _tileZoomLevel;
        double scaledH = _tilesetBitmap.Height * _tileZoomLevel;

        TilesetCanvas.Width  = scaledW;
        TilesetCanvas.Height = scaledH;

        var img = new System.Windows.Controls.Image
        {
            Width            = scaledW,
            Height           = scaledH,
            Source           = ImageProcessing.ConvertSkBitmapToBitmapSource(_tilesetBitmap),
            Stretch          = Stretch.Fill,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        TilesetCanvas.Children.Add(img);

        DrawTilesetGrid();
        TilesetCanvas.Children.Add(_tilesetSelectionRect);
    }

    private void DrawTilesetGrid()
    {
        if (_tilesetBitmap == null || _currentAsset?.GenerationParams == null) return;

        int tileSize  = _target!.Specs.TileWidth;
        double scaled = tileSize * _tileZoomLevel;

        // Columns and rows derived from the asset dimensions — not the fixed TilesetColumns constant.
        int cols = _currentAsset.GenerationParams.OptimizedWidth  / tileSize;
        int rows = _currentAsset.GenerationParams.OptimizedHeight / tileSize;

        var brush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
        brush.Freeze();

        for (int x = 0; x <= cols; x++)
            TilesetCanvas.Children.Add(new Line
            {
                X1 = x * scaled, Y1 = 0,
                X2 = x * scaled, Y2 = rows * scaled,
                Stroke = brush, StrokeThickness = 1, IsHitTestVisible = false
            });

        for (int y = 0; y <= rows; y++)
            TilesetCanvas.Children.Add(new Line
            {
                X1 = 0,         Y1 = y * scaled,
                X2 = cols * scaled, Y2 = y * scaled,
                Stroke = brush, StrokeThickness = 1, IsHitTestVisible = false
            });
    }

    // ── Selection overlay ──────────────────────────────────────────────────

    private void UpdateTilesetSelectionOverlay()
    {
        if (_selectedTileIndex < 0 || _tilesetBitmap == null)
        {
            _tilesetSelectionRect.Visibility = Visibility.Collapsed;
            return;
        }

        int tileSize  = _target!.Specs.TileWidth;
        double scaled = tileSize * _tileZoomLevel;

        int cols = _currentAsset?.GenerationParams != null
            ? _currentAsset.GenerationParams.OptimizedWidth / (_target?.Specs.TileWidth ?? 8)
            : TilesetColumns;
        int col = _selectedTileIndex % cols;
        int row = _selectedTileIndex / cols;

        _tilesetSelectionRect.Width      = scaled;
        _tilesetSelectionRect.Height     = scaled;
        _tilesetSelectionRect.Visibility = Visibility.Visible;

        Canvas.SetLeft(_tilesetSelectionRect, col * scaled);
        Canvas.SetTop (_tilesetSelectionRect, row * scaled);
        Canvas.SetZIndex(_tilesetSelectionRect, 100);
    }

    // ── Mouse events on TilesetCanvas ─────────────────────────────────────

    private void TilesetCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        int tileId = HitTestTile(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        _selectedTileIndex = tileId;
        UpdateTilesetSelectionOverlay();
    }

    private int HitTestTile(System.Windows.Point pos)
    {
        if (_tilesetBitmap == null || _currentAsset?.GenerationParams == null) return -1;

        int tileSize  = _target!.Specs.TileWidth;
        double scaled = tileSize * _tileZoomLevel;

        int col  = (int)(pos.X / scaled);
        int row  = (int)(pos.Y / scaled);
        int cols = _currentAsset.GenerationParams.OptimizedWidth / tileSize;

        if (col < 0 || col >= cols) return -1;

        int totalTiles = _currentAsset.GenerationParams.TileCount;
        int tileId     = row * cols + col;
        return tileId < totalTiles ? tileId : -1;
    }

    private void TilesetCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        int tileId = HitTestTile(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        // Update selection while dragging within tileset
        if (tileId != _selectedTileIndex)
        {
            _selectedTileIndex = tileId;
            UpdateTilesetSelectionOverlay();
        }

        // Start drag-and-drop to composition canvas
        var dragData = new DataObject("TileIndex", tileId);
        DragDrop.DoDragDrop(TilesetCanvas, dragData, DragDropEffects.Copy);
    }

    private void TilesetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

    // ── VRAM info ──────────────────────────────────────────────────────────

    private void UpdateVramInfo()
    {
        if (_currentAsset?.GenerationParams == null)
        {
            TxtVramInfo.Text = "";
            return;
        }

        var gp        = _currentAsset.GenerationParams;
        int tileCount = gp.TileCount;
        int vramBytes = tileCount * (_target?.Specs.Planes[0].BytesPerTile ?? 32);
        int startTile = _state.StartTile;
        int endTile   = startTile + tileCount - 1;

        TxtVramInfo.Text = $"Tiles: {tileCount} | VRAM: {vramBytes}B | Slots: {startTile}–{endTile}";
    }
}
