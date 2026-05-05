using Retruxel.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for tilemap_preprocessor tool.
/// Converts ProcessedTileEntry[] to SMS nametable words with hardware-specific bit encoding.
/// 
/// SMS nametable word format (16-bit):
///   Bits 15-9: tile index (0-511, but SMS uses 0-447)
///   Bit 8: horizontal flip
///   Bit 7: vertical flip
///   Bit 4: palette select (0=BG, 1=Sprite)
///   Bits 3-0: priority (usually 0)
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

        // Get processed map from tool output (ProcessedTileEntry[])
        if (input.TryGetValue("processedMap", out var mapObj) && mapObj is object[] processedMapObj)
        {
            // Convert to ProcessedTileEntry[]
            var processedMap = ConvertToProcessedTileEntryArray(processedMapObj);

            // Convert to SMS nametable words
            var smsNametable = processedMap.Select(entry => ConvertToSmsNametableWord(entry, paletteSlot)).ToArray();

            // Format as hex
            var mapWidth = GetInt(input, "clippedWidth", 32);
            var mapHeight = GetInt(input, "clippedHeight", 24);

            result["processedMap"] = smsNametable;
            result["processedMapHex"] = FormatMapAsHex(smsNametable, mapWidth, mapHeight);
            result["processedMapFlat"] = string.Join(", ", smsNametable.Select(v => $"0x{v:X4}"));
        }

        return result;
    }

    /// <summary>
    /// Converts ProcessedTileEntry to SMS nametable word with hardware-specific bit encoding.
    /// </summary>
    private int ConvertToSmsNametableWord(ProcessedTileEntry entry, int paletteSlot)
    {
        // Bits 15-9: tile index (VRAM slot)
        int nametableWord = entry.VramSlot & 0x1FF;

        // Bit 8: horizontal flip
        if (entry.FlipH) nametableWord |= (1 << 8);

        // Bit 7: vertical flip
        if (entry.FlipV) nametableWord |= (1 << 7);

        // Bit 4: palette select (0=BG palette, 1=Sprite palette)
        if (paletteSlot == 1) nametableWord |= (1 << 4);

        // Rotation: SMS doesn't support rotation by hardware - ignored
        // (tiles with rotation should already be rotated in pixels by TilePacker)

        return nametableWord;
    }

    /// <summary>
    /// Converts object array to ProcessedTileEntry array.
    /// </summary>
    private ProcessedTileEntry[] ConvertToProcessedTileEntryArray(object[] objArray)
    {
        return objArray.Select(obj =>
        {
            var type = obj.GetType();
            var vramProp = type.GetProperty("VramSlot");
            var fhProp = type.GetProperty("FlipH");
            var fvProp = type.GetProperty("FlipV");
            var rotProp = type.GetProperty("Rotation");

            return new ProcessedTileEntry
            {
                VramSlot = vramProp != null ? (int)vramProp.GetValue(obj)! : 0,
                FlipH = fhProp != null && (bool)fhProp.GetValue(obj)!,
                FlipV = fvProp != null && (bool)fvProp.GetValue(obj)!,
                Rotation = rotProp != null ? (int)rotProp.GetValue(obj)! : 0
            };
        }).ToArray();
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

/// <summary>
/// Represents a processed tile entry (from TilemapPreprocessorTool).
/// </summary>
internal class ProcessedTileEntry
{
    public int VramSlot { get; set; }
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }
    public int Rotation { get; set; }
}
