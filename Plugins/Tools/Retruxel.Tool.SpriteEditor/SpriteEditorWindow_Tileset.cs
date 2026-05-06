using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System;
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

    private void RefreshTilesetWithPalette()
    {
        if (_indexedData is null || _currentScene is null) return;

        var slot = _currentScene.PaletteSlots[_activePaletteSlot];
        var skBitmap = _indexedPngService.RenderPreview(_indexedData, slot.Colors, scale: (int)_tileZoomLevel);
        _tilesetImage = ConvertSkBitmapToBitmapSource(skBitmap);

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
            Source = tileImage,
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

    private BitmapSource ExtractTile(int tileIndex)
    {
        if (_tilesetImage == null)
            return BitmapSource.Create(8, 8, 96, 96, PixelFormats.Bgra32, null, new byte[8 * 8 * 4], 8 * 4);

        int col = tileIndex % _tilesetColumns;
        int row = tileIndex / _tilesetColumns;
        int tileSize = (int)(8 * _tileZoomLevel);

        var croppedBitmap = new CroppedBitmap(_tilesetImage, new Int32Rect(col * tileSize, row * tileSize, tileSize, tileSize));

        return croppedBitmap;
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

    private BitmapSource ConvertSkBitmapToBitmapSource(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var memoryStream = new MemoryStream();
        data.SaveTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }
}
