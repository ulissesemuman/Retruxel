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
    /// Returns the complete list of colors this hardware can physically display.
    ///
    /// Each target implements this as appropriate:
    ///   - Calculable palettes (SMS, SNES): generated mathematically from ColorDepthBitsPerChannel
    ///   - Fixed palettes (NES): returned as a hardcoded list of the PPU's actual output colors
    ///
    /// Used by:
    ///   - The color reduction tool (maps any image to the nearest hardware colors)
    ///   - The palette editor (shows only valid colors for this target)
    ///   - The tile editor (validates color choices against hardware limits)
    /// </summary>
    IReadOnlyList<HardwareColor> GetHardwarePalette();

    /// <summary>
    /// Returns the toolchain responsible for compiling projects for this target.
    /// </summary>
    IToolchain GetToolchain();

    /// <summary>
    /// Returns a custom toolchain builder for this target, or null to use auto-discovered builder.
    /// Override this when the target needs specialized build logic that differs from standard builders.
    /// If null, ToolchainOrchestrator will search for a builder with matching TargetId in Plugins/Toolchains/.
    /// </summary>
    object? GetCustomToolchainBuilder() => null;

    /// <summary>
    /// Returns the list of required toolchain binary paths for verification.
    /// Used by StartupService to check if toolchain is extracted.
    /// Paths are relative to %AppData%/Retruxel/toolchain/
    /// Example: ["compilers/sdcc/bin/sdcc.exe", "utils/sega/bin/ihx2sms.exe"]
    /// </summary>
    IEnumerable<string> GetRequiredToolchainBinaries();

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
    /// Returns target-specific overrides for universal modules.
    /// Allows targets to modify module behavior (singleton, max instances, etc.)
    /// without hardcoding target knowledge in the core.
    /// </summary>
    IEnumerable<ModuleOverride> GetModuleOverrides();

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

    /// <summary>
    /// Generates system files (splash screens, boot code, etc.) for this target.
    /// Called before GenerateMainFile to inject additional files into the build.
    /// </summary>
    IEnumerable<GeneratedFile> GenerateSystemFiles();

    /// <summary>
    /// Analyzes the build output and returns hardware usage diagnostics.
    /// Called by the Core after code generation, before compilation.
    /// The target inspects SourceFiles to count tiles, sprites, map entries, etc.
    /// Returns null if this target does not support diagnostics.
    /// </summary>
    BuildDiagnosticsReport? GetBuildDiagnostics(BuildDiagnosticInput input) => null;
}