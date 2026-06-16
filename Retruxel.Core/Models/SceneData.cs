using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

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
    /// Entities in this scene.
    /// </summary>
    [JsonPropertyName("entities")]
    public List<EntityData> Entities { get; set; } = [];

    /// <summary>
    /// Text arrays rendered in this scene.
    /// </summary>
    [JsonPropertyName("textArrays")]
    public List<TextArrayData> TextArrays { get; set; } = [];

    /// <summary>
    /// Scene-level behaviors (e.g. gravity applied to all entities in the scene).
    /// Each entry is a configured BehaviorInstance with scope="scene".
    /// </summary>
    [JsonPropertyName("sceneBehaviors")]
    public List<ActionInstance> SceneBehaviors { get; set; } = [];

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
    
    [JsonPropertyName("spriteBorderBehaviorDefault")]
    public string SpriteBorderBehaviorDefault { get; set; } = "Clamp";
}
