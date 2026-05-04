using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for tilemap_preprocessor tool.
/// Applies palette slot bit (bit 11) to nametable entries.
/// SMS nametable entry format: bit 11 = palette slot (0 or 1).
/// </summary>
public class SmsTilemapPreprocessorExtension : IToolExtension
{
    public string ToolId => "tilemap_preprocessor";

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["paletteSlot"] = 0
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, object>();

        // Get palette slot from input
        var paletteSlot = GetInt(input, "paletteSlot", 0);

        // Get processed map from tool output
        if (input.TryGetValue("processedMap", out var mapObj) && mapObj is int[] processedMap)
        {
            // Apply palette bit (bit 11) to each entry
            var paletteFlag = (paletteSlot == 1) ? 0x0800 : 0x0000;
            var mapWithPalette = processedMap.Select(entry => entry | paletteFlag).ToArray();

            // Reformat as hex
            var mapWidth = GetInt(input, "clippedWidth", 32);
            var mapHeight = GetInt(input, "clippedHeight", 24);

            result["processedMap"] = mapWithPalette;
            result["processedMapHex"] = FormatMapAsHex(mapWithPalette, mapWidth, mapHeight);
            result["processedMapFlat"] = string.Join(", ", mapWithPalette.Select(v => $"0x{v:X4}"));
        }

        return result;
    }

    private int GetInt(Dictionary<string, object> input, string key, int defaultValue = 0)
    {
        if (input.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
        }
        return defaultValue;
    }

    private string FormatMapAsHex(int[] processedMap, int mapWidth, int mapHeight)
    {
        var lines = new List<string>();

        for (int row = 0; row < mapHeight; row++)
        {
            var entries = new List<string>();

            for (int col = 0; col < mapWidth; col++)
            {
                int idx = row * mapWidth + col;
                if (idx >= processedMap.Length)
                {
                    entries.Add("0x0000");
                    continue;
                }

                entries.Add($"0x{processedMap[idx]:X4}");
            }

            bool isLast = row == mapHeight - 1;
            lines.Add($"    {string.Join(", ", entries)}{(isLast ? "" : ",")}");
        }

        return string.Join("\n", lines);
    }
}
