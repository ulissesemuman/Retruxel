using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Input module — reads joypad state with tap and hold detection.
///
/// Supports any target controller port. The user selects which port this
/// entity listens to (PortId) and maps each hardware button to a game action
/// (ButtonMappings). The CodeGen resolves the DevkitConst for each button
/// via the target's InputPort definitions — the editor never sees raw
/// constants, and the CodeGen never sees human-readable labels.
///
/// Example ButtonMappings:
///   { "btn1": "jump", "btn2": "attack", "up": "move_up", "right": "move_right" }
///
/// JSON format:
/// {
///   "module":         "input",
///   "portId":         "port1",
///   "holdThreshold":  60,
///   "buttonMappings": { "btn1": "jump", "btn2": "attack", "up": "move_up", ... }
/// }
/// </summary>
public class InputModule : ILogicModule
{
    public string ModuleId => "input";
    public string DisplayName => "Input";
    public string Category => "Logic";
    public ModuleType Type => ModuleType.Logic;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Global;
    public string[] Compatibility { get; set; } = [];
    public ModuleScope DefaultScope => ModuleScope.Project;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private InputState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "portId",
                DisplayName = "Controller Port",
                Description = "Which hardware port this entity reads input from.",
                Type = ParameterType.Enum,
                DefaultValue = "port1",
                // EnumOptions are populated at runtime from ITarget.GetInputPorts()
                // so the editor shows "Controller 1" / "Controller 2" etc.
                EnumOptions = new() { { "Controller 1", "port1" }, { "Controller 2", "port2" } }
            },
            new ParameterDefinition
            {
                Name = "holdThreshold",
                DisplayName = "Hold Threshold (frames)",
                Description = "Frames before a button press becomes a hold. 60 ≈ 1 second at 60fps.",
                Type = ParameterType.Int,
                DefaultValue = 60,
                MinValue = 10,
                MaxValue = 300
            }
        ]
    };

    /// <summary>
    /// Creates the ViewModel for the property editor.
    /// Returns null as this module uses auto-generated UI from manifest.
    /// </summary>
    public object CreateEditorViewModel() => null!;

    /// <summary>
    /// Input module generates no assets — it only produces code.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<InputState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new InputState(), _jsonOptions);

    private class InputState
    {
        /// <summary>
        /// Hardware port identifier this entity reads from.
        /// Matches InputPort.Id from the target's InputPorts definition.
        /// Ex: "port1", "port2"
        /// </summary>
        [JsonPropertyName("portId")]
        public string PortId { get; set; } = "port1";

        /// <summary>
        /// Number of frames a button must be held before triggering a "hold" event.
        /// At 60fps: 60 frames ≈ 1 second.
        /// </summary>
        [JsonPropertyName("holdThreshold")]
        public int HoldThreshold { get; set; } = 60;

        /// <summary>
        /// Maps hardware button IDs to game action names.
        /// Key: InputButton.Id (e.g. "btn1", "up", "right")
        /// Value: game action name defined by the developer (e.g. "jump", "move_up")
        ///
        /// The CodeGen resolves the DevkitConst for each key via the target's
        /// InputPort.Buttons — the template never sees "PORT_A_KEY_1" directly.
        ///
        /// Unmapped buttons are simply not emitted in the generated code.
        /// </summary>
        [JsonPropertyName("buttonMappings")]
        public Dictionary<string, string> ButtonMappings { get; set; } = new()
        {
            { "up",    "move_up"    },
            { "down",  "move_down"  },
            { "left",  "move_left"  },
            { "right", "move_right" },
            { "btn1",  "action1"    },
            { "btn2",  "action2"    }
        };
    }
}
