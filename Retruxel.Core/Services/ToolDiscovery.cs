using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers ITool implementations from plugin DLLs.
/// </summary>
internal static class ToolDiscovery
{
    public static Dictionary<string, ITool> DiscoverTools(string pluginsPath)
    {
        var result = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);

        var toolsDir = Path.Combine(pluginsPath, "Tools");

        if (!Directory.Exists(toolsDir))
            return result;

        foreach (var dllPath in Directory.GetFiles(toolsDir, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                var dllName = Path.GetFileNameWithoutExtension(dllPath);

                foreach (var type in asm.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract || !typeof(ITool).IsAssignableFrom(type))
                        continue;

                    if (Activator.CreateInstance(type) is ITool tool)
                    {
                        result[tool.ToolId] = tool;
                        result[dllName] = tool;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("BadImageFormatException"))
                    Console.WriteLine($"[ToolDiscovery] WARN: Failed to load tool from {dllPath}: {ex.Message}");
            }
        }

        return result;
    }
}
