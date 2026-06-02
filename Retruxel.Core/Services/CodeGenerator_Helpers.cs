using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

    /// <summary>
    /// Merges two JSON objects. Fields from <paramref name="overlay"/> take precedence,
    /// but fields present only in <paramref name="base"/> are preserved.
    /// This lets the original CodeGenerator-injected JSON (spriteAssetId, startTile, etc.)
    /// survive the module Deserialize→Serialize round-trip, while still picking up
    /// context flags added by InjectContextFlags (usePhysics, useInput, etc.).
    /// </summary>
    private static string MergeJson(string @base, string overlay)
    {
        try
        {
            using var baseDoc    = JsonDocument.Parse(@base);
            using var overlayDoc = JsonDocument.Parse(overlay);

            var merged = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            // Start with all fields from base
            foreach (var prop in baseDoc.RootElement.EnumerateObject())
                merged[prop.Name] = prop.Value.Clone();

            // Overlay wins for fields it knows about
            foreach (var prop in overlayDoc.RootElement.EnumerateObject())
                merged[prop.Name] = prop.Value.Clone();

            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            foreach (var (key, value) in merged)
            {
                writer.WritePropertyName(key);
                value.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // If merge fails for any reason, fall back to overlay only
            return overlay;
        }
    }
}
