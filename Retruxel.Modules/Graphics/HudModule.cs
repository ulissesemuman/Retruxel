using System.Text.Json;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// HUD (Heads-Up Display) module for rendering fixed UI elements like score, lives, health bars.
/// Uses target-specific strategies: WindowPlane (SMS/GG), MidFrameScroll (NES), SpriteOnly (SG-1000/Coleco).
/// </summary>
public class HudModule : IGraphicModule
{
    public string ModuleId => "hud";
    public string DisplayName => "HUD";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Graphic;
    public bool IsSingleton => false;

    public ModuleScope DefaultScope => ModuleScope.Project;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.PerScene;

    public string[] Compatibility { get; set; } = ["sms", "gg", "nes", "sg1000", "coleco", "gb", "gbc"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private HudState _state = new();

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<HudState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new HudState(), _jsonOptions);

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "windowStartLine",
                DisplayName = "Window Start Line",
                Description = "First scanline of the HUD region (0 = top of screen)",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 23
            },
            new ParameterDefinition
            {
                Name = "windowEndLine",
                DisplayName = "Window End Line",
                Description = "Last scanline of the HUD region (inclusive)",
                Type = ParameterType.Int,
                DefaultValue = 1,
                MinValue = 0,
                MaxValue = 23
            },
            new ParameterDefinition
            {
                Name = "bgAssetId",
                DisplayName = "Background Asset",
                Description = "Tile asset for HUD frame/border (optional)",
                Type = ParameterType.AssetReference,
                Required = false
            },
            new ParameterDefinition
            {
                Name = "startTile",
                DisplayName = "Start Tile",
                Description = "First VRAM tile slot for HUD graphics (0-447 for SMS)",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 447
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    private class HudState
    {
        public int WindowStartLine { get; set; } = 0;
        public int WindowEndLine { get; set; } = 1;
        public string? BgAssetId { get; set; } = null;
        public int StartTile { get; set; } = 0;
        public List<HudElement> Elements { get; set; } = [];
    }

    private class HudElement
    {
        public string Id { get; set; } = "element_0";
        public HudElementType Type { get; set; } = HudElementType.Text;
        public int Col { get; set; } = 0;
        public int Line { get; set; } = 0;

        public string? VarRef { get; set; } = null;
        public int Width { get; set; } = 5;

        public int Count { get; set; } = 3;
        public string? VarRef2 { get; set; } = null;
        public string? VarRef3 { get; set; } = null;
        public int TileFull { get; set; } = 0;
        public int TileHalf { get; set; } = 0;
        public int TileEmpty { get; set; } = 0;
    }

    private enum HudElementType
    {
        Text,
        TileGroup
    }
}
