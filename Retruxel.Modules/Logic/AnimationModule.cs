using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Animation module — manages sprite animation frame sequences.
///
/// Animations are defined as named sequences of tile indices with a frame duration.
/// The generated C code provides functions to update the current frame each VBlank.
///
/// JSON format:
/// {
///   "module": "sms.animation",
///   "animations": [
///     { "name": "idle",  "frames": [0, 1],       "speed": 16 },
///     { "name": "walk",  "frames": [2, 3, 4, 5], "speed": 8  },
///     { "name": "punch", "frames": [6, 7],        "speed": 4  },
///     { "name": "kick",  "frames": [8, 9],        "speed": 4  },
///     { "name": "jump",  "frames": [10],           "speed": 1  },
///     { "name": "hurt",  "frames": [11, 12],       "speed": 6  }
///   ]
/// }
/// </summary>
public class AnimationModule : ILogicModule
{
    public string ModuleId => "animation";
    public string DisplayName => "Animation";
    public string Category => "Logic";
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton => false;
    public string[] Compatibility { get; set; } = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private AnimationState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "animations",
                DisplayName  = "Animation Clips",
                Description  = "List of named animation sequences with frame indices and speed.",
                Type         = ParameterType.String,
                DefaultValue = "[]"
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

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<AnimationState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new AnimationState(), _jsonOptions);

    private class AnimationState
    {
        public List<AnimationClip> Animations { get; set; } =
        [
            new() { Name = "idle",  Frames = [0, 1],       Speed = 16 },
            new() { Name = "walk",  Frames = [2, 3, 4, 5], Speed = 8  },
            new() { Name = "punch", Frames = [6, 7],       Speed = 4  },
            new() { Name = "kick",  Frames = [8, 9],       Speed = 4  },
            new() { Name = "jump",  Frames = [10],          Speed = 1  },
            new() { Name = "hurt",  Frames = [11, 12],     Speed = 6  }
        ];
    }
}

/// <summary>A single named animation sequence.</summary>
public class AnimationClip
{
    /// <summary>Animation name used in code. Ex: "idle", "walk", "punch"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tile indices for each frame.
    /// Each value is the tile offset from the sprite module's StartTile.
    /// For metasprites, this is the index of the first tile in the metasprite.
    /// </summary>
    public List<int> Frames { get; set; } = [];

    /// <summary>
    /// VBlank frames to hold each animation frame before advancing.
    /// At 60fps: Speed=8 → ~7.5 fps animation, Speed=16 → ~3.75 fps.
    /// </summary>
    public int Speed { get; set; } = 8;
}
