﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a Retruxel project.
/// Serialized to and deserialized from a .rtrxproject file.
/// This is the single source of truth for the project state.
/// </summary>
public class RetruxelProject
{
    /// <summary>Retruxel file format version. Used for migration on future updates.</summary>
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>Project display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the project folder.</summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Target console identifier. Set at project creation — never changes.
    /// Ex: "sms", "nes", "snes"
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Template used to create this project. Ex: "sms.platformer"</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Project creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Scene ID loaded when the game starts.
    /// </summary>
    public string InitialSceneId { get; set; } = "main";

    /// <summary>
    /// Project-level modules — initialized once in main() and persist across scenes.
    /// Each scene can override individual modules via SceneData.ModuleOverrides.
    /// Candidates: Input, Physics, Animation.
    /// </summary>
    public List<ProjectModuleData> Modules { get; set; } = [];

    /// <summary>
    /// All scenes in the project.
    /// </summary>
    public List<SceneData> Scenes { get; set; } = [];

    /// <summary>
    /// All imported assets shared across all scenes.
    /// Modules reference assets by Id.
    /// </summary>
    public List<AssetEntry> Assets { get; set; } = [];

    /// <summary>
    /// Reusable entity definitions (Prefabs).
    /// EntityData instances reference a Prefab by PrefabId.
    /// </summary>
    public List<PrefabData> Prefabs { get; set; } = [];

    /// <summary>
    /// Remappable input port bindings for this project.
    /// Initialized from the target's default InputPort definitions when the project is created.
    /// The user can remap individual buttons here — changes are persisted in the project file.
    /// Index in this list is the "input slot" referenced by EntityData.InputSlot.
    /// </summary>
    public List<InputPortBinding> InputPorts { get; set; } = [];

    /// <summary>
    /// Returns all entity instance names in the same order the CodeGenerator assigns them.
    /// Each entry is (entityName: "player_0", label: "Player").
    /// Used by the editor to populate entityRef ComboBoxes.
    /// </summary>
    public IEnumerable<(string EntityName, string Label)> GetEntityNames()
    {
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in Scenes)
        {
            foreach (var entity in scene.Entities)
            {
                var prefabId = entity.PrefabId;
                var key      = string.IsNullOrEmpty(prefabId) ? "entity" : prefabId;
                if (!counters.TryGetValue(key, out var idx)) idx = 0;
                var name = string.IsNullOrEmpty(prefabId) ? $"entity_{idx}" : $"{prefabId}_{idx}";
                var label = string.IsNullOrEmpty(entity.Label) ? name : $"{entity.Label} ({name})";
                yield return (name, label);
                counters[key] = idx + 1;
            }
        }
    }
}

/// <summary>
/// A module configured at project level — applies globally unless overridden by a scene.
/// </summary>
public class ProjectModuleData
{
    /// <summary>Module identifier. Ex: "physics", "input", "animation"</summary>
    [JsonPropertyName("moduleId")]
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>Human-readable label in the editor.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this module is active. Disabled modules are not emitted by CodeGen.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Serialized module state — parameters and values specific to this module.
    /// Deserialized by the module itself via IModule.Deserialize().
    /// </summary>
    [JsonPropertyName("state")]
    public JsonElement State { get; set; } = JsonDocument.Parse("{}").RootElement;
}

