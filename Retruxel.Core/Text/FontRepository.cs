using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Retruxel.Core.Text;

/// <summary>
/// Fetches additional fonts from u8g2 compiled fonts file.
/// Source: https://github.com/olikraus/u8g2/raw/refs/heads/master/csrc/u8x8_fonts.c
/// </summary>
public class FontRepository
{
    private static readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true });
    private static readonly Random _random = new();
    
    // CN cache
    private static readonly string[] _cdnUrls =
    [
        "https://cdn.jsdelivr.net/gh/olikraus/u8g2@master/csrc/u8x8_fonts.c",
        "https://cdn.statically.io/gh/olikraus/u8g2/master/csrc/u8x8_fonts.c"
    ];

    private static string GetRandomCdnUrl() => _cdnUrls[_random.Next(_cdnUrls.Length)];

    public static async Task<List<FontRepositoryItem>> FetchAvailableFontsAsync()
    {
        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Retruxel/1.0");
            
            // Try random CDN first, fallback to the other if it fails
            var primaryIndex = _random.Next(_cdnUrls.Length);
            var fallbackIndex = (primaryIndex + 1) % _cdnUrls.Length;
            
            string content;
            try
            {
                content = await _http.GetStringAsync(_cdnUrls[primaryIndex]);
            }
            catch
            {
                content = await _http.GetStringAsync(_cdnUrls[fallbackIndex]);
            }

            // Parse font declarations with metadata comments
            var fontPattern = @"/\*[\s\S]*?Copyright:\s*([^\n]+)[\s\S]*?\*/[\s\S]*?const\s+uint8_t\s+(u8x8_font_\w+)\[(\d+)\].*?=\s*\n((?:\s*""[^""]*""\s*\n?)+)";
            var matches = Regex.Matches(content, fontPattern, RegexOptions.Singleline);

            var fonts = new List<FontRepositoryItem>();
            foreach (Match match in matches)
            {
                var copyright = match.Groups[1].Value.Trim();
                var fontName = match.Groups[2].Value;
                var size = int.Parse(match.Groups[3].Value);

                // Extract font data to count glyphs
                var stringLiterals = match.Groups[4].Value;
                var literalPattern = @"""([^""]*)""";
                var literalMatches = Regex.Matches(stringLiterals, literalPattern);
                var fullDataString = string.Concat(literalMatches.Select(m => m.Groups[1].Value));
                var fontData = ParseCStringToBytes(fullDataString);

                // u8x8 font format: first byte is start char, second is end char
                int glyphCount = 0;
                if (fontData.Length >= 2)
                {
                    int startChar = fontData[0];
                    int endChar = fontData[1];
                    glyphCount = endChar - startChar + 1;
                }

                // Extract human-readable name from font identifier
                var displayName = fontName
                    .Replace("u8x8_font_", "")
                    .Replace("_", " ")
                    .ToUpper();

                // Filter out multi-column/row fonts
                // Keep: fonts without NxM pattern, fonts with 1x1, fonts starting with NxM
                // Remove: fonts with NxM in the middle where N or M > 1
                var dimensionPattern = @"(\d+)[xX](\d+)";
                var dimMatches = Regex.Matches(displayName, dimensionPattern);
                
                bool shouldSkip = false;
                foreach (Match dimMatch in dimMatches)
                {
                    var cols = int.Parse(dimMatch.Groups[1].Value);
                    var rows = int.Parse(dimMatch.Groups[2].Value);
                    
                    // Skip if not 1x1 and pattern is not at the very start
                    if ((cols > 1 || rows > 1) && dimMatch.Index > 0)
                    {
                        shouldSkip = true;
                        break;
                    }
                }
                
                if (shouldSkip)
                    continue;

                fonts.Add(new FontRepositoryItem
                {
                    Name = displayName,
                    FontIdentifier = fontName,
                    Size = size,
                    GlyphCount = glyphCount,
                    Copyright = copyright
                });
            }

            return fonts.OrderBy(f => f.Name).ToList();
        }
        catch
        {
            return [];
        }
    }

    public static async Task<byte[]?> DownloadFontDataAsync(string fontIdentifier)
    {
        try
        {
            // Try random CDN first, fallback to the other if it fails
            var primaryIndex = _random.Next(_cdnUrls.Length);
            var fallbackIndex = (primaryIndex + 1) % _cdnUrls.Length;
            
            string content;
            try
            {
                content = await _http.GetStringAsync(_cdnUrls[primaryIndex]);
            }
            catch
            {
                content = await _http.GetStringAsync(_cdnUrls[fallbackIndex]);
            }

            // Find the font declaration and extract all string literals
            var pattern = $@"const\s+uint8_t\s+{Regex.Escape(fontIdentifier)}\[\d+\].*?=\s*\n((?:\s*""[^""]*""\s*\n?)+)";
            var match = Regex.Match(content, pattern, RegexOptions.Singleline);

            if (!match.Success)
                return null;

            // Extract all string literals and concatenate
            var stringLiterals = match.Groups[1].Value;
            var literalPattern = @"""([^""]*)""";
            var literalMatches = Regex.Matches(stringLiterals, literalPattern);

            var fullDataString = string.Concat(literalMatches.Select(m => m.Groups[1].Value));
            return ParseCStringToBytes(fullDataString);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] ParseCStringToBytes(string cString)
    {
        var bytes = new List<byte>();

        for (int i = 0; i < cString.Length; i++)
        {
            char c = cString[i];

            if (c == '\\' && i + 1 < cString.Length)
            {
                char next = cString[i + 1];

                // Octal escape (\0, \10, \200, etc)
                if (next >= '0' && next <= '7')
                {
                    int octalValue = 0;
                    int digits = 0;
                    i++; // Skip backslash

                    while (i < cString.Length && cString[i] >= '0' && cString[i] <= '7' && digits < 3)
                    {
                        octalValue = octalValue * 8 + (cString[i] - '0');
                        i++;
                        digits++;
                    }
                    i--; // Back one since loop will increment

                    bytes.Add((byte)octalValue);
                }
                else
                {
                    // Other escapes (\n, \t, etc)
                    i++;
                    bytes.Add((byte)next);
                }
            }
            else
            {
                // Regular ASCII character
                bytes.Add((byte)c);
            }
        }

        return bytes.ToArray();
    }
}

public class FontRepositoryItem
{
    public string Name { get; set; } = "";
    public string FontIdentifier { get; set; } = "";
    public int Size { get; set; }
    public int GlyphCount { get; set; }
    public string Copyright { get; set; } = "";
}
