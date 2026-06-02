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
    ActionParameterDef[] Parameters
);

/// <summary>
/// Definition of a single configurable parameter exposed by an action.
/// </summary>
/// <param name="Name">Parameter identifier used in templates as {{params.Name}}.</param>
/// <param name="Type">Value type: "int", "float", "bool", "string".</param>
/// <param name="Default">Default value if not overridden by the user.</param>
/// <param name="Label">Human-readable label shown in the editor UI.</param>
public record ActionParameterDef(
    string Name,
    string Type,
    object Default,
    string Label
);
