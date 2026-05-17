using Retruxel.Tool.SpriteEditor.Models;
using SkiaSharp;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.SpriteEditor.Helpers;

public static class SpriteRenderer
{
    public static SKBitmap RenderFrame(SpriteFrame frame, SKBitmap tilesetImage, int tilesetColumns, int scale = 1)
    {
        if (frame.Tiles.Count == 0)
            return CreateEmptyBitmap(8, 8);

        int minX = frame.Tiles.Min(t => t.OffsetX);
        int minY = frame.Tiles.Min(t => t.OffsetY);
        int maxX = frame.Tiles.Max(t => t.OffsetX + 8);
        int maxY = frame.Tiles.Max(t => t.OffsetY + 8);

        int width = (maxX - minX) * scale;
        int height = (maxY - minY) * scale;

        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            foreach (var tile in frame.Tiles)
            {
                var tileImage = ExtractTile(tilesetImage, tile.TileIndex, tilesetColumns);

                var destRect = SKRect.Create(
                    (tile.OffsetX - minX) * scale,
                    (tile.OffsetY - minY) * scale,
                    8 * scale,
                    8 * scale);

                canvas.DrawBitmap(tileImage, destRect);
            }
        }

        return bitmap;
    }

    private static SKBitmap ExtractTile(SKBitmap tilesetImage, int tileIndex, int tilesetColumns)
    {
        int col = tileIndex % tilesetColumns;
        int row = tileIndex / tilesetColumns;

        var tile = new SKBitmap(8, 8, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(tile))
        {
            var sourceRect = SKRect.Create(col * 8, row * 8, 8, 8);
            var destRect = SKRect.Create(0, 0, 8, 8);

            canvas.DrawBitmap(tilesetImage, sourceRect, destRect);
        }

        return tile;
    }

    private static SKBitmap CreateEmptyBitmap(int width, int height)
    {
        return new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }
}
