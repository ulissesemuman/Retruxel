using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Retruxel.Core.Helpers;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Handles tileset image loading and tile extraction.
/// </summary>
public class TilesetRenderer
{
    private BitmapSource? _tilesetImage;
    private int _columns;
    private int _rows;
    private int _tileSize;

    public BitmapSource? Image => _tilesetImage;
    public int Columns => _columns;
    public int Rows => _rows;
    public int TotalTiles => _columns * _rows;

    public void LoadTileset(string imagePath, int tileSize)
    {
        _tileSize = tileSize;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        _tilesetImage = bitmap;
        _columns = bitmap.PixelWidth / tileSize;
        _rows = bitmap.PixelHeight / tileSize;
    }

    public void LoadFromBitmap(BitmapSource bitmap, int tileSize)
    {
        _tileSize = tileSize;
        _tilesetImage = bitmap;
        _columns = bitmap.PixelWidth / tileSize;
        _rows = bitmap.PixelHeight / tileSize;
    }

    public BitmapSource ExtractTile(int tileId)
    {
        if (_tilesetImage == null) return null!;

        // Out of bounds - return black placeholder
        if (tileId >= TotalTiles)
        {
            var placeholder = new WriteableBitmap(_tileSize, _tileSize, 96, 96, PixelFormats.Bgra32, null);
            placeholder.Lock();
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)placeholder.BackBuffer;
                    int stride = placeholder.BackBufferStride;
                    for (int y = 0; y < _tileSize; y++)
                    {
                        for (int x = 0; x < _tileSize; x++)
                        {
                            int offset = y * stride + x * 4;
                            ptr[offset] = 0;
                            ptr[offset + 1] = 0;
                            ptr[offset + 2] = 0;
                            ptr[offset + 3] = 255;
                        }
                    }
                }
                placeholder.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _tileSize, _tileSize));
            }
            finally
            {
                placeholder.Unlock();
            }
            placeholder.Freeze();
            return placeholder;
        }

        int srcX = (tileId % _columns) * _tileSize;
        int srcY = (tileId / _columns) * _tileSize;

        var croppedBitmap = new CroppedBitmap(_tilesetImage, new System.Windows.Int32Rect(srcX, srcY, _tileSize, _tileSize));
        croppedBitmap.Freeze();
        return croppedBitmap;
    }

    /// <summary>
    /// Extracts a tile with flip flags decoded from encoded tilemap entry.
    /// </summary>
    public BitmapSource ExtractTileEncoded(int encodedValue)
    {
        int tileId = TilemapEntryEncoding.DecodeTileIndex(encodedValue);
        bool flipH = TilemapEntryEncoding.DecodeFlipH(encodedValue);
        bool flipV = TilemapEntryEncoding.DecodeFlipV(encodedValue);

        var baseTile = ExtractTile(tileId);
        
        if (!flipH && !flipV)
            return baseTile;

        return ApplyTransform(baseTile, flipH, flipV);
    }

    /// <summary>
    /// Applies horizontal and/or vertical flip to a tile bitmap.
    /// </summary>
    private BitmapSource ApplyTransform(BitmapSource source, bool flipH, bool flipV)
    {
        var transform = new TransformGroup();
        
        if (flipH)
            transform.Children.Add(new ScaleTransform(-1, 1, _tileSize / 2.0, _tileSize / 2.0));
        
        if (flipV)
            transform.Children.Add(new ScaleTransform(1, -1, _tileSize / 2.0, _tileSize / 2.0));

        var transformed = new TransformedBitmap(source, transform);
        transformed.Freeze();
        return transformed;
    }
}
