using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.FontImporter.Services;

/// <summary>
/// Rasterizes TTF/OTF glyphs using SkiaSharp.
/// All rendering is white-on-transparent so the Tile Editor can
/// later apply palette colors on top.
/// </summary>
public static class FontRasterizer
{
    // Single glyph-

    /// <summary>
    /// Renders a single codepoint at the given tile size.
    /// Returns null if the font has no glyph for that codepoint.
    /// </summary>
    public static SKBitmap? RenderGlyph(
        string ttfPath,
        int codepoint,
        int tileWidth,
        int tileHeight,
        bool useAntialiasing = true,
        float fontSizeMultiplier = 1.0f,
        int offsetX = 0,
        int offsetY = 0)
    {
        using var typeface = LoadTypeface(ttfPath);
        if (typeface is null) return null;

        using var font = BuildFont(typeface, tileHeight, fontSizeMultiplier);
        using var paint = BuildPaint(useAntialiasing);

        // Check if the font actually has a glyph for this codepoint
        var glyphId = typeface.GetGlyph(codepoint);
        if (glyphId == 0) return null;  // 0 = missing glyph

        using var bitmap = new SKBitmap(tileWidth, tileHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        DrawGlyph(canvas, font, paint, codepoint, tileWidth, tileHeight, offsetX, offsetY);

        return bitmap;
    }

    // Spritesheet--

    /// <summary>
    /// Renders a full spritesheet PNG with all selected codepoints
    /// arranged in a grid of <paramref name="columnsPerRow"/> columns.
    /// Characters are laid out in codepoint order, left-to-right, top-to-bottom.
    /// </summary>
    public static SKBitmap RenderSpritesheet(
       string ttfPath,
       List<int> codepoints,
       int tileWidth,
       int tileHeight,
       int columnsPerRow = 16,
       bool useAntialiasing = true,
       float fontSizeMultiplier = 1.0f,
       int offsetX = 0,
       int offsetY = 0)
    {
        using var typeface = LoadTypeface(ttfPath)
            ?? throw new InvalidOperationException("Failed to load font.");

        using var font = BuildFont(typeface, tileHeight, fontSizeMultiplier);
        using var paint = BuildPaint(useAntialiasing);

        var rows = (int)Math.Ceiling(codepoints.Count / (double)columnsPerRow);
        var sheetWidth = tileWidth * columnsPerRow;
        var sheetHeight = tileHeight * rows;

        var bitmap = new SKBitmap(sheetWidth, sheetHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        for (int i = 0; i < codepoints.Count; i++)
        {
            var col = i % columnsPerRow;
            var row = i / columnsPerRow;
            var destX = col * tileWidth;
            var destY = row * tileHeight;

            canvas.Save();
            canvas.Translate(destX, destY);
            DrawGlyph(canvas, font, paint, codepoints[i], tileWidth, tileHeight, offsetX, offsetY);
            canvas.Restore();
        }

        return bitmap;
    }


    /// <summary>
    /// Encodes the spritesheet SKBitmap to a PNG byte array
    /// ready to be written to disk or embedded in the project.
    /// </summary>
    public static byte[] EncodeToPng(SKBitmap source)
    {
        using var image = SKImage.FromBitmap(source);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // Internals

    private static SKTypeface? LoadTypeface(string path)
 => SKTypeface.FromFile(path);

    private static SKFont BuildFont(SKTypeface typeface, int tileHeight, float fontSizeMultiplier = 1.0f)
    {
        // Font size = tile height * multiplier
        var fontSize = tileHeight * fontSizeMultiplier;

        return new SKFont(typeface, fontSize)
        {
            Subpixel = false  // pixel-perfect for retro targets
        };
    }

    private static SKPaint BuildPaint(bool useAntialiasing = true)
    {
        return new SKPaint
        {
            IsAntialias = useAntialiasing,
            Color = SKColors.White
        };
    }

    private static void DrawGlyph(
        SKCanvas canvas,
        SKFont font,
        SKPaint paint,
        int codepoint,
        int tileWidth,
        int tileHeight,
        int offsetX = 0,
        int offsetY = 0)
    {
        var text = char.ConvertFromUtf32(codepoint);

        // Measure to vertically center
        var bounds = new SKRect();
        font.MeasureText(text, out bounds);

        var x = tileWidth / 2f - bounds.MidX + offsetX;  // center horizontally + offset
        var y = tileHeight / 2f - bounds.MidY + offsetY;  // center vertically + offset

        canvas.DrawText(text, x, y, font, paint);
    }
}
