using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Registry for tools (ITool and IVisualTool).
/// Discovers tools from plugin assemblies and provides lookup by ToolId.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();
    private readonly Dictionary<string, IVisualTool> _visualTools = new();

    public IReadOnlyDictionary<string, ITool> Tools => _tools;
    public IReadOnlyDictionary<string, IVisualTool> VisualTools => _visualTools;

    /// <summary>
    /// Discovers and registers all tools from plugin assemblies in the given directory.
    /// </summary>
    public void DiscoverTools(string pluginsPath, IProgress<string>? progress = null)
    {
        if (!Directory.Exists(pluginsPath))
        {
            progress?.Report($"TOOLS_DISCOVERY: Plugins directory not found: {pluginsPath}");
            return;
        }

        var toolsDir = Path.Combine(pluginsPath, "Tools");
        if (!Directory.Exists(toolsDir))
        {
            progress?.Report($"TOOLS_DISCOVERY: Tools directory not found: {toolsDir}");
            return;
        }

        var dllFiles = Directory.GetFiles(toolsDir, "*.dll", SearchOption.AllDirectories);
        progress?.Report($"TOOLS_DISCOVERY: Found {dllFiles.Length} DLL files in Tools directory");

        // Pre-load Retruxel.SDK.dll if present (dependency for most tools)
        var sdkDll = dllFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("Retruxel.SDK.dll", StringComparison.OrdinalIgnoreCase));
        if (sdkDll != null)
        {
            try
            {
                Assembly.LoadFrom(sdkDll);
                progress?.Report($"TOOLS_DISCOVERY: Pre-loaded Retruxel.SDK.dll");
            }
            catch { /* ignore */ }
        }

        foreach (var dllPath in dllFiles)
        {
            try
            {
                // Skip native libraries and dependencies
                var fileName = Path.GetFileName(dllPath);
                if (fileName.StartsWith("System.") || 
                    fileName.StartsWith("Microsoft.") ||
                    fileName.StartsWith("Windows.") ||
                    fileName.Equals("Retruxel.SDK.dll", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("Retruxel.Core.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                var assembly = Assembly.LoadFrom(dllPath);
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract);

                foreach (var type in types)
                {
                    // Check for IVisualTool first (it extends ITool)
                    if (typeof(IVisualTool).IsAssignableFrom(type))
                    {
                        var instance = (IVisualTool)Activator.CreateInstance(type)!;
                        _visualTools[instance.ToolId] = instance;
                        _tools[instance.ToolId] = instance;
                        progress?.Report($"TOOLS_DISCOVERY: Registered visual tool: {instance.ToolId}");
                    }
                    else if (typeof(ITool).IsAssignableFrom(type))
                    {
                        var instance = (ITool)Activator.CreateInstance(type)!;
                        _tools[instance.ToolId] = instance;
                        progress?.Report($"TOOLS_DISCOVERY: Registered tool: {instance.ToolId}");
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"TOOLS_DISCOVERY_ERROR: Failed to load {Path.GetFileName(dllPath)}: {ex.Message}");
            }
        }

        progress?.Report($"TOOLS_DISCOVERY: Registered {_tools.Count} tools ({_visualTools.Count} visual)");
    }

    /// <summary>
    /// Gets a tool by ID.
    /// </summary>
    public ITool? GetTool(string toolId)
    {
        return _tools.TryGetValue(toolId, out var tool) ? tool : null;
    }

    /// <summary>
    /// Gets a visual tool by ID.
    /// </summary>
    public IVisualTool? GetVisualTool(string toolId)
    {
        return _visualTools.TryGetValue(toolId, out var tool) ? tool : null;
    }

    /// <summary>
    /// Manually registers a tool instance.
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.ToolId] = tool;
        if (tool is IVisualTool visualTool)
            _visualTools[tool.ToolId] = visualTool;
    }
}
