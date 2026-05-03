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
    private readonly Dictionary<string, SingletonPolicy> _policyOverrides = new();

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
    /// Returns true if policy is Global or PerScene.
    /// </summary>
    public bool IsModuleSingleton(string moduleId)
    {
        var policy = GetModulePolicy(moduleId);
        return policy != SingletonPolicy.Multiple;
    }

    /// <summary>
    /// Gets the effective singleton policy for a module.
    /// Respects target overrides if present, otherwise uses module's default.
    /// </summary>
    public SingletonPolicy GetModulePolicy(string moduleId)
    {
        // Check override first
        if (_policyOverrides.TryGetValue(moduleId, out var policy))
            return policy;

        // Fallback to module's default
        if (GraphicModules.TryGetValue(moduleId, out var gm))
            return gm.SingletonPolicy;
        if (LogicModules.TryGetValue(moduleId, out var lm))
            return lm.SingletonPolicy;
        if (AudioModules.TryGetValue(moduleId, out var am))
            return am.SingletonPolicy;

        return SingletonPolicy.Multiple;
    }

    /// <summary>
    /// Applies target-specific policy overrides to modules.
    /// </summary>
    public void ApplyPolicyOverrides(Dictionary<string, SingletonPolicy> overrides)
    {
        _policyOverrides.Clear();
        foreach (var (moduleId, policy) in overrides)
            _policyOverrides[moduleId] = policy;
    }



    /// <summary>
    /// Loads all modules for the given target.
    /// </summary>
    public void LoadForTarget(ITarget target, IProgress<string>? progress = null)
    {
        _moduleLoader.LoadCompatible(target.TargetId, progress);
        ApplyPolicyOverrides(target.GetModulePolicyOverrides());
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
