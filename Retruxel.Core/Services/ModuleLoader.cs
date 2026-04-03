using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and loads module DLLs from the internal modules folder
/// and the user plugins folder.
/// Internal modules take precedence over user plugins with the same ModuleId.
/// </summary>
public class ModuleLoader
{
    private const string InternalModulesFolder = "modules";
    private const string UserPluginsFolder = "plugins";

    private readonly string _basePath;

    /// <summary>
    /// All loaded graphic modules indexed by ModuleId.
    /// </summary>
    public IReadOnlyDictionary<string, IGraphicModule> GraphicModules => _graphicModules;

    /// <summary>
    /// All loaded logic modules indexed by ModuleId.
    /// </summary>
    public IReadOnlyDictionary<string, ILogicModule> LogicModules => _logicModules;

    /// <summary>
    /// All loaded audio modules indexed by ModuleId.
    /// </summary>
    public IReadOnlyDictionary<string, IAudioModule> AudioModules => _audioModules;

    private readonly Dictionary<string, IGraphicModule> _graphicModules = new();
    private readonly Dictionary<string, ILogicModule> _logicModules = new();
    private readonly Dictionary<string, IAudioModule> _audioModules = new();

    public ModuleLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Loads all modules from internal and user plugin folders.
    /// Reports progress for display in the splash screen loading log.
    /// </summary>
    public void LoadAll(IProgress<string>? progress = null)
    {
        var internalPath = Path.Combine(_basePath, InternalModulesFolder);
        var pluginsPath = Path.Combine(_basePath, UserPluginsFolder);

        if (Directory.Exists(internalPath))
            LoadFromPath(internalPath, progress);

        if (Directory.Exists(pluginsPath))
            LoadFromPath(pluginsPath, progress);
    }

    /// <summary>
    /// Loads all module DLLs from a given directory.
    /// </summary>
    private void LoadFromPath(string path, IProgress<string>? progress)
    {
        foreach (var dll in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                progress?.Report($"LOADING_MODULE: {Path.GetFileName(dll)}");
                var assembly = Assembly.LoadFrom(dll);
                RegisterModulesFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                progress?.Report($"WARN: Failed to load {Path.GetFileName(dll)} — {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Scans an assembly for types implementing IGraphicModule, ILogicModule or IAudioModule
    /// and registers them. Skips duplicates — first loaded wins.
    /// </summary>
    private void RegisterModulesFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            try
            {
                if (typeof(IGraphicModule).IsAssignableFrom(type))
                {
                    var module = (IGraphicModule)Activator.CreateInstance(type)!;
                    if (!_graphicModules.ContainsKey(module.ModuleId))
                        _graphicModules[module.ModuleId] = module;
                }
                else if (typeof(IAudioModule).IsAssignableFrom(type))
                {
                    var module = (IAudioModule)Activator.CreateInstance(type)!;
                    if (!_audioModules.ContainsKey(module.ModuleId))
                        _audioModules[module.ModuleId] = module;
                }
                else if (typeof(ILogicModule).IsAssignableFrom(type))
                {
                    var module = (ILogicModule)Activator.CreateInstance(type)!;
                    if (!_logicModules.ContainsKey(module.ModuleId))
                        _logicModules[module.ModuleId] = module;
                }
            }
            catch
            {
                // Skip types that cannot be instantiated
            }
        }
    }
}