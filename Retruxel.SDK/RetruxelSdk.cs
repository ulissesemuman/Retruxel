// Retruxel SDK — Public API for plugin developers.
// Reference this project in your plugin DLL instead of Retruxel.Core directly.
// This ensures a stable public contract independent of internal Core changes.
//
// The SDK references Retruxel.Core internally and exposes its public interfaces.
// Plugins automatically get access to:
// - Retruxel.Core.Interfaces (ITool, ITarget, IModule, etc.)
// - Retruxel.Core.Models (public models)
//
// No additional using statements needed - interfaces are available via transitive reference.

