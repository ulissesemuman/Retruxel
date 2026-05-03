namespace Retruxel.Core.Interfaces;

/// <summary>
/// Converts 8x8 raw bitmap glyphs into the native tile format of a specific target.
/// Implemented by each target plugin.
/// </summary>
public interface IFontConverter
{
    string TargetId { get; }

    /// <summary>
    /// Converts a set of glyphs (raw 8x8 bitmaps) into a tile data byte array
    /// ready to be loaded into VRAM via SMS_loadTiles or equivalent.
    /// </summary>
    byte[] ConvertGlyphs(IEnumerable<(char Character, byte[] Bitmap)> glyphs);
}
