using Retruxel.Core.Interfaces;
using Retruxel.SDK.PaletteEditor;

namespace Retruxel.Target.SMS.Tools;

public class SmsPaletteEditorExtension : IToolExtension, IPaletteProvider
{
    public string ToolId => "palette_editor";
    
    public string TargetId => "sms";
    public string DisplayName => "Sega Master System";
    public int SlotCount => 16;
    public int GridRows => 8;
    public int GridColumns => 8;
    
    public object[] HardwareColors { get; }

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
