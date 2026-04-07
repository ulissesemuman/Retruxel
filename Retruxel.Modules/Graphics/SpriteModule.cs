using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Sprite module — loads sprite tile graphics into VRAM and provides
/// functions for managing the SMS Sprite Attribute Table (SAT).
///
/// The SMS supports up to 64 sprites on screen (8 per scanline limit).
/// Sprites are 8×8 or 8×16 pixels depending on VDP mode.
/// Metasprites (larger characters) are built from multiple 8×8 sprites.
///
/// JSON format:
/// {
///   "module":       "sms.sprite",
///   "tilesAssetId": "player_tiles",   // asset ID for sprite CHR data
///   "startTile":    256,              // first VRAM sprite tile slot (256–511 recommended)
///   "doubleHeight": false             // enable 8×16 sprite mode
/// }
/// </summary>
public class SpriteModule : ILogicModule
{
    public string ModuleId    => "sms.sprite";
    public string DisplayName => "Sprite";
    public string Category    => "Graphics";
    public ModuleType Type    => ModuleType.Logic;
    public string[] Compatibility => ["sms", "gg"];

    /// <summary>Asset ID for the sprite tile CHR data.</summary>
    public string TilesAssetId { get; set; } = string.Empty;

    /// <summary>
    /// First VRAM tile slot for sprite graphics.
    /// On SMS, sprites can use tiles 0–255 (first half) or 256–511 (second half).
    /// Using the second half (256+) is recommended to separate BG and sprite tiles.
    /// </summary>
    public int StartTile { get; set; } = 256;

    /// <summary>
    /// Enable 8×16 sprite mode (VDP double-height sprites).
    /// When true, each sprite entry uses two vertically stacked 8×8 tiles.
    /// </summary>
    public bool DoubleHeight { get; set; } = false;

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
                Name        = "tilesAssetId",
                DisplayName = "Sprite Tiles Asset",
                Description = "Asset ID for the sprite CHR graphics data.",
                Type        = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name        = "startTile",
                DisplayName = "Start Tile",
                Description = "First VRAM tile slot for sprite graphics (256–511 recommended).",
                Type        = ParameterType.Int,
                DefaultValue = 256,
                MinValue    = 0,
                MaxValue    = 511
            },
            new ParameterDefinition
            {
                Name        = "doubleHeight",
                DisplayName = "Double Height (8×16)",
                Description = "Enable 8×16 sprite mode. Each sprite entry uses two stacked 8×8 tiles.",
                Type        = ParameterType.Bool,
                DefaultValue = false
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new
        {
            module       = ModuleId,
            tilesAssetId = TilesAssetId,
            startTile    = StartTile,
            doubleHeight = DoubleHeight
        };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("tilesAssetId", out var ta)) TilesAssetId = ta.GetString() ?? string.Empty;
        if (root.TryGetProperty("startTile",    out var st)) StartTile    = st.GetInt32();
        if (root.TryGetProperty("doubleHeight", out var dh)) DoubleHeight = dh.GetBoolean();
    }

    public string GetValidationSample() => Serialize();
}
