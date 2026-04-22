// Retruxel SDK — Public API for plugin developers.
//
// Reference ONLY this project in your plugin DLL.
// Never reference Retruxel.Core directly — internal APIs may change without notice.
//
// Uses TypeForwardedTo to redirect type references from SDK to Core at runtime.
// Plugins compile against SDK but CLR resolves types from Core assembly.

using System.Runtime.CompilerServices;

// ── Interfaces ────────────────────────────────────────────────────────────────

[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IModule))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ILogicModule))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IGraphicModule))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IAudioModule))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ITarget))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ITool))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IToolExtension))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ICodeGenPlugin))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IToolchain))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IUndoableCommand))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ILocalizationService))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IEmulatorConnection))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IPaletteProvider))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Services.ServiceLocator))]

// ── Enums ─────────────────────────────────────────────────────────────────────

[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ModuleType))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.ParameterType))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.GeneratedFileType))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.GeneratedAssetType))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.BuildLogLevel))]

// ── Models ────────────────────────────────────────────────────────────────────

[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.GeneratedFile))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.GeneratedAsset))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.HardwareColor))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.ModuleManifest))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.ParameterDefinition))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.TargetSpecs))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.RomBank))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.ProjectTemplate))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.BuildContext))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.BuildResult))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.BuildLogEntry))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.RetruxelProject))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.AssetEntry))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Models.SceneData))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.EmulatorState))]
