using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Text;

/// <summary>
/// Pre-compilation pass that scans all TextArrayModules in the project,
/// extracts the minimal set of unique characters needed,
/// generates a compact tileset and a translation table.
/// </summary>
public class TextAnalyzer
{
    public TextAnalysisResult Analyze(
        RetruxelProject project,
        IEnumerable<IModule> modules,
        IFontConverter fontConverter,
        int graphicTilesEnd)
    {
        var allStrings = new List<string>();
        
        foreach (var module in modules)
        {
            if (module.ModuleId != "text.array") continue;
            
            var json = module.Serialize();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("languages", out var languages))
            {
                foreach (var lang in languages.EnumerateArray())
                {
                    if (lang.TryGetProperty("strings", out var strings))
                    {
                        foreach (var str in strings.EnumerateArray())
                        {
                            var text = str.GetString();
                            if (!string.IsNullOrEmpty(text))
                                allStrings.Add(text);
                        }
                    }
                }
            }
        }

        var uniqueChars = new HashSet<char>();
        foreach (var str in allStrings)
        {
            foreach (var c in str)
                uniqueChars.Add(c);
        }

        var sortedChars = uniqueChars.OrderBy(c => (int)c).ToList();
        
        var missingChars = new List<char>();
        var glyphsToConvert = new List<(char, byte[])>();
        
        foreach (var c in sortedChars)
        {
            var glyph = DefaultFont.GetGlyph(c);
            if (glyph is null)
            {
                missingChars.Add(c);
            }
            else
            {
                glyphsToConvert.Add((c, glyph));
            }
        }

        var tileData = fontConverter.ConvertGlyphs(glyphsToConvert);
        
        var translationTable = new byte[256];
        for (int i = 0; i < 256; i++)
            translationTable[i] = 255;
        
        for (int i = 0; i < sortedChars.Count; i++)
        {
            var c = sortedChars[i];
            if ((int)c < 256)
                translationTable[(int)c] = (byte)i;
        }

        return new TextAnalysisResult
        {
            Characters = sortedChars,
            FontStartTile = graphicTilesEnd,
            TileData = tileData,
            TranslationTable = translationTable,
            MissingChars = missingChars
        };
    }
}

public class TextAnalysisResult
{
    public List<char> Characters { get; init; } = new();
    public int FontStartTile { get; init; }
    public byte[] TileData { get; init; } = [];
    public byte[] TranslationTable { get; init; } = new byte[256];
    public List<char> MissingChars { get; init; } = new();
    public int TileCount => Characters.Count;
}
