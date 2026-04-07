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
    public string ModuleId    => "sms.animation";
    public string DisplayName => "Animation";
    public string Category    => "Logic";
    public ModuleType Type    => ModuleType.Logic;
    public string[] Compatibility => ["sms", "gg"];

    public List<AnimationClip> Animations { get; set; } = [];

    public AnimationModule()
    {
        // Default animations for a basic platformer character
        Animations =
        [
            new AnimationClip { Name = "idle",  Frames = [0, 1],       Speed = 16 },
            new AnimationClip { Name = "walk",  Frames = [2, 3, 4, 5], Speed = 8  },
            new AnimationClip { Name = "punch", Frames = [6, 7],       Speed = 4  },
            new AnimationClip { Name = "kick",  Frames = [8, 9],       Speed = 4  },
            new AnimationClip { Name = "jump",  Frames = [10],          Speed = 1  },
            new AnimationClip { Name = "hurt",  Frames = [11, 12],     Speed = 6  }
        ];
    }

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
                Name        = "animations",
                DisplayName = "Animation Clips",
                Description = "List of named animation sequences with frame indices and speed.",
                Type        = ParameterType.String,
                DefaultValue = "[]"
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new { module = ModuleId, animations = Animations };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("animations", out var animArray)) return;

        Animations = [];
        foreach (var anim in animArray.EnumerateArray())
        {
            var clip = new AnimationClip();
            if (anim.TryGetProperty("name",   out var n)) clip.Name  = n.GetString() ?? string.Empty;
            if (anim.TryGetProperty("speed",  out var s)) clip.Speed = s.GetInt32();
            if (anim.TryGetProperty("frames", out var f))
                clip.Frames = f.EnumerateArray().Select(x => x.GetInt32()).ToList();
            Animations.Add(clip);
        }
    }

    public string GetValidationSample() => Serialize();
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
