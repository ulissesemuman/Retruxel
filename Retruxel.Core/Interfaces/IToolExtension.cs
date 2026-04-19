namespace Retruxel.Core.Interfaces;

/// <summary>
/// Target-specific extension for a generic tool.
/// Implemented inside the target assembly (ex: Retruxel.Target.SMS.dll).
/// Discovered at runtime by ModuleRenderer via reflection.
///
/// Naming convention (enforced by reflection lookup):
///   Class name must be: {TargetPrefix}{ToolId_PascalCase}Extension
///   Ex: SmsTilemapPreprocessorExtension, NesPngToTilesExtension
///
/// The ModuleRenderer calls Execute() after the generic tool's Execute(),
/// and merges the results — extension keys overwrite generic keys on conflict.
/// </summary>
public interface IToolExtension
{
    /// <summary>
    /// Must match ITool.TargetExtensionId of the tool this extends.
    /// </summary>
    string ToolId { get; }

    /// <summary>
    /// Executes the target-specific logic.
    /// Receives the same input as the generic tool.
    /// Returns additional or overriding keys to merge into the variable dict.
    /// </summary>
    Dictionary<string, object> Execute(Dictionary<string, object> input);
}
