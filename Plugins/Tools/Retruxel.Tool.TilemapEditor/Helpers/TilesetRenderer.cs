using Retruxel.Core.Models;
using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles tileset image loading and tile extraction.
/// All extracted tiles (base + transformed) are cached as BitmapSource (managed WPF).
/// Cache is cleared when a new tileset is loaded.
/// </summary>
public class TilesetRenderer
{
    private SKBitmap? _tilesetImage;
    private int _columns;
    private int _rows;
    private int _tileSize;

    // Cache: (tileId, flipH, flipV, rotation) → BitmapSource
    private readonly Dictionary<(int tileId, bool flipH, bool flipV, int rotation), BitmapSource> _tileCache = new();

    public SKBitmap? Image => _tilesetImage;
    public int Columns => _columns;
    public int Rows => _rows;
    public int TotalTiles => _columns * _rows;

    public void LoadTileset(string imagePath, int tileSize)
    {
        ClearCache();
        _tileSize = tileSize;

        using var stream = File.OpenRead(imagePath);
        _tilesetImage = SKBitmap.Decode(stream);

        if (_tilesetImage != null)
        {
            _columns = _tilesetImage.Width  / tileSize;
            _rows    = _tilesetImage.Height / tileSize;
        }
    }

    public void LoadFromBitmap(SKBitmap bitmap, int tileSize)
    {
        ClearCache();
        _tileSize     = tileSize;
        _tilesetImage = bitmap;
        _columns      = bitmap.Width  / tileSize;
        _rows         = bitmap.Height / tileSize;
    }

    /// <summary>
    /// Extracts a tile with flip/rotation applied. Result is cached as BitmapSource.
    /// </summary>
    public BitmapSource? ExtractTile(TileEntry entry)
    {
        if (_tilesetImage == null) return null;

        var key = (entry.TileIndex, entry.FlipH, entry.FlipV, entry.Rotation);
        if (_tileCache.TryGetValue(key, out var cached))
            return cached;

        var skTile = ExtractSkTile(entry.TileIndex);
        if (skTile == null) return null;

        SKBitmap final;
        bool needsDispose;

        if (!entry.FlipH && !entry.FlipV && entry.Rotation == 0)
        {
            final = skTile;
            needsDispose = true;
        }
        else
        {
            final = ApplyTransform(skTile, entry.FlipH, entry.FlipV, entry.Rotation);
            skTile.Dispose();
            needsDispose = true;
        }

        var bitmapSource = ImageProcessing.ConvertSkBitmapToBitmapSource(final);
        if (needsDispose) final.Dispose();

        _tileCache[key] = bitmapSource;
        return bitmapSource;
    }

    /// <summary>
    /// Extracts a base tile by ID as SKBitmap. Caller must dispose.
    /// Used for compositing (RebuildTilesetBitmap, ExportPng, RenderTileBlock).
    /// </summary>
    public SKBitmap? ExtractSkTile(int tileId)
    {
        if (_tilesetImage == null) return null;

        var tile = new SKBitmap(_tileSize, _tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        if (tileId >= TotalTiles)
        {
            using var c = new SKCanvas(tile);
            c.Clear(SKColors.Black);
        }
        else
        {
            int srcX = (tileId % _columns) * _tileSize;
            int srcY = (tileId / _columns) * _tileSize;

            using var c = new SKCanvas(tile);
            var srcRect  = new SKRect(srcX, srcY, srcX + _tileSize, srcY + _tileSize);
            var destRect = new SKRect(0, 0, _tileSize, _tileSize);
            c.DrawBitmap(_tilesetImage, srcRect, destRect);
        }

        return tile;
    }

    private SKBitmap ApplyTransform(SKBitmap source, bool flipH, bool flipV, int rotation)
    {
        var transformed = new SKBitmap(_tileSize, _tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(transformed);
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

        return transformed;
    }

    public void ClearCache() => _tileCache.Clear();
}
