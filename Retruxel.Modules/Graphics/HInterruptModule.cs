using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Configures the SMS VDP H-Interrupt for mid-frame palette swaps.
/// Fires a horizontal interrupt every N scanlines to swap palette colors,
/// enabling per-scanline gradient effects (sky gradients, water effects, etc.).
/// </summary>
public class HInterruptModule : IGraphicModule
{
    public string ModuleId    => "h_interrupt";
    public string DisplayName => "H-Interrupt (Scanline Palette)";
    public string Category    => "Effects";
    public ModuleType Type    => ModuleType.Graphic;
    public string[] Compatibility { get; set; } = ["sms", "gg"];
    public SingletonPolicy SingletonPolicy => SingletonPolicy.SingletonPerScene;
    public ModuleScope DefaultScope => ModuleScope.Scene;

    private HInterruptState _state = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version  = "1.0.0",
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "scanlineInterval",
                DisplayName  = "Scanline Interval",
                Description  = "Fire the interrupt every N scanlines (1 = every line, 8 = every 8 lines).",
                Type         = ParameterType.Int,
                DefaultValue = 8,
                MinValue     = 1,
                MaxValue     = 192
            },
            new ParameterDefinition
            {
                Name         = "numBands",
                DisplayName  = "Number of Bands",
                Description  = "How many palette swaps happen per frame (one per interrupt trigger).",
                Type         = ParameterType.Int,
                DefaultValue = 4,
                MinValue     = 1,
                MaxValue     = 24
            },
            new ParameterDefinition
            {
                Name         = "swapSlot",
                DisplayName  = "Palette Slot",
                Description  = "Which palette slot to swap: 0 = Background, 1 = Sprite.",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 1
            },
            new ParameterDefinition
            {
                Name         = "colorIndex",
                DisplayName  = "Color Index",
                Description  = "Index within the palette slot to change on each interrupt (0-15).",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 15
            }
        ]
    };

    public string Serialize()   => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) =>
        _state = JsonSerializer.Deserialize<HInterruptState>(json, _jsonOptions) ?? new();

    public string GetValidationSample() => "{}";
    public IEnumerable<GeneratedFile> GenerateCode()   => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    private class HInterruptState
    {
        public int ScanlineInterval { get; set; } = 8;
        public int NumBands         { get; set; } = 4;
        public int SwapSlot         { get; set; } = 0;
        public int ColorIndex       { get; set; } = 0;
    }
}
