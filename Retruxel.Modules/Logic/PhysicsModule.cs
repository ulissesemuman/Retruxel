using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Physics module — gravity, velocity and ground collision for a single entity.
///
/// Uses fixed-point arithmetic (positions stored as pixels × 16) for smooth
/// sub-pixel movement without floating point, which is expensive on the Z80.
///
/// The ground Y position is configurable — for Kung Fu Master each floor
/// of the building has a different ground level.
///
/// JSON format:
/// {
///   "module":       "sms.physics",
///   "gravity":      2,      // acceleration per frame (fixed-point units)
///   "maxFallSpeed": 24,     // terminal velocity (fixed-point units)
///   "jumpForce":    52,     // upward velocity when jumping (fixed-point units)
///   "walkSpeed":    8,      // horizontal speed when walking (fixed-point units)
///   "groundY":      144,    // ground level Y in pixels (screen pixel, not fixed-point)
///   "screenLeft":   0,      // leftmost X boundary in pixels
///   "screenRight":  248     // rightmost X boundary in pixels (256 - sprite width)
/// }
/// </summary>
public class PhysicsModule : ILogicModule
{
    public string ModuleId    => "sms.physics";
    public string DisplayName => "Physics";
    public string Category    => "Logic";
    public ModuleType Type    => ModuleType.Logic;
    public string[] Compatibility => ["sms", "gg"];

    /// <summary>Downward acceleration applied each frame (fixed-point × 16).</summary>
    public int Gravity { get; set; } = 2;

    /// <summary>Maximum downward velocity — caps freefall speed (fixed-point × 16).</summary>
    public int MaxFallSpeed { get; set; } = 24;

    /// <summary>Upward velocity applied when jumping (fixed-point × 16).</summary>
    public int JumpForce { get; set; } = 52;

    /// <summary>Horizontal speed when walking left or right (fixed-point × 16).</summary>
    public int WalkSpeed { get; set; } = 8;

    /// <summary>Ground level Y in screen pixels. Entity stops falling here.</summary>
    public int GroundY { get; set; } = 144;

    /// <summary>Left boundary in screen pixels. Entity cannot move past this.</summary>
    public int ScreenLeft { get; set; } = 0;

    /// <summary>Right boundary in screen pixels. Entity cannot move past this.</summary>
    public int ScreenRight { get; set; } = 248;

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
                Name        = "gravity",
                DisplayName = "Gravity",
                Description = "Downward acceleration per frame (fixed-point units).",
                Type        = ParameterType.Int,
                DefaultValue = 2, MinValue = 1, MaxValue = 16
            },
            new ParameterDefinition
            {
                Name        = "maxFallSpeed",
                DisplayName = "Max Fall Speed",
                Description = "Terminal velocity — caps freefall speed (fixed-point units).",
                Type        = ParameterType.Int,
                DefaultValue = 24, MinValue = 4, MaxValue = 64
            },
            new ParameterDefinition
            {
                Name        = "jumpForce",
                DisplayName = "Jump Force",
                Description = "Upward velocity when jumping (fixed-point units). Higher = higher jump.",
                Type        = ParameterType.Int,
                DefaultValue = 52, MinValue = 16, MaxValue = 128
            },
            new ParameterDefinition
            {
                Name        = "walkSpeed",
                DisplayName = "Walk Speed",
                Description = "Horizontal speed when walking (fixed-point units).",
                Type        = ParameterType.Int,
                DefaultValue = 8, MinValue = 2, MaxValue = 32
            },
            new ParameterDefinition
            {
                Name        = "groundY",
                DisplayName = "Ground Y (pixels)",
                Description = "Ground level Y position in screen pixels.",
                Type        = ParameterType.Int,
                DefaultValue = 144, MinValue = 0, MaxValue = 191
            },
            new ParameterDefinition
            {
                Name        = "screenLeft",
                DisplayName = "Screen Left Boundary",
                Description = "Leftmost X boundary in screen pixels.",
                Type        = ParameterType.Int,
                DefaultValue = 0, MinValue = 0, MaxValue = 255
            },
            new ParameterDefinition
            {
                Name        = "screenRight",
                DisplayName = "Screen Right Boundary",
                Description = "Rightmost X boundary in screen pixels.",
                Type        = ParameterType.Int,
                DefaultValue = 248, MinValue = 0, MaxValue = 255
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new
        {
            module       = ModuleId,
            gravity      = Gravity,
            maxFallSpeed = MaxFallSpeed,
            jumpForce    = JumpForce,
            walkSpeed    = WalkSpeed,
            groundY      = GroundY,
            screenLeft   = ScreenLeft,
            screenRight  = ScreenRight
        };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("gravity",      out var g))  Gravity      = g.GetInt32();
        if (root.TryGetProperty("maxFallSpeed", out var mf)) MaxFallSpeed = mf.GetInt32();
        if (root.TryGetProperty("jumpForce",    out var jf)) JumpForce    = jf.GetInt32();
        if (root.TryGetProperty("walkSpeed",    out var ws)) WalkSpeed    = ws.GetInt32();
        if (root.TryGetProperty("groundY",      out var gy)) GroundY      = gy.GetInt32();
        if (root.TryGetProperty("screenLeft",   out var sl)) ScreenLeft   = sl.GetInt32();
        if (root.TryGetProperty("screenRight",  out var sr)) ScreenRight  = sr.GetInt32();
    }

    public string GetValidationSample() => Serialize();
}
