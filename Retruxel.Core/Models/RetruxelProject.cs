namespace Retruxel.Core.Models;

/// <summary>
/// Represents a Retruxel project.
/// Serialized to and deserialized from a .rtrxproject file.
/// This is the single source of truth for the project state.
/// </summary>
public class RetruxelProject
{
    /// <summary>Retruxel file format version. Used for migration on future updates.</summary>
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>Project display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the project folder.</summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Target console identifier. Set at project creation — never changes.
    /// Ex: "sms", "nes", "snes"
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Template used to create this project. Ex: "sms.platformer"</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Project creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// List of ModuleIds active in this project.
    /// The shell loads these modules when the project is opened.
    /// </summary>
    public List<string> DefaultModules { get; set; } = [];

    /// <summary>
    /// All module and target settings serialized as key-value pairs.
    /// Key format: "ModuleId.ParameterName" or "target.ParameterName"
    /// Ex: { "sms.physics.gravityStrength": 8, "target.region": "PAL" }
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Serialized module states.
    /// Key is ModuleId, value is the raw JSON returned by IModule.Serialize().
    /// </summary>
    public Dictionary<string, string> ModuleStates { get; set; } = new();

    /// <summary>
    /// Whether this project uses only universal modules.
    /// Computed property — true only if all active modules have IsUniversal = true.
    /// Used to determine eligibility for target migration in the future.
    /// </summary>
    public bool IsPortable { get; set; }
}