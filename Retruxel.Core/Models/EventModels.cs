using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// A named event that an action (or other source) can emit.
/// Declared in action.json under "emits". Collected by ActionRegistry at startup.
/// </summary>
public class EventEmitDef
{
    /// <summary>Unique event identifier. Ex: "on_jump", "on_land", "on_attack"</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the binding editor. Ex: "Ao pular"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// A project-level game variable definition.
/// Stored in RetruxelProject.GameVars — replaces the GameVar module instances.
/// </summary>
public class GameVarDefinition
{
    [JsonPropertyName("variableId")]
    public string VariableId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the editor. Ex: "Score", "Lives"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>"int", "byte", "bool"</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "int";

    [JsonPropertyName("initialValue")]
    public string InitialValue { get; set; } = "0";

    [JsonPropertyName("showInHud")]
    public bool ShowInHud { get; set; } = false;
}

/// <summary>
/// Connects an event emitted by an action to a mutation on a game variable.
/// Stored in RetruxelProject.EventBindings.
/// The CodeGen reads these to generate the event_bus_dispatch() switch statement.
/// </summary>
public class EventBinding
{
    [JsonPropertyName("bindingId")]
    public string BindingId { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>Event that triggers this binding. Ex: "on_jump"</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>References GameVarDefinition.VariableId.</summary>
    [JsonPropertyName("variableId")]
    public string VariableId { get; set; } = string.Empty;

    /// <summary>What to do with the variable when the event fires.</summary>
    [JsonPropertyName("operation")]
    public EventBindingOperation Operation { get; set; } = EventBindingOperation.Add;

    /// <summary>Operand value (as string to support int/bool). Ex: "1", "true", "100"</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "1";

    /// <summary>
    /// Optional C expression guard. If non-empty, the mutation is wrapped in an if().
    /// Ex: "g_is_airborne == 0"
    /// </summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}

public enum EventBindingOperation
{
    Set,
    Add,
    Subtract,
    Toggle,
    Reset
}
