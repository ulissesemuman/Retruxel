using System;
using System.Collections.Generic;
using System.Text;

namespace Retruxel.Lib.ImageProcessing;

public static class TileTransformation
{

    /// <summary>
    /// Applies flip and rotation to a tile bitmap.
    /// Replicates TilesetRenderer.ApplyTransform without depending on the plugin assembly.
    /// </summary>
    public static SkiaSharp.SKBitmap ApplyTileTransform(
        SkiaSharp.SKBitmap source, bool flipH, bool flipV, int rotation, int tileSize)
    {
        var result = new SkiaSharp.SKBitmap(tileSize, tileSize,
            SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);

        using var c = new SkiaSharp.SKCanvas(result);
        c.Clear(SkiaSharp.SKColors.Transparent);

        var matrix = SkiaSharp.SKMatrix.Identity;
        float half = tileSize / 2f;

        if (flipH)
            matrix = matrix.PreConcat(SkiaSharp.SKMatrix.CreateScale(-1, 1, half, 0));
        if (flipV)
            matrix = matrix.PreConcat(SkiaSharp.SKMatrix.CreateScale(1, -1, 0, half));
        if (rotation != 0)
            matrix = matrix.PreConcat(SkiaSharp.SKMatrix.CreateRotationDegrees(rotation, half, half));

        c.SetMatrix(matrix);
        c.DrawBitmap(source, 0, 0);

        return result;
    }
}
