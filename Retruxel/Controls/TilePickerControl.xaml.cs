using Retruxel.Core.Models;
using Retruxel.Core.Text;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Controls;

/// <summary>
/// Reusable control for displaying and selecting 8x8 tiles from various sources.
/// Supports DefaultFont, PNG, TTF, and raw bytes. Enables drag-and-drop to composer.
/// </summary>
public partial class TilePickerControl : UserControl
{
    private readonly List<GlyphTile> _tiles = new();
    private readonly HashSet<int> _selectedIndices = new();
    private Point _dragStartPos;
    private int _dragTileIdx = -1;

    // ── Dependency Properties ─────────────────────────────────────────────

    public static readonly DependencyProperty TileScaleProperty =
        DependencyProperty.Register(nameof(TileScale), typeof(int),
            typeof(TilePickerControl), new PropertyMetadata(2, OnVisualPropertyChanged));

    public int TileScale
    {
        get => (int)GetValue(TileScaleProperty);
        set => SetValue(TileScaleProperty, value);
    }

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(int),
            typeof(TilePickerControl), new PropertyMetadata(16, OnVisualPropertyChanged));

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty MultiSelectProperty =
        DependencyProperty.Register(nameof(MultiSelect), typeof(bool),
            typeof(TilePickerControl), new PropertyMetadata(false));

    public bool MultiSelect
    {
        get => (bool)GetValue(MultiSelectProperty);
        set => SetValue(MultiSelectProperty, value);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TilePickerControl control)
            control.RenderGrid();
    }

    // ── Events ────────────────────────────────────────────────────────────

    public event EventHandler<TileSelectedEventArgs>? TileSelected;
    public event EventHandler<TileDragEventArgs>? TileDragStarted;

    // ── Constructor ───────────────────────────────────────────────────────

    public TilePickerControl()
    {
        InitializeComponent();
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void LoadFromDefaultFont(char rangeStart, char rangeEnd)
    {
        var tiles = new List<GlyphTile>();

        for (char c = rangeStart; c <= rangeEnd; c++)
        {
            var bitmap = DefaultFont.GetGlyph(c);
            if (bitmap is null) continue;

            tiles.Add(new GlyphTile
            {
                Character = c,
                Bitmap = bitmap,
                SourceLabel = "DefaultFont",
                SourceIndex = c
            });
        }

        LoadFromTileEntries(tiles);
    }

    public void LoadFromPng(string pngPath)
    {
        using var stream = File.OpenRead(pngPath);
        using var bitmap = SKBitmap.Decode(stream);

        var tiles = new List<GlyphTile>();
        int tilesW = bitmap.Width / 8;
        int tilesH = bitmap.Height / 8;

        for (int ty = 0; ty < tilesH; ty++)
        {
            for (int tx = 0; tx < tilesW; tx++)
            {
                var tileBitmap = new byte[8];

                for (int row = 0; row < 8; row++)
                {
                    byte b = 0;
                    for (int col = 0; col < 8; col++)
                    {
                        var px = tx * 8 + col;
                        var py = ty * 8 + row;
                        var pixel = bitmap.GetPixel(px, py);
                        // Any non-transparent pixel = foreground
                        if (pixel.Alpha > 127 && (pixel.Red > 127 || pixel.Green > 127 || pixel.Blue > 127))
                            b |= (byte)(1 << col);
                    }
                    tileBitmap[row] = b;
                }

                int linearIndex = ty * tilesW + tx;
                tiles.Add(new GlyphTile
                {
                    Character = null,
                    Bitmap = tileBitmap,
                    SourceLabel = $"{Path.GetFileNameWithoutExtension(pngPath)} [{tx},{ty}]",
                    SourceIndex = linearIndex
                });
            }
        }

        LoadFromTileEntries(tiles);
    }

    public void LoadFromTtf(string ttfPath, float fontSize = 8f)
    {
        using var typeface = SKTypeface.FromFile(ttfPath);
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = fontSize,
            IsAntialias = false,
            Color = SKColors.White
        };

        var tiles = new List<GlyphTile>();

        for (int cp = 0x20; cp <= 0xFF; cp++)
        {
            var c = (char)cp;
            var str = c.ToString();

            using var bmp = new SKBitmap(8, 8, SKColorType.Gray8, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(bmp);
            canvas.Clear(SKColors.Black);
            canvas.DrawText(str, 0, fontSize - 1, paint);

            var bitmap = new byte[8];
            for (int row = 0; row < 8; row++)
            {
                byte b = 0;
                for (int col = 0; col < 8; col++)
                {
                    var pixel = bmp.GetPixel(col, row);
                    if (pixel.Red > 127)
                        b |= (byte)(1 << col);
                }
                bitmap[row] = b;
            }

            if (bitmap.All(b => b == 0) && c != ' ') continue;

            tiles.Add(new GlyphTile
            {
                Character = c,
                Bitmap = bitmap,
                SourceLabel = $"{Path.GetFileNameWithoutExtension(ttfPath)} TTF",
                SourceIndex = cp
            });
        }

        LoadFromTileEntries(tiles);
    }

    public void LoadFromRawBytes(byte[] fontData, char startChar)
    {
        var tiles = new List<GlyphTile>();
        int glyphCount = fontData.Length / 8;

        for (int i = 0; i < glyphCount; i++)
        {
            var bitmap = new byte[8];
            Array.Copy(fontData, i * 8, bitmap, 0, 8);

            tiles.Add(new GlyphTile
            {
                Character = (char)(startChar + i),
                Bitmap = bitmap,
                SourceLabel = "Raw bytes",
                SourceIndex = i
            });
        }

        LoadFromTileEntries(tiles);
    }

    /// <summary>
    /// Loads tiles from SMS VRAM format (4bpp planar, 32 bytes per tile).
    /// Converts to font8x8 format for display.
    /// </summary>
    public void LoadFromSmsVram(byte[] vramData, int startTile)
    {
        var tiles = new List<GlyphTile>();
        int tileCount = vramData.Length / 32;

        for (int t = 0; t < tileCount; t++)
        {
            var bitmap = new byte[8];
            int offset = t * 32;

            for (int row = 0; row < 8; row++)
            {
                byte bp0 = vramData[offset + row * 4 + 0];
                byte bp1 = vramData[offset + row * 4 + 1];
                byte bp2 = vramData[offset + row * 4 + 2];
                byte bp3 = vramData[offset + row * 4 + 3];

                byte b = 0;
                for (int col = 0; col < 8; col++)
                {
                    int bit = 7 - col;
                    int anyBit = ((bp0 >> bit) | (bp1 >> bit) | (bp2 >> bit) | (bp3 >> bit)) & 1;
                    if (anyBit == 1)
                        b |= (byte)(1 << col);
                }
                bitmap[row] = b;
            }

            tiles.Add(new GlyphTile
            {
                Character = null,
                Bitmap = bitmap,
                SourceLabel = $"VRAM tile {startTile + t}",
                SourceIndex = startTile + t
            });
        }

        LoadFromTileEntries(tiles);
    }

    public void LoadFromTileEntries(List<GlyphTile> tiles)
    {
        _tiles.Clear();
        _tiles.AddRange(tiles);
        _selectedIndices.Clear();
        RenderGrid();
    }

    public void Clear()
    {
        _tiles.Clear();
        _selectedIndices.Clear();
        TileGridImage.Source = null;
    }

    public IReadOnlyList<GlyphTile> GetSelectedTiles()
    {
        return _selectedIndices.OrderBy(i => i).Select(i => _tiles[i]).ToList();
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    private void RenderGrid()
    {
        if (_tiles.Count == 0)
        {
            TileGridImage.Source = null;
            return;
        }

        int cols = Columns;
        int rows = (int)Math.Ceiling(_tiles.Count / (double)cols);
        int scale = TileScale;
        int tileW = 8 * scale;
        int tileH = 8 * scale;
        int totalW = cols * tileW;
        int totalH = rows * tileH;

        var wb = new WriteableBitmap(totalW, totalH, 96, 96, PixelFormats.Bgra32, null);
        wb.Lock();

        unsafe
        {
            var ptr = (byte*)wb.BackBuffer;
            int stride = wb.BackBufferStride;

            for (int i = 0; i < _tiles.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int offX = col * tileW;
                int offY = row * tileH;

                bool isSelected = _selectedIndices.Contains(i);
                RenderTileIntoBuffer(ptr, stride, _tiles[i].Bitmap, offX, offY, scale, isSelected);
            }

            foreach (var idx in _selectedIndices)
            {
                int col = idx % cols;
                int row = idx / cols;
                DrawSelectionRect(ptr, stride, col * tileW, row * tileH, tileW, tileH);
            }
        }

        wb.AddDirtyRect(new Int32Rect(0, 0, totalW, totalH));
        wb.Unlock();

        TileGridImage.Source = wb;
    }

    private unsafe void RenderTileIntoBuffer(
        byte* ptr, int stride, byte[] bitmap,
        int offX, int offY, int scale, bool selected)
    {
        byte bgColor = selected ? (byte)0x40 : (byte)0x1E;

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

    private unsafe void DrawSelectionRect(
        byte* ptr, int stride,
        int x, int y, int w, int h)
    {
        byte r = 0x8E, g = 0xFF, b = 0x71, a = 0xFF;

        for (int i = 0; i < w; i++)
        {
            for (int t = 0; t < 2; t++)
            {
                int off1 = (y + t) * stride + (x + i) * 4;
                int off2 = (y + h - 1 - t) * stride + (x + i) * 4;
                ptr[off1 + 0] = b; ptr[off1 + 1] = g; ptr[off1 + 2] = r; ptr[off1 + 3] = a;
                ptr[off2 + 0] = b; ptr[off2 + 1] = g; ptr[off2 + 2] = r; ptr[off2 + 3] = a;
            }
        }

        for (int i = 0; i < h; i++)
        {
            for (int t = 0; t < 2; t++)
            {
                int off1 = (y + i) * stride + (x + t) * 4;
                int off2 = (y + i) * stride + (x + w - 1 - t) * 4;
                ptr[off1 + 0] = b; ptr[off1 + 1] = g; ptr[off1 + 2] = r; ptr[off1 + 3] = a;
                ptr[off2 + 0] = b; ptr[off2 + 1] = g; ptr[off2 + 2] = r; ptr[off2 + 3] = a;
            }
        }
    }

    // ── Interaction ───────────────────────────────────────────────────────

    private void TileGridImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(TileGridImage);
        int tileW = 8 * TileScale;
        int tileH = 8 * TileScale;
        int col = (int)(pos.X / tileW);
        int row = (int)(pos.Y / tileH);
        int idx = row * Columns + col;

        if (idx < 0 || idx >= _tiles.Count) return;

        if (MultiSelect && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            if (_selectedIndices.Contains(idx))
                _selectedIndices.Remove(idx);
            else
                _selectedIndices.Add(idx);
        }
        else
        {
            _selectedIndices.Clear();
            _selectedIndices.Add(idx);
        }

        RenderGrid();
        TileSelected?.Invoke(this, new TileSelectedEventArgs(_tiles[idx], idx));

        _dragStartPos = pos;
        _dragTileIdx = idx;
    }

    private void TileGridImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragTileIdx < 0) return;

        var pos = e.GetPosition(TileGridImage);
        var diff = pos - _dragStartPos;

        if (Math.Abs(diff.X) < 4 && Math.Abs(diff.Y) < 4) return;

        var tile = _tiles[_dragTileIdx];
        var data = new DataObject("RetruxelTileEntry", tile);
        TileDragStarted?.Invoke(this, new TileDragEventArgs(tile, _dragTileIdx));
        DragDrop.DoDragDrop(TileGridImage, data, DragDropEffects.Copy);

        _dragTileIdx = -1;
    }

    private void TileGridImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragTileIdx = -1;
    }
}
