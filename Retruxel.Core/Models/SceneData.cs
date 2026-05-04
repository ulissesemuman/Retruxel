using Retruxel.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents a scene in the project with all its elements and their states.
/// </summary>
public class SceneData
{
    [JsonPropertyName("sceneId")]
    public string SceneId { get; set; } = string.Empty;

    [JsonPropertyName("sceneName")]
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Palette slots for this scene. Count is fixed by the target (ITarget.GetPaletteSlotCount()).
    /// Each slot contains ColorsPerSlot hardware colors.
    /// Initialized with default colors when scene is created.
    /// </summary>
    [JsonPropertyName("paletteSlots")]
    public List<PaletteSlotData> PaletteSlots { get; set; } = new();

    [JsonPropertyName("elements")]
    public List<SceneElementData> Elements { get; set; } = [];
}

/// <summary>
/// Represents a single module instance placed in a scene.
/// </summary>
public class SceneElementData
{
    [JsonPropertyName("elementId")]
    public string ElementId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("moduleId")]
    public string ModuleId { get; set; } = string.Empty;

    [JsonPropertyName("tileX")]
    public int TileX { get; set; }

    [JsonPropertyName("tileY")]
    public int TileY { get; set; }

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("scopeOverride")]
    public ModuleScope? ScopeOverride { get; set; } = null;

    [JsonPropertyName("moduleState")]
    public JsonElement ModuleState { get; set; }

    /// <summary>
    /// Returns the effective scope for this element.
    /// Uses ScopeOverride if set, otherwise falls back to module's DefaultScope.
    /// </summary>
    public ModuleScope GetEffectiveScope(IModule module)
        => ScopeOverride ?? module.DefaultScope;
}
