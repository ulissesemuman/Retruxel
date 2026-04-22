using Retruxel.Core.Interfaces;
using Retruxel.SDK.PaletteEditor;

namespace Retruxel.Target.SMS.Tools;

public class SmsPaletteEditorExtension : IToolExtension, IPaletteProvider
{
    private static readonly byte[] ColorLevels = { 0, 85, 170, 255 };
    
    public string ToolId => "palette_editor";
    
    public string TargetId => "sms";
    public string DisplayName => "Sega Master System";
    public int SlotCount => 16;
    public int GridRows => 8;
    public int GridColumns => 8;
    
    public object[] HardwareColors { get; }

    public SmsPaletteEditorExtension()
    {
        HardwareColors = GenerateColors();
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

    private static object[] GenerateColors()
    {
        var colors = new object[64];
        int index = 0;
        
        for (int b = 0; b < 4; b++)
        {
            for (int g = 0; g < 4; g++)
            {
                for (int r = 0; r < 4; r++)
                {
                    colors[index++] = new { R = ColorLevels[r], G = ColorLevels[g], B = ColorLevels[b] };
                }
            }
        }
        
        return colors;
    }
}
