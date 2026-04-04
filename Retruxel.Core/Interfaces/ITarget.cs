using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Contract for a target console platform.
/// Each supported console implements this interface.
/// The target is selected once at project creation and never changes.
/// </summary>
public interface ITarget
{
    /// <summary>Unique target identifier. Ex: "sms", "nes", "snes"</summary>
    string TargetId { get; }

    /// <summary>Display name shown in the UI. Ex: "Sega Master System"</summary>
    string DisplayName { get; }

    /// <summary>Short description shown in the target selection screen.</summary>
    string Description { get; }

    /// <summary>Hardware specifications of this target.</summary>
    TargetSpecs Specs { get; }

    /// <summary>
    /// Returns the toolchain responsible for compiling projects for this target.
    /// </summary>
    IToolchain GetToolchain();

    /// <summary>
    /// Returns all built-in modules bundled with this target.
    /// These are loaded automatically when a project is created for this target.
    /// </summary>
    IEnumerable<IModule> GetBuiltinModules();

    /// <summary>
    /// Returns the project templates available for this target.
    /// Shown in the New Project wizard.
    /// </summary>
    IEnumerable<ProjectTemplate> GetTemplates();

    /// <summary>
    /// Returns the target-specific settings definitions.
    /// The shell auto-generates the Target Settings UI from these.
    /// </summary>
    IEnumerable<ParameterDefinition> GetSettingsDefinitions();

    /// <summary>
    /// Returns generated files for a given module.
    /// Each target translates universal module data into target-specific C code.
    /// Returns empty if the target has no translator for that module.
    /// </summary>
    IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module);

    /// <summary>
    /// Generates the main entry point file for this target.
    /// The host shell calls this after all module files are generated.
    /// Each target knows its own required headers, init calls and ROM metadata.
    /// </summary>
    GeneratedFile GenerateMainFile(RetruxelProject project, IEnumerable<GeneratedFile> moduleFiles);
}