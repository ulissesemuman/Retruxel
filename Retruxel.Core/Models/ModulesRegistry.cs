namespace Retruxel.Core.Models;

/// <summary>
/// Root structure for modules.json registry.
/// </summary>
public class ModulesRegistry
{
    public Dictionary<string, ModuleRegistryEntry> Modules { get; set; } = new();
}

/// <summary>
/// Entry for a single module in the registry.
/// </summary>
public class ModuleRegistryEntry
{
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = "Logic"; // Logic, Graphic, Audio
    public bool IsSingleton { get; set; } = false;
    public Dictionary<string, string> CodeGens { get; set; } = new();
}
