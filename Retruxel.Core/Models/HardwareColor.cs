using System;

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
        if (string.IsNullOrWhiteSpace(hex)) return new HardwareColor(0, 0, 0);
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return new HardwareColor(0, 0, 0);

        ReadOnlySpan<char> span = hex.AsSpan();
        return new HardwareColor(
            byte.Parse(span.Slice(0, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(span.Slice(2, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(span.Slice(4, 2), System.Globalization.NumberStyles.HexNumber)
        );
    }
}
