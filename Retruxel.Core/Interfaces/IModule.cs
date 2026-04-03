namespace Retruxel.Core.Interfaces;

/// <summary>
/// Base contract for all Retruxel modules.
/// Every module — graphic, logic or audio — implements this interface.
/// </summary>
public interface IModule
{
    /// <summary>Unique module identifier. Ex: "sms.tiles", "universal.text"</summary>
    string ModuleId { get; }

    /// <summary>Display name shown in the UI.</summary>
    string DisplayName { get; }

    /// <summary>Category for UI grouping. Ex: "Background", "Sprites", "Physics", "Music"</summary>
    string Category { get; }

    /// <summary>Module type — determines which specialized interface it implements.</summary>
    ModuleType Type { get; }

    /// <summary>
    /// Indicates whether this module is universal (portable across targets)
    /// or exclusive to a specific target.
    /// </summary>
    bool IsUniversal { get; }

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
    Graphic,
    Logic,
    Audio
}