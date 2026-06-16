using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Immutable definition of an action, discovered from action.json at startup.
/// Describes what parameters the action exposes to the user.
/// Not saved in the project — lives in ActionRegistry.
/// </summary>
public record ActionDefinition(
    string ActionId,
    string DisplayName,
    string Category,
    ActionParameterDef[] Parameters,
    string[] Dependencies,   // actionIds this action requires
    string Scope             // "entity" | "scene"
);

/// <summary>
/// Definition of a single configurable parameter exposed by an action.
/// </summary>
/// <param name="Name">Parameter identifier used in templates as {{params.Name}}.</param>
/// <param name="Type">Value type: "int", "float", "bool", "string", "entityRef".</param>
/// <param name="Default">Default value if not overridden by the user.</param>
/// <param name="Label">Human-readable label shown in the editor UI.</param>
/// <param name="EntityRef">When true, the editor renders a ComboBox listing all project prefabs.</param>
/// <param name="Options">When non-empty, the editor renders a ComboBox with these fixed options. Populated from pipe-separated default values in action.json (e.g. "patrol|walk|idle").</param>
public record ActionParameterDef(
    string Name,
    string Type,
    object Default,
    string Label,
    bool EntityRef = false,
    string[] Options = null!
);
