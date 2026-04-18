using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and loads code generation plugins at runtime.
/// Codegens are discovered from /codegens/ and /plugins/codegens/ directories.
/// </summary>
public class CodeGenLoader
{
    private const string InternalCodeGensFolder = "codegens";
    private const string UserPluginsFolder = "plugins/codegens";

    private readonly string _basePath;
    private readonly Dictionary<string, ICodeGenPlugin> _codegens = new();
    private bool _isDiscovered = false;

    public CodeGenLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Discovers all codegens from internal and user plugin folders.
    /// </summary>
    public void DiscoverCodeGens(IProgress<string>? progress = null)
    {
        if (_isDiscovered) return;

        var internalPath = Path.Combine(_basePath, InternalCodeGensFolder);
        var pluginsPath = Path.Combine(_basePath, UserPluginsFolder);

        if (Directory.Exists(internalPath))
            LoadFromPath(internalPath, progress);

        if (Directory.Exists(pluginsPath))
            LoadFromPath(pluginsPath, progress);

        _isDiscovered = true;
        progress?.Report($"CODEGEN_DISCOVERY_COMPLETE: {_codegens.Count} codegens loaded");
    }

    /// <summary>
    /// Loads all codegen DLLs from a given directory.
    /// </summary>
    private void LoadFromPath(string path, IProgress<string>? progress)
    {
        if (!Directory.Exists(path)) return;

        foreach (var dll in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                progress?.Report($"LOADING_CODEGEN: {Path.GetFileName(dll)}");
                var assembly = Assembly.LoadFrom(dll);
                RegisterCodeGensFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                progress?.Report($"WARN: Failed to load {Path.GetFileName(dll)} — {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Scans an assembly for types implementing ICodeGenPlugin and registers them.
    /// Key format: "{moduleId}:{targetId}" or ":{targetId}" for target-specific modules.
    /// </summary>
    private void RegisterCodeGensFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => typeof(ICodeGenPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in types)
        {
            try
            {
                var codegen = (ICodeGenPlugin)Activator.CreateInstance(type)!;
                var key = $"{codegen.ModuleId ?? string.Empty}:{codegen.TargetId}";

                // Skip duplicates — first loaded wins
                if (!_codegens.ContainsKey(key))
                    _codegens[key] = codegen;
            }
            catch
            {
                // Skip types that cannot be instantiated
            }
        }
    }

    /// <summary>
    /// Gets a codegen for a specific module and target combination.
    /// </summary>
    public ICodeGenPlugin? GetCodeGen(string moduleId, string targetId)
    {
        if (!_isDiscovered)
            DiscoverCodeGens();

        var key = $"{moduleId}:{targetId}";
        return _codegens.TryGetValue(key, out var codegen) ? codegen : null;
    }

    /// <summary>
    /// Gets all codegens for a specific target (including target-specific modules).
    /// </summary>
    public IEnumerable<ICodeGenPlugin> GetCodeGensForTarget(string targetId)
    {
        if (!_isDiscovered)
            DiscoverCodeGens();

        return _codegens.Values.Where(c => c.TargetId == targetId);
    }

    /// <summary>
    /// Gets all target-specific modules (codegens with ModuleId == null).
    /// </summary>
    public IEnumerable<ICodeGenPlugin> GetTargetSpecificModules(string targetId)
    {
        return GetCodeGensForTarget(targetId).Where(c => c.ModuleId == null);
    }

    /// <summary>
    /// Manually registers a codegen.
    /// </summary>
    public void RegisterCodeGen(ICodeGenPlugin codegen)
    {
        var key = $"{codegen.ModuleId ?? string.Empty}:{codegen.TargetId}";
        if (!_codegens.ContainsKey(key))
            _codegens[key] = codegen;
    }

    /// <summary>
    /// Gets all discovered codegens.
    /// </summary>
    public IReadOnlyDictionary<string, ICodeGenPlugin> GetAll()
    {
        if (!_isDiscovered)
            DiscoverCodeGens();

        return _codegens;
    }
}
