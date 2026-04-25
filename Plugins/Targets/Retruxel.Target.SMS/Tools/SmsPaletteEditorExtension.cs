using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Tools;

public class SmsPaletteEditorExtension : IToolExtension, IPaletteProvider, ITool
{
    // ITool properties
    public string ToolId => "palette_editor_ext_sms";
    public string Description => "Provides SMS palette provider for Palette Editor";
    public object? Icon => null;
    public string Category => "Extensions";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public bool RequiresProject => false;
    public bool HasUI => false;

    // IPaletteProvider properties
    public string TargetId => "sms";
    public int SlotCount => 16;
    public int GridRows => 8;
    public int GridColumns => 8;
    public object[] HardwareColors { get; }

    // Shared DisplayName property
    public string DisplayName => "Sega Master System";

    public SmsPaletteEditorExtension()
    {
        HardwareColors = SmsColorUtils.GenerateAllColors();
    }

    public string GetColorFormat(int colorIndex)
    {
        return $"SMS: ${colorIndex:X2}";
    }

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        return new Dictionary<string, object>
        {
            ["paletteProvider"] = this
        };
    }
}
