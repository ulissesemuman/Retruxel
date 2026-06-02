using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Type of physical input port on the target hardware.
/// </summary>
public enum InputPortType
{
    /// <summary>Standard gamepad / joystick port.</summary>
    Controller,

    /// <summary>Keyboard input (e.g. SG-1000, ColecoVision).</summary>
    Keyboard,

    /// <summary>Light gun (e.g. Sega Light Phaser).</summary>
    LightGun
}

/// <summary>
/// Describes a single mappable button on an input port.
/// Defined by the target — these are the hardware defaults.
/// </summary>
/// <param name="Id">Internal identifier used by the editor and InputModule. Ex: "btn1"</param>
/// <param name="Label">Display name shown in the UI. Ex: "Button 1"</param>
/// <param name="DevkitConst">C constant emitted by CodeGen. Ex: "PORT_A_KEY_1"</param>
public record InputButton(string Id, string Label, string DevkitConst);

/// <summary>
/// Describes a physical input port on the target hardware.
/// Defined by the target — these are the hardware defaults.
/// </summary>
/// <param name="Id">Internal identifier. Ex: "port1"</param>
/// <param name="Label">Display name shown in the UI. Ex: "Controller 1"</param>
/// <param name="Type">Port type (controller, keyboard, light gun).</param>
/// <param name="Buttons">Buttons available on this port.</param>
public record InputPort(
    string Id,
    string Label,
    InputPortType Type,
    InputButton[] Buttons);

/// <summary>
/// A remappable button binding stored in the project.
/// The user can change DevkitConst to remap a button to a different hardware key.
/// </summary>
public class InputButtonBinding
{
    /// <summary>Button identifier — matches InputButton.Id. Ex: "btn1"</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label shown in the UI. Ex: "Button 1"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// C constant emitted by CodeGen. Defaults to the target's hardware constant.
    /// The user can remap this to any valid constant for the target.
    /// Ex: "PORT_A_KEY_1"
    /// </summary>
    [JsonPropertyName("devkitConst")]
    public string DevkitConst { get; set; } = string.Empty;
}

/// <summary>
/// A remappable input port stored in the project.
/// Initialized from the target's default InputPort definitions.
/// The user can remap individual buttons without changing the port structure.
/// </summary>
public class InputPortBinding
{
    /// <summary>Port identifier — matches InputPort.Id. Ex: "port1"</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label shown in the UI. Ex: "Controller 1"</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Port type.</summary>
    [JsonPropertyName("type")]
    public InputPortType Type { get; set; } = InputPortType.Controller;

    /// <summary>Remappable button bindings for this port.</summary>
    [JsonPropertyName("buttons")]
    public List<InputButtonBinding> Buttons { get; set; } = [];
}
