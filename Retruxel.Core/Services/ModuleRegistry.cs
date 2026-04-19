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

    public IReadOnlyDictionary<string, IGraphicModule> GraphicModules => _moduleLoader.GraphicModules;
    public IReadOnlyDictionary<string, ILogicModule> LogicModules => _moduleLoader.LogicModules;
    public IReadOnlyDictionary<string, IAudioModule> AudioModules => _moduleLoader.AudioModules;

    public ModuleRegistry(string basePath)
    {
        _basePath = basePath;
        _moduleLoader = new ModuleLoader(basePath);
    }



    /// <summary>
    /// Loads all modules for the given target.
    /// </summary>
    public void LoadForTarget(string targetId, IProgress<string>? progress = null)
    {
        _moduleLoader.LoadCompatible(targetId, progress);
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
