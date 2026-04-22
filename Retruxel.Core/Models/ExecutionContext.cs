namespace Retruxel.Core.Models;

/// <summary>
/// Defines the execution context of a tool
/// </summary>
public enum ExecutionContext
{
    /// <summary>
    /// Tool opened outside of a project (exports to file)
    /// </summary>
    Standalone,

    /// <summary>
    /// Tool opened within a project (creates module in scene)
    /// </summary>
    InProject,

    /// <summary>
    /// Tool executed without UI (used by other tools)
    /// </summary>
    Headless,

    /// <summary>
    /// Tool in preview mode (doesn't save anything)
    /// </summary>
    Preview
}
