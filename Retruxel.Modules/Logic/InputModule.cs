using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Input module — reads joypad state with tap and hold detection.
///
/// Supports the SMS 2-button controller:
///   - D-pad: up, down, left, right
///   - Button 1: tap (short press) and hold (≥ holdThreshold frames)
///   - Button 2: tap (short press) and hold (≥ holdThreshold frames)
///
/// The hold mechanic is used to trigger special actions — ex: hold Button 1
/// for 1 second to kick instead of punch in Kung Fu Master.
///
/// JSON format:
/// {
///   "module":         "sms.input",
///   "holdThreshold": 60,     // frames before a button press is considered "held" (~1s at 60fps)
///   "port":          1       // joypad port (1 or 2)
/// }
/// </summary>
public class InputModule : ILogicModule
{
    public string     ModuleId     => "sms.input";
    public string     DisplayName  => "Input";
    public string     Category     => "Logic";
    public ModuleType Type         => ModuleType.Logic;
    public bool     IsSingleton  => true;
    public string[]   Compatibility => ["sms", "gg"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private InputState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId      = ModuleId,
        Version       = "1.0.0",
        Type          = ModuleType.Logic,
        Compatibility = Compatibility,
        Parameters    =
        [
            new ParameterDefinition
            {
                Name         = "holdThreshold",
                DisplayName  = "Hold Threshold (frames)",
                Description  = "Frames before a button press becomes a hold. 60 ≈ 1 second at 60fps.",
                Type         = ParameterType.Int,
                DefaultValue = 60,
                MinValue     = 10,
                MaxValue     = 300
            },
            new ParameterDefinition
            {
                Name         = "port",
                DisplayName  = "Joypad Port",
                Description  = "Joypad port to read (1 or 2).",
                Type         = ParameterType.Enum,
                DefaultValue = "1",
                EnumOptions  = new() { { "Port 1", "1" }, { "Port 2", "2" } }
            }
        ]
    };

    /// <summary>
    /// Creates the ViewModel for the property editor.
    /// Returns null as this module uses auto-generated UI from manifest.
    /// </summary>
    public object CreateEditorViewModel() => null!;

    /// <summary>
    /// Generates font tiles as assets.
    /// Each character in the text becomes a tile in the asset.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()              => JsonSerializer.Serialize(_state, _jsonOptions);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<InputState>(json, _jsonOptions) ?? new();
    public string GetValidationSample()    => JsonSerializer.Serialize(new InputState(), _jsonOptions);

    private class InputState
    {
        /// <summary>
        /// Number of frames a button must be held before triggering a "hold" event.
        /// At 60fps: 60 frames ≈ 1 second.
        /// </summary>
        public int HoldThreshold { get; set; } = 60;

        /// <summary>Joypad port to read (1 or 2).</summary>
        public int Port { get; set; } = 1;
    }
}
