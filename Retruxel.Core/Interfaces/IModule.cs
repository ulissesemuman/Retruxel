using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Base contract for all Retruxel modules.
/// Every module — graphic, logic or audio — implements this interface.
/// </summary>
public interface IModule
{
    /// <summary>Unique module identifier. Ex: "text.display", "scroll"</summary>
    string ModuleId { get; }

    /// <summary>Display name shown in the UI.</summary>
    string DisplayName { get; }

    /// <summary>Category for UI grouping. Ex: "Background", "Sprites", "Physics", "Music"</summary>
    string Category { get; }

    /// <summary>
    /// Determines where this module appears in the scene editor.
    /// Plane and Entity modules have dedicated tree sections and
    /// are excluded from the general module picker.
    /// </summary>
    ModuleType Type => ModuleType.Logic;

    /// <summary>
    /// List of target IDs this module is compatible with.
    /// Use ["all"] to indicate compatibility with all targets.
    /// Ex: ["sms", "gg"] or ["snes"] or ["all"]
    /// </summary>
    string[] Compatibility { get; }

    /// <summary>
    /// Defines how many instances of this module can exist.
    /// Global: one per project (shared across scenes)
    /// PerScene: one per scene (different scenes can have their own)
    /// Multiple: unlimited instances
    /// Targets can override this via GetModulePolicyOverrides().
    /// </summary>
    SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;

    /// <summary>
    /// Optional: ID of the visual tool associated with this module.
    /// If set, double-clicking the module or selecting "Edit" from context menu
    /// opens the visual editor window (e.g., "plane_editor", "palette_editor").
    /// The tool must implement IVisualTool and be registered in the ToolRegistry.
    /// Returns null if the module has no visual editor.
    /// </summary>
    string? VisualToolId => null;

    /// <summary>
    /// Default scope for this module type.
    /// Determines whether init() is called in main() OnStart (Project)
    /// or inside scene_X_init() (Scene).
    /// The user can override this per instance in the SceneEditor.
    /// </summary>
    ModuleScope DefaultScope => ModuleScope.Project;

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

/// <summary>
/// Determines where a module appears in the scene editor.
/// Plane and Entity modules have dedicated tree sections and
/// are excluded from the general module picker.
/// </summary>
public enum ModuleType
{
    Logic,    // Physics, Input, Animation, Scroll
    Graphics, // Palette, Fade, Text, HUD
    Audio,    // Sound, Music
    Plane,    // PlaneModule — exclusive to the PLANES section
    Entity    // EntityModule, EnemyModule — exclusive to the ENTITIES section
}

/// <summary>
/// Contract for modules that represent a background plane layer.
/// Modules implementing this interface appear in the PLANES section
/// of the scene tree and open the plane editor on click.
/// </summary>
public interface IPlaneModule : IModule
{
    string AssetId { get; }
    int    Width   { get; }
    int    Height  { get; }
}

/// <summary>
/// Contract for modules that represent a game entity (player, enemy, NPC).
/// Modules implementing this interface appear in the ENTITIES section
/// of the scene tree and contribute to the sprite usage counter.
/// </summary>
public interface IEntityModule : IModule
{
    string SpriteAssetId { get; }
    int    WidthTiles    { get; }
    int    HeightTiles   { get; }
}