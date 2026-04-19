using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for the tilemap_preprocessor tool.
/// Adds SMS nametable entry format (2 bytes per tile, bits 0-8 index, bits 9-12 flags).
/// The generic tool handles collision bitfield and map offset math.
/// This extension adds SMS-specific nametable formatting if needed.
/// </summary>
public class SmsTilemapPreprocessorExtension : IToolExtension
{
    public string ToolId => "tilemap_preprocessor";

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // For now, SMS nametable format matches the generic output.
        // Override here if SMS-specific flags (flip H/V, palette bit) are needed.
        
        // Example: if we wanted to add SMS-specific nametable entries with flags:
        // var processedMap = input["processedMap"] as int[] ?? Array.Empty<int>();
        // var smsNametable = processedMap.Select(tile => 
        //     (ushort)(tile | 0x0000) // bits 9-12 could be flip/palette flags
        // ).ToArray();
        // 
        // return new Dictionary<string, object>
        // {
        //     ["smsNametable"] = smsNametable,
        //     ["smsNametableHex"] = FormatSmsNametable(smsNametable)
        // };

        // Currently no SMS-specific processing needed
        return new Dictionary<string, object>();
    }
}
