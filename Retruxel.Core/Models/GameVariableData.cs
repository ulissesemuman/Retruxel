using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// A runtime game variable managed by Retruxel.
/// Emitted as a field in the generated GameVars struct (vars.h / vars.c).
/// Examples: hp, lives, xp, mp, stamina, coins.
/// </summary>
public class GameVariableData
{
    /// <summary>
    /// Unique identifier used as the C field name.
    /// Must be a valid C identifier: lowercase, underscores only.
    /// Ex: "hp", "player_lives", "xp"
    /// </summary>
    [JsonPropertyName("variableId")]
    public string VariableId { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the editor. Ex: "Hit Points"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// C type emitted in the struct.
    /// Ex: "uint8_t", "int8_t", "uint16_t", "int16_t"
    /// uint8_t fits in one Z80 register — prefer it for SMS targets.
    /// </summary>
    [JsonPropertyName("cType")]
    public string CType { get; set; } = "uint8_t";

    /// <summary>Initial value emitted in the vars.c initializer.</summary>
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; } = "0";

    /// <summary>
    /// Minimum valid value — used for editor validation and optional codegen bounds check.
    /// </summary>
    [JsonPropertyName("minValue")]
    public int MinValue { get; set; } = 0;

    /// <summary>
    /// Maximum valid value — used for editor validation and optional codegen bounds check.
    /// </summary>
    [JsonPropertyName("maxValue")]
    public int MaxValue { get; set; } = 255;

    /// <summary>
    /// Optional group label for editor organization only. Ex: "Player", "World", "Combat".
    /// Has no effect on code generation.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// When true, this variable is saved to SRAM (battery-backed RAM) if the target supports it.
    /// The codegen emits save/load helpers for persistent variables.
    /// </summary>
    [JsonPropertyName("persistent")]
    public bool Persistent { get; set; } = false;
}
