namespace Retruxel.Core.Interfaces;

/// <summary>
/// Converts 8x8 raw bitmap glyphs (font8x8 format) into the native tile
/// format of a specific target hardware.
///
/// Input: glyph bytes in font8x8 format (8 bytes, row-wise, LSB = pixel 0)
/// Output: tile data bytes ready to be loaded into VRAM
/// </summary>
public interface IFontConverter
{
    string TargetId { get; }

    /// <summary>
    /// Converts a sequence of glyphs into a contiguous tile data array.
    /// Order of glyphs in input determines tile indices in output.
    /// </summary>
    byte[] ConvertGlyphs(IEnumerable<(char Character, byte[] Bitmap)> glyphs);
}
