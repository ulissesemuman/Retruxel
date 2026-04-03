namespace Retruxel.Core.Models;

/// <summary>
/// Represents a project template available for a specific target.
/// Templates pre-configure a set of modules for common game types.
/// Shown in the New Project wizard after the target is selected.
/// </summary>
public class ProjectTemplate
{
    /// <summary>Unique template identifier. Ex: "sms.platformer", "sms.blank"</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Display name shown in the wizard. Ex: "Platformer", "Blank Project"</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description of what this template sets up.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Preview image path relative to the target's resources folder.
    /// Shown as thumbnail in the New Project wizard.
    /// </summary>
    public string PreviewImagePath { get; set; } = string.Empty;

    /// <summary>
    /// List of ModuleIds pre-configured by this template.
    /// The shell loads and initializes these modules when the project is created.
    /// </summary>
    public string[] DefaultModules { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Default parameter values applied when this template is used.
    /// Key is "ModuleId.ParameterName", value is the default.
    /// Ex: { "sms.physics.gravityStrength": 8 }
    /// </summary>
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
}