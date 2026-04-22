using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates ModuleLoader to provide a unified module system.
/// Handles module discovery and registration.
/// </summary>
public class ModuleRegistry
{
    private readonly string _basePath;
    private readonly ModuleLoader _moduleLoader;
    private readonly Dictionary<string, ModuleOverride> _overrides = new();

    public IReadOnlyDictionary<string, IGraphicModule> GraphicModules => _moduleLoader.GraphicModules;
    public IReadOnlyDictionary<string, ILogicModule> LogicModules => _moduleLoader.LogicModules;
    public IReadOnlyDictionary<string, IAudioModule> AudioModules => _moduleLoader.AudioModules;

    public ModuleRegistry(string basePath)
    {
        _basePath = basePath;
        _moduleLoader = new ModuleLoader(basePath);
    }

    /// <summary>
    /// Checks if a module is singleton for the current target.
    /// Respects target overrides if present, otherwise uses module's default.
    /// </summary>
    public bool IsModuleSingleton(string moduleId)
    {
        // Check override first
        if (_overrides.TryGetValue(moduleId, out var ovr) && ovr.IsSingleton.HasValue)
            return ovr.IsSingleton.Value;

        // Fallback to module's default
        if (GraphicModules.TryGetValue(moduleId, out var gm))
            return gm.IsSingleton;
        if (LogicModules.TryGetValue(moduleId, out var lm))
            return lm.IsSingleton;
        if (AudioModules.TryGetValue(moduleId, out var am))
            return am.IsSingleton;

        return false;
    }

    /// <summary>
    /// Gets the maximum number of instances allowed for a module.
    /// Returns null if unlimited.
    /// </summary>
    public int? GetMaxInstances(string moduleId)
    {
        if (_overrides.TryGetValue(moduleId, out var ovr))
            return ovr.MaxInstances;
        return null;
    }

    /// <summary>
    /// Applies target-specific overrides to modules.
    /// </summary>
    public void ApplyOverrides(IEnumerable<ModuleOverride> overrides)
    {
        _overrides.Clear();
        foreach (var ovr in overrides)
            _overrides[ovr.ModuleId] = ovr;
    }



    /// <summary>
    /// Loads all modules for the given target.
    /// </summary>
    public void LoadForTarget(ITarget target, IProgress<string>? progress = null)
    {
        _moduleLoader.LoadCompatible(target.TargetId, progress);
        ApplyOverrides(target.GetModuleOverrides());
        progress?.Report($"REGISTRY_LOADED: {_moduleLoader.GraphicModules.Count + _moduleLoader.LogicModules.Count + _moduleLoader.AudioModules.Count} modules registered");
    }

    /// <summary>
    /// Registers built-in modules from a target.
    /// </summary>
    public void RegisterBuiltinModules(ITarget target)
    {
        _moduleLoader.RegisterBuiltinModules(target);
    }





    /// <summary>
    /// Manually registers a graphic module.
    /// </summary>
    public void RegisterGraphicModule(IGraphicModule module)
    {
        _moduleLoader.RegisterGraphicModule(module);
    }

    /// <summary>
    /// Manually registers a logic module.
    /// </summary>
    public void RegisterLogicModule(ILogicModule module)
    {
        _moduleLoader.RegisterLogicModule(module);
    }

    /// <summary>
    /// Manually registers an audio module.
    /// </summary>
    public void RegisterAudioModule(IAudioModule module)
    {
        _moduleLoader.RegisterAudioModule(module);
    }


}
