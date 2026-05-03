using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private BitmapSource? _tilesetImage;
    private int _tilesetColumns;
    private int _tilesetRows;
    private int _totalTiles;

    public void LoadTileset(string imagePath)
    {
        if (!System.IO.File.Exists(imagePath))
            return;

        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        _tilesetImage = bitmap;
        _tilesetColumns = bitmap.PixelWidth / 8;
        _tilesetRows = bitmap.PixelHeight / 8;
        _totalTiles = _tilesetColumns * _tilesetRows;

        RenderTileset();
    }

    private void RenderTileset()
    {
        if (_tilesetImage == null)
            return;

        TilesetItemsControl.Items.Clear();

        for (int i = 0; i < _totalTiles; i++)
        {
            var tileButton = CreateTileButton(i);
            TilesetItemsControl.Items.Add(tileButton);
        }
    }

    private Border CreateTileButton(int tileIndex)
    {
        var tileImage = ExtractTile(tileIndex);
        
        var image = new Image
        {
            Source = tileImage,
            Width = 24,
            Height = 24,
            Stretch = Stretch.None
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        var border = new Border
        {
            Width = 24,
            Height = 24,
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

        var croppedBitmap = new CroppedBitmap(_tilesetImage, new Int32Rect(col * 8, row * 8, 8, 8));
        
        var renderTarget = new RenderTargetBitmap(8, 8, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(croppedBitmap, new Rect(0, 0, 8, 8));
        }
        renderTarget.Render(visual);
        
        return renderTarget;
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
