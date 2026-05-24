using Retruxel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Legacy flat element model — kept for backward compatibility with pre-refactor project files.
/// New code should use the typed collections (Planes, Entities, TextArrays) on SceneData.
/// Serialized as "elements" in .rtrxproject for migration support.
/// </summary>
[Obsolete("Use typed collections (Planes, Entities, TextArrays) instead. This will be removed in a future version after migration support is dropped.")]
public class SceneElementData
{
    [JsonPropertyName("elementId")]
    public string ElementId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("moduleId")]
    public string ModuleId { get; set; } = string.Empty;

    [JsonPropertyName("moduleState")]
    public System.Text.Json.JsonElement ModuleState { get; set; }

    [JsonPropertyName("tileX")]
    public int TileX { get; set; }

    [JsonPropertyName("tileY")]
    public int TileY { get; set; }

    [JsonPropertyName("trigger")]
    public string? Trigger { get; set; }
}

/// <summary>
/// Represents a scene in the project.
/// </summary>
public class SceneData
{
    [JsonPropertyName("sceneId")]
    public string SceneId { get; set; } = string.Empty;

    [JsonPropertyName("sceneName")]
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Palette slots for this scene.
    /// Count is fixed by the target (ITarget.GetPaletteSlotCount()).
    /// </summary>
    [JsonPropertyName("paletteSlots")]
    public List<PaletteSlotData> PaletteSlots { get; set; } = [];

    /// <summary>
    /// Scene-level module overrides.
    /// Any module listed here takes precedence over the project-level module with the same ModuleId.
    /// Can also disable a project module by setting Enabled = false.
    /// Ex: Physics with different gravity for an underwater scene.
    /// </summary>
    [JsonPropertyName("moduleOverrides")]
    public List<ProjectModuleData> ModuleOverrides { get; set; } = [];

    /// <summary>
    /// Hardware planes in this scene.
    /// Count is limited by the target (SMS = 1, SNES = 4).
    /// </summary>
    [JsonPropertyName("planes")]
    public List<PlaneData> Planes { get; set; } = [];

    /// <summary>
    /// Entities in this scene (player, enemies, etc).
    /// </summary>
    [JsonPropertyName("entities")]
    public List<EntityData> Entities { get; set; } = [];

    /// <summary>
    /// Text arrays rendered in this scene.
    /// </summary>
    [JsonPropertyName("textArrays")]
    public List<TextArrayData> TextArrays { get; set; } = [];

    /// <summary>
    /// HUD configuration for this scene.
    /// Null means no HUD (e.g. cutscene).
    /// </summary>
    [JsonPropertyName("hud")]
    public HudData? Hud { get; set; }

    /// <summary>
    /// Palette effect configuration (flash, fade, cycle).
    /// Null means no palette effect in this scene.
    /// </summary>
    [JsonPropertyName("paletteEffect")]
    public PaletteEffectData? PaletteEffect { get; set; }

    /// <summary>
    /// Background scroll configuration.
    /// Null means no scroll (static background).
    /// </summary>
    [JsonPropertyName("backgroundScroll")]
    public BackgroundScrollData? BackgroundScroll { get; set; }

    /// <summary>
    /// Legacy flat element list — kept for backward compatibility with pre-refactor project files.
    /// New code should use Planes, Entities, and TextArrays instead.
    /// Populated only when loading old .rtrxproject files; cleared after migration.
    /// </summary>
    [Obsolete("Use typed collections (Planes, Entities, TextArrays) instead. This will be removed in a future version after migration support is dropped.")]  
    [JsonPropertyName("elements")]
    public List<SceneElementData> Elements { get; set; } = [];
}