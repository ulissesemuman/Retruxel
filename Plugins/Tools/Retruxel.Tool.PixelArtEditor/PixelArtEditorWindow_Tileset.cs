using Retruxel.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow
{
    private const int TilesetColumns = 16;
    private Rectangle? _tilesetSelectionRect;

    private void LoadAssets(string? initialAssetId = null)
    {
        CmbTilesetAsset.Items.Clear();
        if (_project == null) return;

        foreach (var asset in _project.Assets)
            CmbTilesetAsset.Items.Add(asset.Id);

        if (initialAssetId != null)
        {
            int idx = CmbTilesetAsset.Items.IndexOf(initialAssetId);
            if (idx >= 0) { CmbTilesetAsset.SelectedIndex = idx; return; }
        }

        if (CmbTilesetAsset.Items.Count > 0)
            CmbTilesetAsset.SelectedIndex = 0;
    }

    private void LoadAsset(string assetId)
    {
        _currentAsset = _project?.Assets.FirstOrDefault(a => a.Id == assetId);
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        _selectedTileIndex = 0;
        LoadTileIntoEditor(0);
        RenderTilesetCanvas();

        var gp = _currentAsset.GenerationParams;
        TxtVramRegionInfo.Text = $"{gp.TileCount} tiles · {gp.OptimizedWidth}×{gp.OptimizedHeight}px";
        TxtTileName.Text = $"— {assetId}";
    }

    private void RenderTilesetCanvas()
    {
        TilesetCanvas.Children.Clear();
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        var gp    = _currentAsset.GenerationParams;
        int ts    = _tileSize;
        double sc = _tileZoom;
        int cols  = Math.Max(1, gp.OptimizedWidth / ts);
        int rows  = (int)Math.Ceiling((double)gp.TileCount / cols);
        double cw = cols * ts * sc;
        double ch = rows * ts * sc;

        TilesetCanvas.Width  = cw;
        TilesetCanvas.Height = ch;

        var bitmap = RenderTilesetBitmap(gp, ts, cols, rows);
        var img = new Image
        {
            Source = bitmap, Width = cw, Height = ch,
            Stretch = Stretch.Fill, IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(img, 0); Canvas.SetTop(img, 0);
        TilesetCanvas.Children.Add(img);

        // Grid overlay
        var gridBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
        gridBrush.Freeze();
        for (int x = 0; x <= cols; x++)
            TilesetCanvas.Children.Add(new Line { X1 = x*ts*sc, Y1 = 0, X2 = x*ts*sc, Y2 = ch, Stroke = gridBrush, StrokeThickness = 1 });
        for (int y = 0; y <= rows; y++)
            TilesetCanvas.Children.Add(new Line { X1 = 0, Y1 = y*ts*sc, X2 = cw, Y2 = y*ts*sc, Stroke = gridBrush, StrokeThickness = 1 });

        // Selection rect
        int selCol = _selectedTileIndex % cols;
        int selRow = _selectedTileIndex / cols;
        _tilesetSelectionRect = new Rectangle
        {
            Width = ts*sc, Height = ts*sc,
            Stroke = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(40, 0x8E, 0xFF, 0x71)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_tilesetSelectionRect, selCol * ts * sc);
        Canvas.SetTop(_tilesetSelectionRect,  selRow * ts * sc);
        TilesetCanvas.Children.Add(_tilesetSelectionRect);

        UpdateTilePreview();
    }

    private WriteableBitmap RenderTilesetBitmap(AssetGenerationParams gp, int ts, int cols, int rows)
    {
        int bw = cols * ts, bh = rows * ts;
        var wb  = new WriteableBitmap(bw, bh, 96, 96, PixelFormats.Bgra32, null);
        var buf = new int[bw * bh];

        for (int i = 0; i < Math.Min(gp.TileCount, cols * rows); i++)
        {
            int srcCols = Math.Max(1, gp.OptimizedWidth / ts);
            for (int py = 0; py < ts; py++)
            for (int px = 0; px < ts; px++)
            {
                int srcX = (i % srcCols) * ts + px;
                int srcY = (i / srcCols) * ts + py;
                int srcIdx = srcY * gp.OptimizedWidth + srcX;
                if (srcIdx >= gp.MapIndex.Length) continue;
                var col = GetColorAt(gp.MapIndex[srcIdx]);
                buf[((i / cols) * ts + py) * bw + ((i % cols) * ts + px)] =
                    (col.A << 24) | (col.R << 16) | (col.G << 8) | col.B;
            }
        }

        wb.WritePixels(new Int32Rect(0, 0, bw, bh), buf, bw * 4, 0);
        return wb;
    }

    private void HandleTilesetClick(System.Windows.Point pos)
    {
        if (_currentAsset?.GenerationParams == null) return;
        int srcCols = Math.Max(1, _currentAsset.GenerationParams.OptimizedWidth / _tileSize);
        int col = (int)(pos.X / (_tileSize * _tileZoom));
        int row = (int)(pos.Y / (_tileSize * _tileZoom));
        int tileId = row * srcCols + col;
        if (tileId < 0 || tileId >= _currentAsset.GenerationParams.TileCount) return;

        _selectedTileIndex = tileId;
        LoadTileIntoEditor(tileId);
        RenderTilesetCanvas();
        RenderEditorCanvas();
    }

    private void LoadTileIntoEditor(int tileIndex)
    {
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;
        var gp   = _currentAsset.GenerationParams;
        int ts   = _tileSize;
        int cols = Math.Max(1, gp.OptimizedWidth / ts);

        _pixels = new byte[ts, ts];
        for (int py = 0; py < ts; py++)
        for (int px = 0; px < ts; px++)
        {
            int srcX = (tileIndex % cols) * ts + px;
            int srcY = (tileIndex / cols) * ts + py;
            int idx  = srcY * gp.OptimizedWidth + srcX;
            if (idx < gp.MapIndex.Length)
                _pixels[px, py] = gp.MapIndex[idx];
        }
    }

    private void UpdateTilePreview()
    {
        int ts    = _tileSize;
        int scale = 4;
        var wb    = new WriteableBitmap(ts * scale, ts * scale, 96, 96, PixelFormats.Bgra32, null);
        var buf   = new int[ts * scale * ts * scale];

        for (int py = 0; py < ts; py++)
        for (int px = 0; px < ts; px++)
        {
            var col  = GetColorAt(_pixels[px, py]);
            int argb = (col.A << 24) | (col.R << 16) | (col.G << 8) | col.B;
            for (int sy = 0; sy < scale; sy++)
            for (int sx = 0; sx < scale; sx++)
                buf[(py * scale + sy) * ts * scale + (px * scale + sx)] = argb;
        }

        wb.WritePixels(new Int32Rect(0, 0, ts * scale, ts * scale), buf, ts * scale * 4, 0);
        ImgSelectedTile.Source = wb;
        ImgSelectedTile.Width  = ts * scale;
        ImgSelectedTile.Height = ts * scale;
    }
}
