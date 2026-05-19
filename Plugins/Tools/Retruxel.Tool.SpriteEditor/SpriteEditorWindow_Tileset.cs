using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private int _tilesetColumns;
    private int _tilesetRows;
    private int _totalTiles;
    private double _tileZoomLevel = 2.0;
    private string? _currentAssetId;
    private int _activePaletteSlot = 1;

    /// <summary>
    /// Renders the tileset using the active palette slot from the current scene.
    /// Called whenever the palette selection changes or the asset changes.
    /// </summary>
    private void RefreshTilesetWithPalette()
    {
        if (_indexedData is null || _currentScene is null) return;

        if (_activePaletteSlot >= _currentScene.PaletteSlots.Count)
            _activePaletteSlot = 0;

        var slot = _currentScene.PaletteSlots[_activePaletteSlot];
        _tilesetImage = _indexedPngService.RenderPreview(_indexedData, slot.Colors, scale: (int)_tileZoomLevel);

        RenderTileset();
    }

    private void UpdateVramInfo()
    {
        if (_indexedData is null)
        {
            TxtVramInfo.Text = "";
            return;
        }

        var tileCount = _totalTiles;
        var vramBytes = tileCount * 32;
        var startTile = _state.StartTile;
        var endTile = startTile + tileCount - 1;

        TxtVramInfo.Text = $"Tiles: {tileCount} | VRAM: {vramBytes}B | Slots: {startTile}–{endTile}";
    }

    private void RenderTileset()
    {
        if (_tilesetImage == null)
        {
            TilesetItemsControl.Items.Clear();
            return;
        }

        TilesetItemsControl.Items.Clear();

        int tileSize = (int)(8 * _tileZoomLevel);

        for (int i = 0; i < _totalTiles; i++)
        {
            var tileButton = CreateTileButton(i, tileSize);
            TilesetItemsControl.Items.Add(tileButton);
        }
    }

    private Border CreateTileButton(int tileIndex, int tileSize)
    {
        var tileImage = ExtractTile(tileIndex);

        var image = new Image
        {
            Source = ImageProcessing.ConvertSkBitmapToBitmapSource(tileImage),
            Width = tileSize,
            Height = tileSize,
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        var border = new Border
        {
            Width = tileSize,
            Height = tileSize,
            Margin = new Thickness(2),
            Background = (Brush)FindResource("BrushSurfaceContainerLow"),
            Child = image,
            Cursor = Cursors.Hand,
            Tag = tileIndex
        };

        border.MouseLeftButtonDown += TileButton_Click;
        border.MouseMove += TileButton_MouseMove;

        return border;
    }

    private SKBitmap ExtractTile(int tileIndex)
    {
        if (_tilesetImage == null)
            return new SKBitmap(8, 8, SKColorType.Bgra8888, SKAlphaType.Premul);

        int col = tileIndex % _tilesetColumns;
        int row = tileIndex / _tilesetColumns;
        int tileSize = (int)(8 * _tileZoomLevel);

        var tile = new SKBitmap(tileSize, tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(tile))
        {
            var sourceRect = SKRect.Create(col * tileSize, row * tileSize, tileSize, tileSize);
            var destRect = SKRect.Create(0, 0, tileSize, tileSize);

            canvas.DrawBitmap(_tilesetImage, sourceRect, destRect);
        }

        return tile;
    }

    private void TileButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int tileIndex)
        {
            _selectedTileIndex = tileIndex;
            UpdateTilesetSelection();
        }
    }

    private void TileButton_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is Border border && border.Tag is int tileIndex)
        {
            var dragData = new DataObject("TileIndex", tileIndex);
            DragDrop.DoDragDrop(border, dragData, DragDropEffects.Copy);
        }
    }

    private void UpdateTilesetSelection()
    {
        foreach (Border item in TilesetItemsControl.Items)
        {
            if (item.Tag is int tileIndex && tileIndex == _selectedTileIndex)
            {
                item.BorderBrush = (Brush)FindResource("BrushPrimary");
                item.BorderThickness = new Thickness(2);
            }
            else
            {
                item.BorderBrush = Brushes.Transparent;
                item.BorderThickness = new Thickness(0);
            }
        }
    }
}
