using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Base contract for all Retruxel modules.
/// Every module — graphic, logic or audio — implements this interface.
/// </summary>
public interface IModule
{
    /// <summary>Unique module identifier. Ex: "text.display", "sms.vdp.scroll"</summary>
    string ModuleId { get; }

    /// <summary>Display name shown in the UI.</summary>
    string DisplayName { get; }

    /// <summary>Category for UI grouping. Ex: "Background", "Sprites", "Physics", "Music"</summary>
    string Category { get; }

    /// <summary>Module type — determines which specialized interface it implements.</summary>
    ModuleType Type { get; }

    /// <summary>
    /// List of target IDs this module is compatible with.
    /// Use ["all"] to indicate compatibility with all targets.
    /// Ex: ["sms", "gg"] or ["snes"] or ["all"]
    /// </summary>
    string[] Compatibility { get; }

    /// <summary>Serializes the current module state to the .rtrxproject file.</summary>
    string Serialize();

    /// <summary>Restores the module state from the .rtrxproject file.</summary>
    void Deserialize(string json);

    /// <summary>
    /// Returns a minimal configuration sample for the ToolchainValidator.
    /// Auto-discovered — no manual maintenance required.
    /// </summary>
    string GetValidationSample();
}

/// <summary>Module type classification.</summary>
public enum ModuleType
{
    Graphics,
    Logic,
    Audio
}