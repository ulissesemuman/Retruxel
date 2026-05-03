using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Retruxel.Tool.SpriteEditor.Models;

namespace Retruxel.Tool.SpriteEditor.Helpers;

public static class SpriteRenderer
{
    public static BitmapSource RenderFrame(SpriteFrame frame, BitmapSource tilesetImage, int tilesetColumns, int scale = 1)
    {
        if (frame.Tiles.Count == 0)
            return CreateEmptyBitmap(8, 8);

        int minX = frame.Tiles.Min(t => t.OffsetX);
        int minY = frame.Tiles.Min(t => t.OffsetY);
        int maxX = frame.Tiles.Max(t => t.OffsetX + 8);
        int maxY = frame.Tiles.Max(t => t.OffsetY + 8);

        int width = (maxX - minX) * scale;
        int height = (maxY - minY) * scale;

        var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            foreach (var tile in frame.Tiles)
            {
                var tileImage = ExtractTile(tilesetImage, tile.TileIndex, tilesetColumns);
                var rect = new Rect((tile.OffsetX - minX) * scale, (tile.OffsetY - minY) * scale, 8 * scale, 8 * scale);
                context.DrawImage(tileImage, rect);
            }
        }

        renderTarget.Render(visual);
        return renderTarget;
    }

    private static BitmapSource ExtractTile(BitmapSource tilesetImage, int tileIndex, int tilesetColumns)
    {
        int col = tileIndex % tilesetColumns;
        int row = tileIndex / tilesetColumns;

        var croppedBitmap = new CroppedBitmap(tilesetImage, new Int32Rect(col * 8, row * 8, 8, 8));
        
        var renderTarget = new RenderTargetBitmap(8, 8, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(croppedBitmap, new Rect(0, 0, 8, 8));
        }
        
        renderTarget.Render(visual);
        return renderTarget;
    }

    private static BitmapSource CreateEmptyBitmap(int width, int height)
    {
        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, new byte[width * height * 4], width * 4);
    }
}
