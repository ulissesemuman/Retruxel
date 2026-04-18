using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates ModuleLoader and CodeGenLoader to provide a unified module system.
/// Handles compatibility injection, target-specific module creation, and registry management.
/// </summary>
public class ModuleRegistry
{
    private const string ModulesRegistryFile = "Assets/Data/modules.json";

    private readonly string _basePath;
    private readonly ModuleLoader _moduleLoader;
    private readonly CodeGenLoader _codeGenLoader;
    private ModulesRegistry? _registry;

    public IReadOnlyDictionary<string, IGraphicModule> GraphicModules => _moduleLoader.GraphicModules;
    public IReadOnlyDictionary<string, ILogicModule> LogicModules => _moduleLoader.LogicModules;
    public IReadOnlyDictionary<string, IAudioModule> AudioModules => _moduleLoader.AudioModules;

    public ModuleRegistry(string basePath)
    {
        _basePath = basePath;
        _moduleLoader = new ModuleLoader(basePath);
        _codeGenLoader = new CodeGenLoader(basePath);
    }

    /// <summary>
    /// Loads modules.json registry.
    /// </summary>
    private void LoadRegistry(IProgress<string>? progress = null)
    {
        if (_registry != null) return;

        var registryPath = Path.Combine(_basePath, ModulesRegistryFile);
        if (!File.Exists(registryPath))
        {
            progress?.Report($"WARN: {ModulesRegistryFile} not found, using empty registry");
            _registry = new ModulesRegistry();
            return;
        }

        try
        {
            var json = File.ReadAllText(registryPath);
            _registry = JsonSerializer.Deserialize<ModulesRegistry>(json) ?? new ModulesRegistry();
            progress?.Report($"REGISTRY_LOADED: {_registry.Modules.Count} modules registered");
        }
        catch (Exception ex)
        {
            progress?.Report($"ERROR: Failed to load {ModulesRegistryFile} — {ex.Message}");
            _registry = new ModulesRegistry();
        }
    }

    /// <summary>
    /// Loads all modules and codegens for the given target, injecting compatibility dynamically.
    /// </summary>
    public void LoadForTarget(string targetId, IProgress<string>? progress = null)
    {
        LoadRegistry(progress);
        _codeGenLoader.DiscoverCodeGens(progress);
        _moduleLoader.LoadCompatible(targetId, progress);

        InjectCompatibilityForAll(progress);
        CreateTargetModules(targetId, progress);
    }

    /// <summary>
    /// Registers built-in modules from a target.
    /// </summary>
    public void RegisterBuiltinModules(ITarget target)
    {
        _moduleLoader.RegisterBuiltinModules(target);
    }

    /// <summary>
    /// Injects Compatibility[] into all loaded modules based on registry and discovered codegens.
    /// </summary>
    private void InjectCompatibilityForAll(IProgress<string>? progress)
    {
        foreach (var module in _moduleLoader.GraphicModules.Values)
            InjectCompatibility(module, progress);

        foreach (var module in _moduleLoader.LogicModules.Values)
            InjectCompatibility(module, progress);

        foreach (var module in _moduleLoader.AudioModules.Values)
            InjectCompatibility(module, progress);
    }

    /// <summary>
    /// Injects Compatibility[] into a module based on:
    /// 1. modules.json registry (if present)
    /// 2. Available CodeGens discovered by CodeGenLoader
    /// 3. Fallback to module's hardcoded Compatibility if neither exists
    /// </summary>
    private void InjectCompatibility(IModule module, IProgress<string>? progress)
    {
        if (_registry?.Modules.TryGetValue(module.ModuleId, out var entry) == true)
        {
            var targets = entry.CodeGens.Keys.ToArray();
            var compatibilityProp = module.GetType().GetProperty("Compatibility");
            if (compatibilityProp?.CanWrite == true)
            {
                compatibilityProp.SetValue(module, targets);
                progress?.Report($"COMPATIBILITY_INJECTED: {module.ModuleId} → [{string.Join(", ", targets)}]");
                return;
            }
        }

        var discoveredTargets = _codeGenLoader.GetAll()
            .Where(kvp => kvp.Value.ModuleId == module.ModuleId)
            .Select(kvp => kvp.Value.TargetId)
            .Distinct()
            .ToArray();

        if (discoveredTargets.Length > 0)
        {
            var compatibilityProp = module.GetType().GetProperty("Compatibility");
            if (compatibilityProp?.CanWrite == true)
            {
                compatibilityProp.SetValue(module, discoveredTargets);
                progress?.Report($"COMPATIBILITY_DISCOVERED: {module.ModuleId} → [{string.Join(", ", discoveredTargets)}]");
                return;
            }
        }

        progress?.Report($"COMPATIBILITY_HARDCODED: {module.ModuleId} → [{string.Join(", ", module.Compatibility)}]");
    }

    /// <summary>
    /// Creates TargetModule instances for codegens that have ModuleId == null.
    /// These are target-specific modules without a universal parent.
    /// </summary>
    private void CreateTargetModules(string targetId, IProgress<string>? progress)
    {
        var targetSpecificCodeGens = _codeGenLoader.GetTargetSpecificModules(targetId);

        foreach (var codegen in targetSpecificCodeGens)
        {
            var moduleId = $"{targetId}.{codegen.DisplayName.ToLowerInvariant().Replace(" ", "")}";

            if (_moduleLoader.LogicModules.ContainsKey(moduleId))
                continue;

            var targetModule = new TargetModule
            {
                ModuleId = moduleId,
                DisplayName = codegen.DisplayName,
                Category = codegen.Category,
                IsSingleton = codegen.IsSingleton,
                Compatibility = new[] { targetId }
            };

            _moduleLoader.RegisterLogicModule(targetModule);
            progress?.Report($"TARGET_MODULE_CREATED: {moduleId} for {targetId}");
        }
    }

    /// <summary>
    /// Gets the CodeGen for a specific module and target.
    /// </summary>
    public ICodeGenPlugin? GetCodeGen(string moduleId, string targetId)
    {
        return _codeGenLoader.GetCodeGen(moduleId, targetId);
    }

    /// <summary>
    /// Gets all CodeGens for a specific target.
    /// </summary>
    public IEnumerable<ICodeGenPlugin> GetCodeGensForTarget(string targetId)
    {
        return _codeGenLoader.GetCodeGensForTarget(targetId);
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

    /// <summary>
    /// Resets code generation state for all discovered codegens.
    /// Should be called before each build to ensure clean state.
    /// </summary>
    public void ResetCodeGenState()
    {
        // Reset static counters in codegens via reflection
        foreach (var codegen in _codeGenLoader.GetAll().Values)
        {
            var resetMethod = codegen.GetType().GetMethod("ResetCounter", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            resetMethod?.Invoke(null, null);
        }
    }
}
