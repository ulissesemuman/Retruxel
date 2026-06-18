using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.PaletteHelpers;
using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private AssetEntry? _currentAsset;

    private void LoadAssets()
    {
        CmbTilesetAsset.Items.Clear();

        if (_project.Assets.Count == 0)
        {
            TxtVramRegionInfo.Text = "No assets found. Click IMPORT ASSET to add one.";
            return;
        }

        foreach (var asset in _project.Assets)
            CmbTilesetAsset.Items.Add(asset.Id);

        TxtTilesetInfo.Text = "Select a tileset asset to begin";
        TxtVramRegionInfo.Text = "No asset selected";
    }

    private void LoadTilesetImage(AssetEntry asset)
    {
        try
        {
            _currentAsset = asset;

            int tileSize = _target.Specs.TileWidth;
            int calculatedColumns = asset.GenerationParams.OptimizedWidth / tileSize;
            TxtImportColumns.Text = calculatedColumns.ToString();

            if (asset.GenerationParams.MapIndex == null || asset.GenerationParams.MapIndex.Length == 0)
            {
                MessageBox.Show(
                    $"Asset '{asset.Id}' has no MapIndex. Re-import the asset to generate it.",
                    "Missing MapIndex", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RefreshTilesetFromAsset();
            RenderCanvas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tileset: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null) return;

        string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);

        if (asset == null) return;

        if (_tilesetRenderer.Image != null && !_isInitializing)
        {
            int oldTileCount = _tilesetRenderer.TotalTiles;
            int newTileCount = asset.GenerationParams.TileCount;

            if (newTileCount < oldTileCount)
            {
                var currentLayer = _planeData.GetLayer(_currentLayerIndex);
                // Check tile indices
                int tilesAboveLimit = currentLayer.Count(entry =>
                    !entry.IsEmpty && entry.TileIndex >= newTileCount);

                if (tilesAboveLimit > 0)
                {
                    var result = MessageBox.Show(
                        $"Warning: The new tileset '{asset.Id}' has only {newTileCount} tiles.\n\n" +
                        $"Your current plane uses {tilesAboveLimit} tile(s) with indices above {newTileCount - 1}.\n" +
                        $"These tiles will appear as black placeholders and will be lost if you save.\n\n" +
                        $"Do you want to continue?",
                        "Tileset Size Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        TxtTilesetInfo.Text = $"{asset.FileName} ({asset.GenerationParams.TileCount} tiles)";

        var tileCount = asset.GenerationParams?.TileCount ?? 0;
        var bytesPerTile = _target.Specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32;
        TxtVramRegionInfo.Text = $"{tileCount} tiles · {tileCount * bytesPerTile} bytes";

        LoadTilesetImage(asset);
    }

    private void TilesetScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        => HandleTilesetMouseWheel(e);

    private void RefreshTilesetFromAsset()
    {
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        IReadOnlyList<HardwareColor> colors;

        if (_currentScene != null && _currentScene.PaletteSlots.Count > 0)
        {
            if (_selectedPaletteSlot >= _currentScene.PaletteSlots.Count)
                _selectedPaletteSlot = 0;

            var slot = _currentScene.PaletteSlots[_selectedPaletteSlot];
            colors = PaletteHelpers.ResolvePaletteColors(slot, _target);
        }
        else
        {
            // No active scene — fall back to raw hardware palette so the
            // tileset is still visible (e.g. when opening the editor standalone).
            colors = _target.GetHardwarePalette();
        }

        int tileSize = _target.Specs.TileWidth;
        int rows = (int)Math.Ceiling(_currentAsset.GenerationParams.TileCount / 16.0);
        int bitmapW = 16 * tileSize;
        int bitmapH = rows * tileSize;

        var skBitmap = IndexedBitmapRenderer.Render(
            _currentAsset.GenerationParams.MapIndex, colors, _currentAsset.GenerationParams.OptimizedWidth, _currentAsset.GenerationParams.OptimizedHeight);

        _tilesetRenderer.LoadFromBitmap(skBitmap, tileSize);
        RebuildTilesetBitmap();
        RenderTilesetCanvas();
        UpdateTileselectionOverlay();
    }

    private IReadOnlyList<HardwareColor> ResolvePaletteColors(PaletteSlotData slot)
    {
        var hardwarePalette = _target.GetHardwarePalette();
        var result = new List<HardwareColor>();

        foreach (var hexColor in slot.Colors)
        {
            var hw = FindClosestHardwareColor(hexColor, hardwarePalette);
            result.Add(hw);
        }

        if (result.Count == 0)
            result.Add(new HardwareColor(0, 0, 0));

        return result;
    }

    private static HardwareColor FindClosestHardwareColor(
        string hexColor,
        IReadOnlyList<HardwareColor> palette)
    {
        if (string.IsNullOrEmpty(hexColor) || hexColor.Length < 7 || hexColor[0] != '#')
            return palette.Count > 0 ? palette[0] : new HardwareColor(0, 0, 0);

        int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

        HardwareColor best = palette[0];
        int bestDist = int.MaxValue;

        foreach (var hw in palette)
        {
            int dist = Math.Abs(hw.R - r) + Math.Abs(hw.G - g) + Math.Abs(hw.B - b);
            if (dist < bestDist) { bestDist = dist; best = hw; }
            if (dist == 0) break;
        }

        return best;
    }

    private const int TilesetColumns = 16;

    internal int GetTilesetColumns() => TilesetColumns;
    private SKBitmap? _tilesetBitmap;
    private Image? _tilesetImage;

    private readonly Rectangle _tilesetSelectionRect = new()
    {
        Stroke = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
        StrokeThickness = 1,
        Fill = new SolidColorBrush(Color.FromArgb(40, 0x8E, 0xFF, 0x71)),
        IsHitTestVisible = false
    };

    private bool _tilesetGridVisible = true;
    private Color _tilesetGridColor = Color.FromArgb(60, 255, 255, 255);
    private bool _isTilesetSelecting;
    private Point _tilesetSelectStartTile;

    /// <summary>
    /// Rebuilds the tileset bitmap by extracting each tile from the TilesetRenderer and drawing them onto a new SKBitmap in a grid layout.
    /// </summary>
    private void RebuildTilesetBitmap()
    {
        if (_tilesetRenderer.Image == null) return;

        int totalTiles = _currentAsset?.GenerationParams.TileCount ?? _tilesetRenderer.TotalTiles;
        int tileSize = _target.Specs.TileWidth;

        int rows = (int)Math.Ceiling(totalTiles / (double)TilesetColumns);
        int bitmapWidth = TilesetColumns * tileSize;
        int bitmapHeight = rows * tileSize;

        _tilesetBitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        // Creates a new bitmap and draws each tile onto it in the correct position
        using (var canvas = new SKCanvas(_tilesetBitmap))
        {
            canvas.Clear(SKColors.Transparent);

            for (int tileId = 0; tileId < totalTiles; tileId++)
            {
                int col = tileId % TilesetColumns;
                int row = tileId / TilesetColumns;
                using var tileImage = _tilesetRenderer.ExtractSkTile(tileId);
                if (tileImage != null)
                    canvas.DrawBitmap(tileImage, col * tileSize, row * tileSize);
            }
        }
    }

    private void RenderTilesetCanvas()
    {
        if (_tilesetBitmap == null) return;

        TilesetCanvas.Children.Clear();

        int tileSize = _target.Specs.TileWidth;
        double scaledTile = tileSize * _tileZoomLevel;

        double canvasW = _tilesetBitmap.Width * _tileZoomLevel;
        double canvasH = _tilesetBitmap.Height * _tileZoomLevel;

        TilesetCanvas.Width = canvasW;
        TilesetCanvas.Height = canvasH;

        _tilesetImage = new Image
        {
            Width = canvasW,
            Height = canvasH,
            Source = ImageProcessing.ConvertSkBitmapToBitmapSource(_tilesetBitmap),
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(_tilesetImage, BitmapScalingMode.NearestNeighbor);
        TilesetCanvas.Children.Add(_tilesetImage);

        if (_tilesetGridVisible)
            DrawTilesetGrid(scaledTile);

        TilesetCanvas.Children.Add(_tilesetSelectionRect);
    }

    private void DrawTilesetGrid(double scaledTile)
    {
        if (_tilesetBitmap == null) return;

        int cols = TilesetColumns;
        int rows = _tilesetBitmap.Height / _target.Specs.TileWidth;
        var brush = new SolidColorBrush(_tilesetGridColor);
        brush.Freeze();

        for (int x = 0; x <= cols; x++)
        {
            TilesetCanvas.Children.Add(new Line
            {
                X1 = x * scaledTile,
                Y1 = 0,
                X2 = x * scaledTile,
                Y2 = rows * scaledTile,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            });
        }

        for (int y = 0; y <= rows; y++)
        {
            TilesetCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = y * scaledTile,
                X2 = cols * scaledTile,
                Y2 = y * scaledTile,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            });
        }
    }

    private void UpdateTileselectionOverlay()
    {
        if (_selectedTileIds.Count == 0 || _tilesetBitmap == null)
        {
            _tilesetSelectionRect.Visibility = Visibility.Collapsed;
            return;
        }

        int tileSize = _target.Specs.TileWidth;
        double scaledTile = tileSize * _tileZoomLevel;

        int minCol = _selectedTileIds.Min(id => id % TilesetColumns);
        int minRow = _selectedTileIds.Min(id => id / TilesetColumns);

        _tilesetSelectionRect.Width = _selectionWidth * scaledTile;
        _tilesetSelectionRect.Height = _selectionHeight * scaledTile;
        _tilesetSelectionRect.Visibility = Visibility.Visible;

        Canvas.SetLeft(_tilesetSelectionRect, minCol * scaledTile);
        Canvas.SetTop(_tilesetSelectionRect, minRow * scaledTile);
        Canvas.SetZIndex(_tilesetSelectionRect, 100);
    }

    private void TilesetCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleTilesetPanStart(e);
            return;
        }

        int tileId = HitTestTile(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        // Collision mode: click toggles tile as solid instead of selecting it
        if (TryHandleCollisionTilesetClick(tileId)) return;

        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _isTilesetSelecting = true;
            _tilesetSelectStartTile = GetTilePosition(tileId);
            _selectedTileIds.Clear();
            _selectedTileIds.Add(tileId);
            _selectionWidth = _selectionHeight = 1;
            _selectedFlipH = _selectedFlipV = false;
        }
        else
        {
            _isTilesetSelecting = false;
            _selectedTileId = tileId;
            _selectedTileIds.Clear();
            _selectedTileIds.Add(tileId);
            _selectionWidth = _selectionHeight = 1;
        }

        TilesetCanvas.CaptureMouse();
        UpdateTileselectionOverlay();
        UpdateSelectedTilePreview();
    }

    private void TilesetCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleTilesetPanMove(e);
            return;
        }

        if (!_isTilesetSelecting || e.LeftButton != MouseButtonState.Pressed) return;

        int tileId = HitTestTile(e.GetPosition(TilesetCanvas));
        if (tileId < 0) return;

        UpdateRectangularSelection(_tilesetSelectStartTile, GetTilePosition(tileId));
    }

    private void TilesetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentToolMode == ToolMode.Navigate)
        {
            HandleTilesetPanEnd();
            return;
        }

        _isTilesetSelecting = false;
        TilesetCanvas.ReleaseMouseCapture();
    }

    private void TilesetCanvas_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        => HandleTilesetPanStart(e);

    private void TilesetCanvas_MouseMiddleButtonUp(object sender, MouseButtonEventArgs e)
        => HandleTilesetPanEnd();

    private void TilesetCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        => HandleTilesetMouseWheel(e);

    private int HitTestTile(Point canvasPos)
    {
        if (_tilesetBitmap == null) return -1;

        int tileSize = _target.Specs.TileWidth;
        double scaledTile = tileSize * _tileZoomLevel;

        int col = (int)(canvasPos.X / scaledTile);
        int row = (int)(canvasPos.Y / scaledTile);

        if (col < 0 || col >= TilesetColumns) return -1;

        string? assetId = CmbTilesetAsset.SelectedItem?.ToString();
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
        int totalTiles = asset?.GenerationParams.TileCount ?? _tilesetRenderer.TotalTiles;

        int tileId = row * TilesetColumns + col;
        return tileId < totalTiles ? tileId : -1;
    }

    private void ToggleTilesetGrid()
    {
        _tilesetGridVisible = !_tilesetGridVisible;
        RenderTilesetCanvas();
    }

    private void SetTilesetGridColor(Color color)
    {
        _tilesetGridColor = color;
        RenderTilesetCanvas();
    }
}
