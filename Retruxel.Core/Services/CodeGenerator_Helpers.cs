using System.Linq;

namespace Retruxel.Core.Services;

/// <summary>
/// Helper methods: formatting, utilities.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Formats a byte array as a C array initializer string.
    /// Example: "0x3C, 0x42, 0x42, 0x3C"
    /// Returns "0x00" for empty arrays to prevent C compilation errors.
    /// </summary>
    private static string FormatByteArray(byte[] data)
    {
        if (data.Length == 0) return "0x00";

        var chunks = data.Select((b, i) => new { Byte = b, Index = i })
            .GroupBy(x => x.Index / 16)
            .Select(g => string.Join(", ", g.Select(x => $"0x{x.Byte:X2}")));

        return string.Join(",\n    ", chunks);
    }
}
