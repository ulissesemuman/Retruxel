namespace Retruxel.Core.Models;

/// <summary>
/// Represents a hardware color from a retro console palette.
/// Core-level type with no UI framework dependencies.
/// </summary>
public record HardwareColor(byte R, byte G, byte B)
{
    /// <summary>
    /// Returns the color as a hex string (e.g., "#FF8800").
    /// </summary>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// Creates a HardwareColor from a hex string (e.g., "#FF8800" or "FF8800").
    /// </summary>
    public static HardwareColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            throw new ArgumentException("Hex color must be 6 characters (RRGGBB)");

        return new HardwareColor(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16)
        );
    }
}
