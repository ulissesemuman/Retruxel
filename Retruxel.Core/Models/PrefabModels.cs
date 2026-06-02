using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// A configured action inside a Prefab.
/// References an ActionDefinition by ActionId and stores user-configured parameter values.
/// Multiple ActionInstances can reference the same ActionId with different parameters.
/// </summary>
public class ActionInstance
{
    /// <summary>Unique identifier for this instance within the Prefab.</summary>
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>References ActionDefinition.ActionId. Ex: "jump", "walk", "attack"</summary>
    [JsonPropertyName("actionId")]
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// User-configured parameter values for this instance.
    /// Keys match ActionParameterDef.Name. Missing keys fall back to ActionParameterDef.Default.
    /// Ex: { "jumpHeight": 6, "canDouble": true }
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Maps input buttons to ActionInstance guids within a Prefab.
/// Each Prefab can use a different port and have fully independent mappings.
/// </summary>
public class PrefabInputMapping
{
    /// <summary>Hardware port identifier. Ex: "port1", "port2"</summary>
    [JsonPropertyName("portId")]
    public string PortId { get; set; } = "port1";

    /// <summary>
    /// Maps button IDs to lists of ActionInstance.InstanceId.
    /// Array per button allows multiple actions to be triggered simultaneously.
    /// Ex: { "btn1": ["guid-jump"], "right": ["guid-walk", "guid-footstep"] }
    /// </summary>
    [JsonPropertyName("buttonMappings")]
    public Dictionary<string, List<string>> ButtonMappings { get; set; } = new();
}

/// <summary>
/// A reusable entity definition. Replaces the implicit EntityType concept.
///
/// A Prefab defines the shared properties of a class of entities:
/// sprite, dimensions, available actions, and input mapping.
/// Instances (EntityData) reference a Prefab and can override action parameters.
///
/// A Prefab can exist without actions (e.g. moving platforms, decorations).
/// A Prefab can exist without InputMapping (e.g. enemies driven by AI logic).
/// </summary>
public class PrefabData
{
    /// <summary>Unique identifier. Used as C name prefix in generated code. Ex: "player", "goblin"</summary>
    [JsonPropertyName("prefabId")]
    public string PrefabId { get; set; } = string.Empty;

    /// <summary>Human-readable name shown in the editor. Ex: "Player", "Goblin Warrior"</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Asset Id of the sprite used by this Prefab.</summary>
    [JsonPropertyName("spriteAssetId")]
    public string SpriteAssetId { get; set; } = string.Empty;

    /// <summary>Palette slot index used to render this Prefab's sprite.</summary>
    [JsonPropertyName("paletteSlot")]
    public int PaletteSlot { get; set; } = 1;

    /// <summary>Sprite width in tiles (1 tile = 8px).</summary>
    [JsonPropertyName("widthTiles")]
    public int WidthTiles { get; set; } = 2;

    /// <summary>Sprite height in tiles.</summary>
    [JsonPropertyName("heightTiles")]
    public int HeightTiles { get; set; } = 2;

    /// <summary>
    /// Actions available to this Prefab.
    /// Each ActionInstance configures an ActionDefinition with specific parameter values.
    /// Can be empty for non-interactive entities.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<ActionInstance> Actions { get; set; } = [];

    /// <summary>
    /// Input mapping for this Prefab.
    /// Null means this Prefab receives no player input.
    /// </summary>
    [JsonPropertyName("inputMapping")]
    public PrefabInputMapping? InputMapping { get; set; }
}
