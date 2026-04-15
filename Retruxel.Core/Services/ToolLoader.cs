using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and loads tool plugins at runtime.
/// Tools are discovered from the /tools/ directory and loaded via reflection.
/// Internal tools take precedence over user plugins with the same ToolId.
/// </summary>
public class ToolLoader
{
    private const string InternalToolsFolder = "tools";
    private const string UserPluginsFolder = "plugins";

    private readonly string _basePath;
    private readonly Dictionary<string, ITool> _tools = new();
    private bool _isDiscovered = false;

    public ToolLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Discovers all tools from internal and user plugin folders.
    /// This method is called once during application startup.
    /// </summary>
    public void DiscoverTools(IProgress<string>? progress = null)
    {
        if (_isDiscovered) return;

        var internalPath = Path.Combine(_basePath, InternalToolsFolder);
        var pluginsPath = Path.Combine(_basePath, UserPluginsFolder);

        if (Directory.Exists(internalPath))
            LoadFromPath(internalPath, progress);

        if (Directory.Exists(pluginsPath))
            LoadFromPath(pluginsPath, progress);

        _isDiscovered = true;
        progress?.Report($"TOOL_DISCOVERY_COMPLETE: {_tools.Count} tools loaded");
    }

    /// <summary>
    /// Loads all tool DLLs from a given directory.
    /// </summary>
    private void LoadFromPath(string path, IProgress<string>? progress)
    {
        foreach (var dll in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                progress?.Report($"LOADING_TOOL: {Path.GetFileName(dll)}");
                var assembly = Assembly.LoadFrom(dll);
                RegisterToolsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                progress?.Report($"WARN: Failed to load {Path.GetFileName(dll)} — {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Scans an assembly for types implementing ITool and registers them.
    /// Skips duplicates — first loaded wins.
    /// </summary>
    private void RegisterToolsFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in types)
        {
            try
            {
                var tool = (ITool)Activator.CreateInstance(type)!;

                // Skip duplicates — first loaded wins
                if (!_tools.ContainsKey(tool.ToolId))
                    _tools[tool.ToolId] = tool;
            }
            catch
            {
                // Skip types that cannot be instantiated
            }
        }
    }

    /// <summary>
    /// Gets all discovered tools.
    /// </summary>
    public IReadOnlyDictionary<string, ITool> GetTools()
    {
        if (!_isDiscovered)
            DiscoverTools();

        return _tools;
    }

    /// <summary>
    /// Gets tools filtered by category.
    /// </summary>
    public IEnumerable<ITool> GetToolsByCategory(string category)
    {
        return GetTools().Values
            .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a tool by its unique ID.
    /// </summary>
    public ITool? GetToolById(string toolId)
    {
        GetTools().TryGetValue(toolId, out var tool);
        return tool;
    }

    /// <summary>
    /// Gets all unique categories from discovered tools.
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        return GetTools().Values
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <summary>
    /// Gets tools that are available for the specified target.
    /// Returns all universal tools and target-specific tools for the given target.
    /// </summary>
    public IEnumerable<ITool> GetToolsForTarget(string? targetId)
    {
        return GetTools().Values
            .Where(t => t.TargetId == null || t.TargetId == targetId);
    }

    /// <summary>
    /// Manually registers a tool.
    /// Used during development before auto-discovery is fully implemented.
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        if (!_tools.ContainsKey(tool.ToolId))
            _tools[tool.ToolId] = tool;
    }
}
