using SkiaSharp;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;

namespace Retruxel.Tools.FontImporter.Services;

/// <summary>
/// Rasterizes TTF/OTF glyphs to WPF BitmapSource using SkiaSharp.
/// All rendering is white-on-transparent so the Tile Editor can
/// later apply palette colors on top.
/// </summary>
public static class FontRasterizer
{
    // ── Single glyph ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a single codepoint at the given tile size.
    /// Returns null if the font has no glyph for that codepoint.
    /// </summary>
    public static BitmapSource? RenderGlyph(
        string ttfPath,
        int    codepoint,
        int    tileWidth,
        int    tileHeight)
    {
        using var typeface = LoadTypeface(ttfPath);
        if (typeface is null) return null;

        using var paint = BuildPaint(typeface, tileWidth, tileHeight);

        // Check if the font actually has a glyph for this codepoint
        var glyphId = typeface.GetGlyph(codepoint);
        if (glyphId == 0) return null;  // 0 = missing glyph

        using var bitmap = new SKBitmap(tileWidth, tileHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        DrawGlyph(canvas, paint, codepoint, tileWidth, tileHeight);

        return ToBitmapSource(bitmap);
    }

    // ── Spritesheet ───────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a full spritesheet PNG with all selected codepoints
    /// arranged in a grid of <paramref name="columnsPerRow"/> columns.
    /// Characters are laid out in codepoint order, left-to-right, top-to-bottom.
    /// </summary>
    public static BitmapSource RenderSpritesheet(
        string      ttfPath,
        List<int>   codepoints,
        int         tileWidth,
        int         tileHeight,
        int         columnsPerRow = 16)
    {
        using var typeface = LoadTypeface(ttfPath)
            ?? throw new InvalidOperationException("Failed to load font.");

        using var paint = BuildPaint(typeface, tileWidth, tileHeight);

        var rows         = (int)Math.Ceiling(codepoints.Count / (double)columnsPerRow);
        var sheetWidth   = tileWidth  * columnsPerRow;
        var sheetHeight  = tileHeight * rows;

        using var bitmap = new SKBitmap(sheetWidth, sheetHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        for (int i = 0; i < codepoints.Count; i++)
        {
            var col    = i % columnsPerRow;
            var row    = i / columnsPerRow;
            var destX  = col * tileWidth;
            var destY  = row * tileHeight;

            canvas.Save();
            canvas.Translate(destX, destY);
            DrawGlyph(canvas, paint, codepoints[i], tileWidth, tileHeight);
            canvas.Restore();
        }

        return ToBitmapSource(bitmap);
    }

    /// <summary>
    /// Encodes the spritesheet BitmapSource to a PNG byte array
    /// ready to be written to disk or embedded in the project.
    /// </summary>
    public static byte[] EncodeToPng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private static SKTypeface? LoadTypeface(string path)
        => SKTypeface.FromFile(path);

    private static SKPaint BuildPaint(SKTypeface typeface, int tileWidth, int tileHeight)
    {
        // Font size = 80% of tile height to leave a small margin
        var fontSize = tileHeight * 0.80f;

        return new SKPaint
        {
            Typeface    = typeface,
            TextSize    = fontSize,
            IsAntialias = false,   // pixel-perfect for retro targets
            Color       = SKColors.White,
            TextAlign   = SKTextAlign.Center
        };
    }

    private static void DrawGlyph(
        SKCanvas canvas,
        SKPaint  paint,
        int      codepoint,
        int      tileWidth,
        int      tileHeight)
    {
        var text = char.ConvertFromUtf32(codepoint);

        // Measure to vertically center
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);

        var x = tileWidth  / 2f;
        var y = tileHeight / 2f - bounds.MidY;  // center vertically

        canvas.DrawText(text, x, y, paint);
    }

    private static BitmapSource ToBitmapSource(SKBitmap bitmap)
    {
        using var image  = SKImage.FromBitmap(bitmap);
        using var data   = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());

        var wpfBitmap = new BitmapImage();
        wpfBitmap.BeginInit();
        wpfBitmap.CacheOption  = BitmapCacheOption.OnLoad;
        wpfBitmap.StreamSource = stream;
        wpfBitmap.EndInit();
        wpfBitmap.Freeze();

        return wpfBitmap;
    }
}
