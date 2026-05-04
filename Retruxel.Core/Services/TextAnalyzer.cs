using Retruxel.Core.Interfaces;
using Retruxel.Core.Text;
using System.Text;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Analyzes TextArrayModule instances to extract font data and generate
/// compact translation tables for target hardware.
/// Uses DefaultFont (font8x8-based) instead of PNG for efficiency.
/// </summary>
public class TextAnalyzer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Analyzes all TextArrayModule instances and generates font data for code generation.
    /// 
    /// Process:
    /// 1. Collects all unique characters used across all string arrays
    /// 2. Extracts glyphs from DefaultFont (font8x8-based)
    /// 3. Converts glyphs to target-specific format via IFontConverter
    /// 4. Generates compact translation table (256 bytes, 0xFF for unused chars)
    /// 
    /// Returns dictionary with keys:
    ///   "fontStartTile"        → int (VRAM tile slot where font begins)
    ///   "fontTileCount"        → int (number of tiles in compact font)
    ///   "fontTileData"         → string (hex array C format: "0x00, 0x00, ...")
    ///   "fontTranslationTable" → string (hex array C format, 256 bytes)
    ///   "missingChars"         → List<char> (characters not in DefaultFont)
    /// </summary>
    public Dictionary<string, object> Analyze(
        IEnumerable<string> moduleJsons,
        IFontConverter fontConverter,
        int fontStartTile)
    {
        // Collect all unique characters used in all strings
        var usedChars = new HashSet<char>();
        foreach (var json in moduleJsons)
        {
            var state = JsonSerializer.Deserialize<TextArrayState>(json, _jsonOptions);
            if (state?.Languages == null) continue;

            foreach (var lang in state.Languages)
            {
                foreach (var str in lang.Strings)
                {
                    foreach (var ch in str)
                    {
                        usedChars.Add(ch);
                    }
                }
            }
        }

        // If no characters used, return empty data
        if (usedChars.Count == 0)
        {
            return new Dictionary<string, object>
            {
                ["fontStartTile"] = fontStartTile,
                ["fontTileCount"] = 0,
                ["fontTileData"] = "",
                ["fontTranslationTable"] = GenerateEmptyTranslationTable(),
                ["missingChars"] = new List<char>()
            };
        }

        // Separate supported and missing characters
        var supportedChars = usedChars.Where(DefaultFont.Supports).OrderBy(c => (int)c).ToList();
        var missingChars = usedChars.Where(c => !DefaultFont.Supports(c)).OrderBy(c => (int)c).ToList();

        // Build glyph list for conversion
        var glyphs = supportedChars
            .Select(c => (c, DefaultFont.GetGlyph(c)!))
            .ToList();

        // Convert glyphs to target format
        var tileData = fontConverter.ConvertGlyphs(glyphs);

        // Build character-to-index mapping
        var charToCompactIndex = new Dictionary<char, int>();
        for (int i = 0; i < supportedChars.Count; i++)
        {
            charToCompactIndex[supportedChars[i]] = i;
        }

        // Generate translation table (256 bytes)
        var translationTable = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            var ch = (char)i;
            translationTable[i] = charToCompactIndex.TryGetValue(ch, out var idx)
                ? (byte)idx
                : (byte)0xFF;
        }

        // Format as C hex arrays
        var fontTileData = FormatAsHexArray(tileData);
        var fontTranslationTableStr = FormatAsHexArray(translationTable);

        return new Dictionary<string, object>
        {
            ["fontStartTile"] = fontStartTile,
            ["fontTileCount"] = supportedChars.Count,
            ["fontTileData"] = fontTileData,
            ["fontTranslationTable"] = fontTranslationTableStr,
            ["missingChars"] = missingChars
        };
    }

    /// <summary>
    /// Formats byte array as C hex array string.
    /// Example: "0x00, 0x01, 0xFF"
    /// </summary>
    private string FormatAsHexArray(byte[] data)
    {
        if (data.Length == 0) return "";

        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sb.Append($"0x{data[i]:X2}");
            if (i < data.Length - 1)
                sb.Append(", ");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Generates an empty translation table (all 0xFF).
    /// </summary>
    private string GenerateEmptyTranslationTable()
    {
        var table = Enumerable.Repeat((byte)0xFF, 256).ToArray();
        return FormatAsHexArray(table);
    }

    // Internal state classes for deserialization
    private class TextArrayState
    {
        public string Name { get; set; } = "";
        public List<TextLanguage> Languages { get; set; } = [];
    }

    private class TextLanguage
    {
        public string Code { get; set; } = "";
        public List<string> Strings { get; set; } = [];
    }
}
