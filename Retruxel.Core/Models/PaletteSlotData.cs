using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a single palette slot in a scene.
/// Each slot contains a fixed number of colors determined by the target hardware.
/// </summary>
public class PaletteSlotData
{
    /// <summary>
    /// Zero-based index of this slot (0 = first slot, 1 = second slot, etc.).
    /// </summary>
    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; set; }

    /// <summary>
    /// Colors as hex strings (e.g., "#FF0000" for red).
    /// Count must equal ITarget.GetColorsPerSlot().
    /// </summary>
    [JsonPropertyName("colors")]
    public List<string> Colors { get; set; } = new();

    /// <summary>
    /// Optional label for editor display (e.g., "Background", "Player", "Enemies").
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}
