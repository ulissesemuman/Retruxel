namespace Retruxel.Core.Interfaces;

/// <summary>
/// Converts hex color strings to target-specific hardware palette format.
/// Each target implements this to convert #RRGGBB colors to native format
/// (e.g., SMS RGB222, NES palette indices, SNES RGB555).
/// </summary>
public interface IPaletteConverter
{
    /// <summary>
    /// Target identifier this converter is for.
    /// </summary>
    string TargetId { get; }

    /// <summary>
    /// Converts a sequence of hex color strings to target-native byte array.
    /// </summary>
    /// <param name="hexColors">Colors in #RRGGBB format</param>
    /// <returns>Byte array in target-specific format</returns>
    byte[] ConvertColors(IEnumerable<string> hexColors);
}
