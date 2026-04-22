using Retruxel.Core.Interfaces;

namespace Retruxel.Core.Models;

/// <summary>
/// Describes a module's identity, dependencies and configurable parameters.
/// The shell reads this manifest to auto-generate the configuration UI
/// without the module needing any knowledge of WPF.
/// </summary>
public class ModuleManifest
{
    /// <summary>Unique module identifier. Must match IModule.ModuleId.</summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>Semantic version of this module. Ex: "1.0.0"</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Module type — Graphic, Logic or Audio.</summary>
    public ModuleType Type { get; set; }

    /// <summary>
    /// List of target IDs this module is compatible with.
    /// Use ["all"] for modules compatible with every target.
    /// Ex: ["sms", "gg"], ["snes"], ["all"]
    /// </summary>
    public string[] Compatibility { get; set; } = ["all"];

    /// <summary>
    /// List of ModuleIds this module depends on.
    /// The shell ensures dependencies are loaded before this module.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Configurable parameters exposed to the UI.
    /// The shell renders each parameter as an appropriate control
    /// (slider, toggle, dropdown, etc.) based on its ParameterType.
    /// </summary>
    public ParameterDefinition[] Parameters { get; set; } = Array.Empty<ParameterDefinition>();

    /// <summary>
    /// Names of C functions this module exposes to other modules.
    /// Allows inter-module communication in the generated code.
    /// Ex: "physics_apply_gravity", "input_is_pressed"
    /// </summary>
    public string[] ExposedFunctions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Defines a single configurable parameter exposed by a module.
/// </summary>
public class ParameterDefinition
{
    /// <summary>Internal parameter name. Used as JSON key. Ex: "gravityStrength"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Label shown in the UI. Ex: "Gravity Strength"</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Helper text shown as tooltip in the UI.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parameter data type — determines which UI control the shell renders.
    /// </summary>
    public ParameterType Type { get; set; }

    /// <summary>Default value when the module is first added to a project.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Minimum allowed value. Applies to Int and Float types.</summary>
    public object? MinValue { get; set; }

    /// <summary>Maximum allowed value. Applies to Int and Float types.</summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// For ModuleReference type: comma-separated list of moduleIds this parameter accepts.
    /// Ex: "palette" → only PaletteModule instances
    ///     "sprite,metasprite" → SpriteModule or MetaspriteModule instances
    /// </summary>
    public string? ModuleFilter { get; set; }

    /// <summary>
    /// For ModuleReference type: if true, build validation fails when no module is selected.
    /// The UI shows a warning and disables the Generate ROM button.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Available options for Enum type parameters.
    /// Each entry is a display name → internal value pair.
    /// </summary>
    public Dictionary<string, string> EnumOptions { get; set; } = new();
}

/// <summary>
/// Data type of a configurable parameter.
/// Determines which UI control the shell auto-generates.
/// </summary>
public enum ParameterType
{
    /// <summary>Integer value — renders as slider or numeric input</summary>
    Int,

    /// <summary>Floating point value — renders as slider</summary>
    Float,

    /// <summary>Boolean value — renders as toggle switch</summary>
    Bool,

    /// <summary>One of a fixed set of options — renders as dropdown</summary>
    Enum,

    /// <summary>Reference to a sprite asset in the project</summary>
    SpriteRef,

    /// <summary>Reference to a tile asset in the project</summary>
    TileRef,

    /// <summary>Reference to an audio asset in the project</summary>
    AudioRef,

    /// <summary>Free text input</summary>
    String,

    /// <summary>Array of integers</summary>
    IntArray,

    /// <summary>Array of strings</summary>
    StringArray,

    /// <summary>
    /// Reference to another module instance in the same scene.
    /// Renders as a dropdown listing matching module instances + [+ Create New...] option.
    /// ModuleFilter defines which moduleIds are accepted.
    /// Stored as the elementId (GUID) of the referenced SceneElement.
    /// </summary>
    ModuleReference
}