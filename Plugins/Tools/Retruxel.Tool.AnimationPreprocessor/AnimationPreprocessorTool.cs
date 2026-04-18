using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.AnimationPreprocessor;

/// <summary>
/// Generic animation preprocessor tool.
/// Processes animation clips (name, frames, speed) and returns formatted arrays.
/// Returns raw data - the CodeGen template decides the format.
/// </summary>
public class AnimationPreprocessorTool : ITool
{
    public string ToolId => "animation_preprocessor";
    public string DisplayName => "Animation Preprocessor";
    public string Description => "Processes animation clips and generates frame arrays, speed arrays, and enums";
    public string Category => "Logic";
    public string? TargetId => null;
    public string? ModuleId => "animation";
    public object? Icon => null;
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public bool IsSingleton => true;
    public bool RequiresProject => false;

    public IEnumerable<string> Validate(Dictionary<string, object> input)
    {
        var errors = new List<string>();

        if (!input.TryGetValue("animations", out var anims) || anims is not object[] clips || clips.Length == 0)
        {
            errors.Add("No animation clips defined");
            return errors;
        }

        foreach (var clip in clips)
        {
            if (clip is not Dictionary<string, object> clipDict)
                continue;

            var name = clipDict.TryGetValue("name", out var n) ? n?.ToString() : "";
            var frames = clipDict.TryGetValue("frames", out var f) && f is int[] fr ? fr : Array.Empty<int>();
            var speed = clipDict.TryGetValue("speed", out var s) ? Convert.ToInt32(s) : 0;

            if (string.IsNullOrEmpty(name))
                errors.Add("Animation clip has no name");

            if (frames.Length == 0)
                errors.Add($"Clip '{name}' has no frames");

            if (speed <= 0)
                errors.Add($"Clip '{name}' has invalid speed ({speed})");
        }

        return errors;
    }

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var animations = input.TryGetValue("animations", out var anims) && anims is object[] clips
            ? clips
            : Array.Empty<object>();

        var processedClips = new List<AnimationClip>();

        foreach (var clip in animations)
        {
            if (clip is not Dictionary<string, object> clipDict)
                continue;

            var name = clipDict.TryGetValue("name", out var n) ? n?.ToString() ?? "anim" : "anim";
            var frames = clipDict.TryGetValue("frames", out var f) && f is int[] fr ? fr : Array.Empty<int>();
            var speed = clipDict.TryGetValue("speed", out var s) ? Convert.ToInt32(s) : 8;

            processedClips.Add(new AnimationClip
            {
                Name = name,
                Frames = frames,
                Speed = speed
            });
        }

        // Generate outputs
        var clipCount = processedClips.Count;
        var enumEntries = GenerateEnumEntries(processedClips);
        var frameArrays = GenerateFrameArrays(processedClips);
        var speedArray = string.Join(", ", processedClips.Select(c => c.Speed));
        var frameCountArray = string.Join(", ", processedClips.Select(c => c.Frames.Length));
        var pointerArray = string.Join(",\n    ", processedClips.Select(c => $"anim_{c.Name.ToLower()}_frames"));

        return new Dictionary<string, object>
        {
            ["clipCount"] = clipCount,
            ["enumEntries"] = enumEntries,
            ["frameArrays"] = frameArrays,
            ["speedArray"] = speedArray,
            ["frameCountArray"] = frameCountArray,
            ["pointerArray"] = pointerArray,
            ["clips"] = processedClips.Select(c => new Dictionary<string, object>
            {
                ["name"] = c.Name,
                ["nameLower"] = c.Name.ToLower(),
                ["nameUpper"] = c.Name.ToUpper(),
                ["frames"] = c.Frames,
                ["framesString"] = string.Join(", ", c.Frames),
                ["speed"] = c.Speed,
                ["frameCount"] = c.Frames.Length
            }).ToArray()
        };
    }

    private string GenerateEnumEntries(List<AnimationClip> clips)
    {
        var entries = new List<string>();
        for (int i = 0; i < clips.Count; i++)
        {
            entries.Add($"    ANIM_{clips[i].Name.ToUpper()} = {i},");
        }
        entries.Add($"    ANIM_COUNT = {clips.Count}");
        return string.Join("\n", entries);
    }

    private string GenerateFrameArrays(List<AnimationClip> clips)
    {
        var arrays = new List<string>();
        foreach (var clip in clips)
        {
            arrays.Add($"static const unsigned char anim_{clip.Name.ToLower()}_frames[] = {{\n    {string.Join(", ", clip.Frames)}\n}};");
        }
        return string.Join("\n", arrays);
    }

    private class AnimationClip
    {
        public string Name { get; set; } = "";
        public int[] Frames { get; set; } = Array.Empty<int>();
        public int Speed { get; set; }
    }
}
