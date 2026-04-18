using SkiaSharp;
using System.IO;

namespace Retruxel.Lib.ImageProcessing;

public class PngReader
{
    public SKBitmap Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image file not found: {path}");

        return SKBitmap.Decode(path);
    }
}
