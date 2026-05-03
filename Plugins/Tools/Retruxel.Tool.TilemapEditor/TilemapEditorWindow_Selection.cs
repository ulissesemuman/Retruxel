using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Handles rectangular selection in tileset and tilemap, plus paint preview.
/// </summary>
public partial class TilemapEditorWindow
{
    // Tileset selection
    private const int TILESET_COLUMNS = 16; // Fixed for ROM alignment
    private Point? _tileSelectionStart = null;
    private List<int> _selectedTileIds = new List<int> { 0 };
    private int _selectionWidth = 1;
    private int _selectionHeight = 1;

    // Canvas paint preview
    private Rectangle? _paintPreviewRect = null;
    private Canvas? _paintPreviewCanvas = null;

    /// <summary>
    /// Initializes selection system.
    /// </summary>
    private void InitializeSelection()
    {
        _selectedTileIds.Clear();
        _selectedTileIds.Add(0);
        _selectionWidth = 1;
        _selectionHeight = 1;

        // Create paint preview canvas overlay
        _paintPreviewCanvas = new Canvas
        {
            Background = Brushes.Transparent,
            IsHitTestVisible = false
        };
        
        // Add to tilemap canvas (will be positioned in RenderCanvas)
        if (TilemapCanvas.Parent is ScrollViewer sv)
        {
            // Canvas is inside ScrollViewer, we need to overlay it
            // This will be handled in Canvas_MouseMove
        }
    }

