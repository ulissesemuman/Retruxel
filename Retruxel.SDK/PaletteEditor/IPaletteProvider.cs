namespace Retruxel.SDK.PaletteEditor;

/// <summary>
/// Interface that target-specific IToolExtension implementations must provide
/// via the "paletteProvider" key in Execute() return dictionary.
/// Used by PaletteEditor tool to get target-specific palette information.
/// </summary>
public interface IPaletteProvider
{
    string TargetId { get; }
    string DisplayName { get; }
    int SlotCount { get; }
    object[] HardwareColors { get; }
    int GridRows { get; }
    int GridColumns { get; }
    string GetColorFormat(int colorIndex);
}