/// <summary>
/// A placed instance of a Prefab in a scene.
/// Carries only instance-specific data: position, label, and action parameter overrides.
/// All shared properties (sprite, palette, actions, input) live in the referenced PrefabData.
/// </summary>
public class EntityData
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the editor tree. Ex: "Player", "Goblin #3"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// References PrefabData.PrefabId.
    /// Determines sprite, palette, actions, and input mapping for this instance.
    /// </summary>
    [JsonPropertyName("prefabId")]
    public string PrefabId { get; set; } = string.Empty;

    /// <summary>
    /// Per-instance action parameter overrides, keyed by ActionId.
    /// Overrides the corresponding ActionInstance parameters defined in the Prefab.
    /// Ex: { "jump": { "jumpHeight": 8 } } makes this instance jump higher than the Prefab default.
    /// </summary>
    [JsonPropertyName("actionOverrides")]
    public Dictionary<string, Dictionary<string, object>> ActionOverrides { get; set; } = new();

    /// <summary>Initial tile position X in the scene.</summary>
    [JsonPropertyName("startTileX")]
    public int StartTileX { get; set; }

    /// <summary>Initial tile position Y in the scene.</summary>
    [JsonPropertyName("startTileY")]
    public int StartTileY { get; set; }

    /// <summary>Whether this entity is visible in the scene preview.</summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    // ── Legacy fields — kept for migration only, do not use in new code ──────

    /// <summary>Legacy: entity type string. Migrated to PrefabId on load.</summary>
    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    /// <summary>Legacy: sprite asset on the instance. Migrated to PrefabData on load.</summary>
    [JsonPropertyName("spriteAssetId")]
    public string? SpriteAssetId { get; set; }

    /// <summary>Legacy: palette slot on the instance. Migrated to PrefabData on load.</summary>
    [JsonPropertyName("paletteSlot")]
    public int? PaletteSlot { get; set; }

    /// <summary>Legacy: width in tiles on the instance. Migrated to PrefabData on load.</summary>
    [JsonPropertyName("widthTiles")]
    public int? WidthTiles { get; set; }

    /// <summary>Legacy: height in tiles on the instance. Migrated to PrefabData on load.</summary>
    [JsonPropertyName("heightTiles")]
    public int? HeightTiles { get; set; }

    /// <summary>Legacy: input slot index. Migrated to PrefabData.InputMapping on load.</summary>
    [JsonPropertyName("inputSlot")]
    public int? InputSlot { get; set; }
    
    [JsonPropertyName("spriteBorderBehavior")]
    public string SpriteBorderBehavior { get; set; } = "Default";
}

/// <summary>
/// Text content rendered in a scene using the font tileset.
/// </summary>
public class TextArrayData
{
    [JsonPropertyName("textId")]
    public string TextId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Text entries — each maps to a screen position + string content.</summary>
    [JsonPropertyName("entries")]
    public List<TextEntryData> Entries { get; set; } = [];
}

/// <summary>
/// A single text string rendered at a fixed position.
/// </summary>
public class TextEntryData
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("tileX")]
    public int TileX { get; set; }

    [JsonPropertyName("tileY")]
    public int TileY { get; set; }

    [JsonPropertyName("paletteSlot")]
    public int PaletteSlot { get; set; }
}

/// <summary>
/// HUD configuration for a scene.
/// </summary>
public class HudData
{
    /// <summary>
    /// User-facing HUD type.
    /// Fixed = hardware window (SMS WindowPlane), Overlay = sprite-based.
    /// </summary>
    [JsonPropertyName("hudType")]
    public HudType HudType { get; set; } = HudType.Fixed;

    /// <summary>Asset Id for the HUD tileset.</summary>
    [JsonPropertyName("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [JsonPropertyName("paletteSlot")]
    public int PaletteSlot { get; set; }
}

/// <summary>
/// Palette effect (flash, fade, color cycle) applied in a scene.
/// </summary>
public class PaletteEffectData
{
    [JsonPropertyName("effectType")]
    public string EffectType { get; set; } = string.Empty; // "flash", "fade", "cycle"

    [JsonPropertyName("targetSlot")]
    public int TargetSlot { get; set; }

    [JsonPropertyName("state")]
    public JsonElement State { get; set; } = JsonDocument.Parse("{}").RootElement;
}

/// <summary>
/// Background scroll configuration for a scene.
/// </summary>
public class BackgroundScrollData
{
    [JsonPropertyName("scrollX")]
    public bool ScrollX { get; set; }

    [JsonPropertyName("scrollY")]
    public bool ScrollY { get; set; }

    [JsonPropertyName("speedX")]
    public int SpeedX { get; set; }

    [JsonPropertyName("speedY")]
    public int SpeedY { get; set; }

    [JsonPropertyName("state")]
    public JsonElement State { get; set; } = JsonDocument.Parse("{}").RootElement;
}

/// <summary>
/// User-facing HUD type — how the HUD is presented.
/// The target resolves this to a hardware strategy internally.
/// </summary>
public enum HudType
{
    /// <summary>Fixed HUD that does not scroll. Uses hardware window if available.</summary>
    Fixed,

    /// <summary>HUD rendered as sprites over the background.</summary>
    Overlay
}