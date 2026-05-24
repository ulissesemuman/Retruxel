using Retruxel.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for plane_preprocessor tool.
/// Converts ProcessedTileEntry[] to SMS nametable words with hardware-specific bit encoding.
///
/// SMS nametable word format (16-bit):
///   Bits 8-0:  tile index (0-511)
///   Bit 9:     horizontal flip
///   Bit 10:    vertical flip
///   Bit 11:    palette select (0=BG, 1=Sprite)
///   Bit 12:    priority
/// </summary>
public class SmsPlanePreprocessorExtension : IToolExtension
{
    public string ToolId => "plane_preprocessor";

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
        // Bits 8-0: tile index (9 bits)
        int nametableWord = entry.VramSlot & 0x1FF;

        // Bit 9: horizontal flip
        if (entry.FlipH) nametableWord |= (1 << 9);

        // Bit 10: vertical flip
        if (entry.FlipV) nametableWord |= (1 << 10);

        // Bit 11: palette select (0=BG, 1=Sprite)
        if (paletteSlot == 1) nametableWord |= (1 << 11);

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
/// Represents a processed tile entry (from PlanePreprocessorTool).
/// </summary>
internal class ProcessedTileEntry
{
    public int VramSlot { get; set; }
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }
    public int Rotation { get; set; }
}
