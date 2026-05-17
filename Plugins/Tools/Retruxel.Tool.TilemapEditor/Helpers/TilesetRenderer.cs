using Retruxel.Core.Models;
using SkiaSharp;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles tileset image loading and tile extraction.
/// </summary>
public class TilesetRenderer
{
    private SKBitmap? _tilesetImage;
    private int _columns;
    private int _rows;
    private int _tileSize;

    public SKBitmap? Image => _tilesetImage;
    public int Columns => _columns;
    public int Rows => _rows;
    public int TotalTiles => _columns * _rows;

    public void LoadTileset(string imagePath, int tileSize)
    {
        _tileSize = tileSize;

        using var stream = File.OpenRead(imagePath);
        _tilesetImage = SKBitmap.Decode(stream);

        if (_tilesetImage != null)
        {
            _columns = _tilesetImage.Width / tileSize;
            _rows = _tilesetImage.Height / tileSize;
        }
    }

    public void LoadFromBitmap(SKBitmap bitmap, int tileSize)
    {
        _tileSize = tileSize;
        _tilesetImage = bitmap;
        _columns = bitmap.Width / tileSize;
        _rows = bitmap.Height / tileSize;
    }

    public SKBitmap? ExtractTile(int tileId)
    {
        if (_tilesetImage == null) return null;

        // Out of bounds - return black placeholder
        if (tileId >= TotalTiles)
        {
            var placeholder = new SKBitmap(_tileSize, _tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(placeholder))
            {
                canvas.Clear(SKColors.Black);
            }
            return placeholder;
        }

        int srcX = (tileId % _columns) * _tileSize;
        int srcY = (tileId / _columns) * _tileSize;

        var tile = new SKBitmap(_tileSize, _tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(tile))
        {
            var srcRect = new SKRect(srcX, srcY, srcX + _tileSize, srcY + _tileSize);
            var destRect = new SKRect(0, 0, _tileSize, _tileSize);
            canvas.DrawBitmap(_tilesetImage, srcRect, destRect);
        }

        return tile;
    }

    public SKBitmap? ExtractTile(TileEntry entry)
    {
        var baseTile = ExtractTile(entry.TileIndex);

        if (baseTile == null)
            return null;

        if (!entry.FlipH && !entry.FlipV && entry.Rotation == 0)
            return baseTile;

        return ApplyTransform(baseTile, entry.FlipH, entry.FlipV, entry.Rotation);
    }

    private SKBitmap ApplyTransform(SKBitmap source, bool flipH, bool flipV, int rotation)
    {
        var transformed = new SKBitmap(_tileSize, _tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(transformed))
        {
            canvas.Clear(SKColors.Transparent);

            var matrix = SKMatrix.Identity;

            if (flipH)
                matrix = matrix.PreConcat(SKMatrix.CreateScale(-1, 1, _tileSize / 2f, 0));

            if (flipV)
                matrix = matrix.PreConcat(SKMatrix.CreateScale(1, -1, 0, _tileSize / 2f));

            if (rotation != 0)
                matrix = matrix.PreConcat(SKMatrix.CreateRotationDegrees(rotation, _tileSize / 2f, _tileSize / 2f));

            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(source, 0, 0);
        }

        return transformed;
    }
}
