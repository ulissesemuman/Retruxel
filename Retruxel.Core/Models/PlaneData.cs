using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a hardware plane plane in a scene.
/// SMS has 1, SNES has up to 4 (BG1-BG4).
/// Each HardwarePlane contains N logical layers merged at build time.
/// </summary>
public class PlaneData
{
    [JsonPropertyName("planeId")]
    public string PlaneId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Hardware plane identifier from the target.
    /// Ex: "bg" for SMS, "bg1"/"bg2" for SNES.
    /// </summary>
    [JsonPropertyName("hardwarePlaneId")]
    public string HardwarePlaneId { get; set; } = string.Empty;

    /// <summary>
    /// HUD flag — if true, this plane uses the hardware window (fixed, no scroll).
    /// Maps to HudType.Fixed at the target level.
    /// </summary>
    [JsonPropertyName("isHud")]
    public bool IsHud { get; set; }

    /// <summary>
    /// Default palette slot for this plane.
    /// Targets with PaletteMode.PerTile can override per tile via TileEntry.PaletteSlot.
    /// Targets with PaletteMode.PerPlane use this as the single palette for all tiles.
    /// </summary>
    [JsonPropertyName("paletteSlot")]
    public int PaletteSlot { get; set; } = 0;

    /// <summary>
    /// Logical layers composing this hardware plane.
    /// Merged by the build pipeline in order (index 0 = bottom).
    /// </summary>
    [JsonPropertyName("layers")]
    public List<PlaneLayerData> Layers { get; set; } = [];
}

/// <summary>
/// A logical layer inside a hardware BG plane.
/// Layers are an editor abstraction — the hardware sees only one merged Name Table.
/// The CodeGen merges all layers into the final Name Table at build time.
/// </summary>
public class PlaneLayerData
{
    /// <summary>
    /// Stable unique identifier for this layer within its plane.
    /// Used to persist the layer across renames.
    /// </summary>
    [JsonPropertyName("layerId")]
    public string LayerId { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the editor tree. Ex: "Background", "Midground", "Foreground"
    /// </summary>
    [JsonPropertyName("layerName")]
    public string LayerName { get; set; } = string.Empty;

    /// <summary>
    /// References AssetEntry.Id for the tileset used by this layer.
    /// Empty string means the layer has no asset assigned yet.
    /// </summary>
    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this layer is visible in the scene preview.
    /// Invisible layers are still compiled into the ROM.
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Width of the tile map for this layer in tiles.
    /// Defaults to PlaneSpecs.DefaultWidth when layer is created.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; } = 32;

    /// <summary>
    /// Height of the tile map for this layer in tiles.
    /// Defaults to PlaneSpecs.DefaultHeight when layer is created.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; } = 28;

    /// <summary>
    /// Flat array of tile entries, row-major order (row 0 left-to-right, then row 1, etc.).
    /// Length must equal Width × Height.
    /// </summary>
    [JsonPropertyName("tiles")]
    public List<TileEntry> Tiles { get; set; } = [];
}