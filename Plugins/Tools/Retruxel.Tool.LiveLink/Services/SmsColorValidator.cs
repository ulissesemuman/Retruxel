using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Retruxel.Tool.LiveLink.Services
{
    public static class SmsColorValidator
    {
        public static (bool IsValid, int TotalColors, int InvalidColors, List<(byte R, byte G, byte B)> InvalidColorList) ValidateImage(BitmapSource bitmap)
        {
            var smsPalette = GenerateSmsMasterPalette();
            var smsColorSet = new HashSet<(byte R, byte G, byte B)>(smsPalette);
            
            var pixels = ExtractPixels(bitmap);
            var uniqueColors = pixels.Distinct().ToList();
            
            var invalidColors = uniqueColors.Where(c => !smsColorSet.Contains(c)).ToList();
            
            return (invalidColors.Count == 0, uniqueColors.Count, invalidColors.Count, invalidColors);
        }
        
        private static List<(byte R, byte G, byte B)> GenerateSmsMasterPalette()
        {
            var palette = new List<(byte R, byte G, byte B)>();
            for (int r = 0; r < 4; r++)
            {
                for (int g = 0; g < 4; g++)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        palette.Add(((byte)(r * 85), (byte)(g * 85), (byte)(b * 85)));
                    }
                }
            }
            return palette;
        }
        
        private static List<(byte R, byte G, byte B)> ExtractPixels(BitmapSource bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            
            BitmapSource convertedBitmap = bitmap;
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                convertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }
            
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            
            convertedBitmap.CopyPixels(pixels, stride, 0);
            
            var result = new List<(byte R, byte G, byte B)>();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                result.Add((pixels[i + 2], pixels[i + 1], pixels[i]));
            }
            
            return result;
        }
    }
}
