using SkiaSharp;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Lib.WPFImageProcessing
{
    public static class ImageProcessing
    {
        public static BitmapSource ConvertSkBitmapToBitmapSource(SKBitmap skBitmap)
        {
            var bitmap = new WriteableBitmap(
                skBitmap.Width, skBitmap.Height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.Lock();
            unsafe
            {
                var src = (byte*)skBitmap.GetPixels().ToPointer();
                var dst = (byte*)bitmap.BackBuffer.ToPointer();

                int srcStride = skBitmap.RowBytes;
                int dstStride = bitmap.BackBufferStride;

                if (srcStride == dstStride)
                {
                    // Caminho rápido — cópia única, sem loop
                    Buffer.MemoryCopy(src, dst, dstStride * skBitmap.Height, srcStride * skBitmap.Height);
                }
                else
                {
                    // Strides diferentes — copia linha a linha ignorando padding
                    int copyWidth = skBitmap.Width * 4;
                    for (int y = 0; y < skBitmap.Height; y++)
                        Buffer.MemoryCopy(
                            src + y * srcStride,
                            dst + y * dstStride,
                            dstStride, copyWidth);
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, skBitmap.Width, skBitmap.Height));
            bitmap.Unlock();

            return bitmap;
        }

        public static SKBitmap ConvertBitmapSourceToSkiaBitmap(BitmapSource wpfBitmap)
        {
            // Convert WPF bitmap to byte array
            int width = wpfBitmap.PixelWidth;
            int height = wpfBitmap.PixelHeight;

            var convertedBitmap = wpfBitmap;
            if (wpfBitmap.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                convertedBitmap = new FormatConvertedBitmap(wpfBitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            }

            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            convertedBitmap.CopyPixels(pixels, stride, 0);

            // Create SKBitmap and copy pixels
            var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            unsafe
            {
                var ptr = (byte*)skBitmap.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    ptr[i] = pixels[i];
                }
            }

            return skBitmap;
        }
    }
}