    /// <summary>
    /// Handles mouse down on tileset tile - starts rectangular selection.
    /// </summary>
    private void TileButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        int tileId = (int)border.Tag;

        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            // Start rectangular selection
            _tileSelectionStart = GetTilePosition(tileId);
            _selectedTileIds.Clear();
            _selectedTileIds.Add(tileId);
            _selectionWidth = 1;
            _selectionHeight = 1;
        }
        else
        {
            // Single tile selection
            _selectedTileId = tileId;
            _selectedTileIds.Clear();
            _selectedTileIds.Add(tileId);
            _selectionWidth = 1;
            _selectionHeight = 1;
            _tileSelectionStart = null;
        }

        UpdateTileSelectionVisual();
        UpdateSelectedTilePreview();
    }

    /// <summary>
    /// Handles mouse move on tileset - updates rectangular selection.
    /// </summary>
    private void TileButton_MouseMove(object sender, MouseEventArgs e)
    {
        if (_tileSelectionStart == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (sender is not Border border) return;
        int tileId = (int)border.Tag;

        Point endPos = GetTilePosition(tileId);
        UpdateRectangularSelection(_tileSelectionStart.Value, endPos);
    }

    /// <summary>
    /// Handles mouse up on tileset - finalizes selection.
    /// </summary>
    private void TileButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _tileSelectionStart = null;
    }

    /// <summary>
    /// Converts tile ID to grid position (x, y).
    /// </summary>
    private Point GetTilePosition(int tileId)
    {
        int x = tileId % TILESET_COLUMNS;
        int y = tileId / TILESET_COLUMNS;
        return new Point(x, y);
    }

    /// <summary>
    /// Updates rectangular selection from start to end position.
    /// </summary>
    private void UpdateRectangularSelection(Point start, Point end)
    {
        int minX = (int)Math.Min(start.X, end.X);
        int maxX = (int)Math.Max(start.X, end.X);
        int minY = (int)Math.Min(start.Y, end.Y);
        int maxY = (int)Math.Max(start.Y, end.Y);

        _selectionWidth = maxX - minX + 1;
        _selectionHeight = maxY - minY + 1;
        _selectedTileIds.Clear();

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int tileId = y * TILESET_COLUMNS + x;
                
                // Get actual tile count from asset
                string? assetId = CmbTilesetAsset.SelectedItem?.ToString();
                var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
                int maxTiles = asset?.TileCount ?? _tilesetRenderer.TotalTiles;
                
                if (tileId < maxTiles)
                    _selectedTileIds.Add(tileId);
            }
        }

        UpdateTileSelectionVisual();
        UpdateSelectedTilePreview();
    }

    /// <summary>
    /// Updates visual feedback for selected tiles in tileset.
    /// </summary>
    private void UpdateTileSelectionVisual()
    {
        foreach (var item in TilesetGrid.Items)
        {
            if (item is Border border)
            {
                int tileId = (int)border.Tag;
                if (_selectedTileIds.Contains(tileId))
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
                    border.BorderThickness = new Thickness(2);
                }
                else
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                    border.BorderThickness = new Thickness(1);
                }
            }
        }
    }

    /// <summary>
    /// Updates the selected tile preview panel with current selection.
    /// </summary>
    private void UpdateSelectedTilePreview()
    {
        if (_tilesetRenderer.Image == null || _selectedTileIds.Count == 0)
            return;

        if (_selectedTileIds.Count == 1)
        {
            // Single tile
            var tileImage = _tilesetRenderer.ExtractTile(_selectedTileIds[0]);
            if (tileImage != null)
            {
                ImgSelectedTile.Source = tileImage;
                ImgSelectedTile.Stretch = Stretch.Fill;
                TxtSelectedTileInfo.Text = $"Tile ID: {_selectedTileIds[0]}";
            }
        }
        else
        {
            // Block selection
            var blockImage = RenderTileBlock(_selectedTileIds, _selectionWidth, _selectionHeight);
            ImgSelectedTile.Source = blockImage;
            ImgSelectedTile.Stretch = Stretch.Uniform;
            TxtSelectedTileInfo.Text = $"Block: {_selectionWidth}×{_selectionHeight} tiles ({_selectedTileIds.Count} total)";
        }
    }

    /// <summary>
    /// Renders a block of tiles into a single image.
    /// </summary>
    private BitmapSource RenderTileBlock(List<int> tileIds, int width, int height)
    {
        int tileSize = _target.Specs.TileWidth;
        var bitmap = new RenderTargetBitmap(
            width * tileSize,
            height * tileSize,
            96, 96,
            PixelFormats.Pbgra32);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            for (int i = 0; i < tileIds.Count; i++)
            {
                int x = i % width;
                int y = i / width;
                var tileImage = _tilesetRenderer.ExtractTile(tileIds[i]);
                if (tileImage != null)
                    context.DrawImage(tileImage, new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
            }
        }

        bitmap.Render(visual);
        return bitmap;
    }

    /// <summary>
    /// Shows paint preview on canvas when mouse moves.
    /// </summary>
    private void ShowPaintPreview(int tileX, int tileY)
    {
        if (_tilesetRenderer.Image == null)
        {
            HidePaintPreview();
            return;
        }

        int tileSize = _target.Specs.TileWidth;
        double scaledSize = tileSize * _canvasZoom;

        // Remove old preview
        if (_paintPreviewRect != null && TilemapCanvas.Children.Contains(_paintPreviewRect))
            TilemapCanvas.Children.Remove(_paintPreviewRect);

        // Create new preview rectangle
        _paintPreviewRect = new Rectangle
        {
            Width = _selectionWidth * scaledSize,
            Height = _selectionHeight * scaledSize,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 142, 255, 113)), // Semi-transparent green
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(40, 142, 255, 113)), // Very transparent green fill
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_paintPreviewRect, tileX * scaledSize);
        Canvas.SetTop(_paintPreviewRect, tileY * scaledSize);
        Canvas.SetZIndex(_paintPreviewRect, 1000); // Always on top

        TilemapCanvas.Children.Add(_paintPreviewRect);
    }

    /// <summary>
    /// Hides paint preview.
    /// </summary>
    private void HidePaintPreview()
    {
        if (_paintPreviewRect != null && TilemapCanvas.Children.Contains(_paintPreviewRect))
        {
            TilemapCanvas.Children.Remove(_paintPreviewRect);
            _paintPreviewRect = null;
        }
    }

    /// <summary>
    /// Places a block of tiles on the tilemap.
    /// </summary>
    private void PlaceTileBlock(int startX, int startY)
    {
        if (_selectedTileIds.Count == 0) return;

        for (int i = 0; i < _selectedTileIds.Count; i++)
        {
            int localX = i % _selectionWidth;
            int localY = i / _selectionWidth;
            int targetX = startX + localX;
            int targetY = startY + localY;

            if (targetX >= 0 && targetX < _tilemapData.Width &&
                targetY >= 0 && targetY < _tilemapData.Height)
            {
                int index = targetY * _tilemapData.Width + targetX;
                var currentLayer = _tilemapData.GetLayer(_currentLayerIndex);
                if (index >= 0 && index < currentLayer.Length)
                    currentLayer[index] = _selectedTileIds[i];
            }
        }

        RenderCanvas();
    }
}
