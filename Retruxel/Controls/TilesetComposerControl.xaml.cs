using Retruxel.Core.Models;
using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Controls;

/// <summary>
/// Control for composing a custom tileset by dragging tiles from TilePickerControl.
/// Supports reordering and removal. Output drives TextAnalyzer tile generation.
/// </summary>
public partial class TilesetComposerControl : UserControl
{
    private readonly List<GlyphTile> _composedTiles = new();
    private const int TileScale = 2;
    private const int Columns = 16;

    public IReadOnlyList<GlyphTile> ComposedTiles => _composedTiles;

    public event EventHandler? CompositionChanged;

    public TilesetComposerControl()
    {
        InitializeComponent();
        RenderComposer();
    }

    public void Clear()
    {
        _composedTiles.Clear();
        CompositionChanged?.Invoke(this, EventArgs.Empty);
        RenderComposer();
    }

    public void LoadComposition(IEnumerable<GlyphTile> tiles)
    {
        _composedTiles.Clear();
        _composedTiles.AddRange(tiles);
        CompositionChanged?.Invoke(this, EventArgs.Empty);
        RenderComposer();
    }

    private void Border_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("RetruxelTileEntry"))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("RetruxelTileEntry") is not GlyphTile tile) return;

        // Avoid duplicates for character tiles
        if (tile.Character.HasValue &&
            _composedTiles.Any(t => t.Character == tile.Character))
            return;

        _composedTiles.Add(tile);
        CompositionChanged?.Invoke(this, EventArgs.Empty);
        RenderComposer();
    }

    private void ComposerImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(ComposerImage);
            int tileW = 8 * TileScale;
            int tileH = 8 * TileScale;
            int col = (int)(pos.X / tileW);
            int row = (int)(pos.Y / tileH);
            int idx = row * Columns + col;

            if (idx >= 0 && idx < _composedTiles.Count)
            {
                _composedTiles.RemoveAt(idx);
                CompositionChanged?.Invoke(this, EventArgs.Empty);
                RenderComposer();
            }
        }
    }

    private void RenderComposer()
    {
        UpdateStats();

        if (_composedTiles.Count == 0)
        {
            ComposerImage.Source = null;
            return;
        }

        int cols = Columns;
        int rows = (int)Math.Ceiling(_composedTiles.Count / (double)cols);
        int scale = TileScale;
        int tileW = 8 * scale;
        int tileH = 8 * scale;
        int totalW = cols * tileW;
        int totalH = rows * tileH;

        var skBitmap = new SKBitmap(totalW, totalH, SKColorType.Bgra8888, SKAlphaType.Premul);

        unsafe
        {
            var ptr = (byte*)skBitmap.GetPixels();
            int stride = skBitmap.RowBytes;

            for (int i = 0; i < _composedTiles.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int offX = col * tileW;
                int offY = row * tileH;

                RenderTileIntoBuffer(ptr, stride, _composedTiles[i].Bitmap, offX, offY, scale);
            }
        }

        ComposerImage.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(skBitmap);
    }


    private unsafe void RenderTileIntoBuffer(
        byte* ptr, int stride, byte[] bitmap,
        int offX, int offY, int scale)
    {
        byte bgColor = 0x26;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                bool lit = ((bitmap[row] >> col) & 1) == 1;
                byte r = lit ? (byte)0xFF : bgColor;
                byte g = lit ? (byte)0xFF : bgColor;
                byte b = lit ? (byte)0xFF : bgColor;
                byte a = (byte)0xFF;

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int px = offX + col * scale + sx;
                        int py = offY + row * scale + sy;
                        int off = py * stride + px * 4;
                        ptr[off + 0] = b;
                        ptr[off + 1] = g;
                        ptr[off + 2] = r;
                        ptr[off + 3] = a;
                    }
                }
            }
        }
    }

    private void UpdateStats()
    {
        int count = _composedTiles.Count;
        int bytes = count * 32; // SMS 4bpp planar = 32 bytes per tile
        TxtTileCount.Text = $"{count} tiles — {bytes} bytes";
    }
}
