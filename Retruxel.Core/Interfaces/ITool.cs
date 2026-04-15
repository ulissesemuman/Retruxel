using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Interface for Retruxel tools (plugins that provide utility functionality).
/// Tools are discovered at runtime from the /tools/ directory.
/// </summary>
public interface ITool
{
    /// <summary>Unique identifier for the tool (e.g., "retruxel.tool.assetimporter")</summary>
    string ToolId { get; }

    /// <summary>Display name shown in the UI (e.g., "Asset Importer")</summary>
    string DisplayName { get; }

    /// <summary>Short description of what the tool does</summary>
    string Description { get; }

    /// <summary>Icon path or resource key</summary>
    string IconPath { get; }

    /// <summary>Category for grouping tools in the menu (e.g., "Import", "Export", "Utilities")</summary>
    string Category { get; }

    /// <summary>Menu path where the tool appears (e.g., "Tools/Import/Asset Importer")</summary>
    string MenuPath { get; }

    /// <summary>Keyboard shortcut (optional, e.g., "Ctrl+Shift+A")</summary>
    string? Shortcut { get; }

    /// <summary>Whether the tool requires an open project to run</summary>
    bool RequiresProject { get; }

    /// <summary>Target-specific tool? If set, only shows when that target is active</summary>
    string? TargetId { get; }

    /// <summary>Execute the tool. Returns true if successful.</summary>
    /// <param name="context">Context with project info, selected target, etc.</param>
    bool Execute(IToolContext context);
}

/// <summary>
/// Context provided to tools when they are executed.
/// </summary>
public interface IToolContext
{
    /// <summary>Current project (null if no project open)</summary>
    RetruxelProject? CurrentProject { get; }

    /// <summary>Active target (null if no project open)</summary>
    ITarget? ActiveTarget { get; }

    /// <summary>Service provider for accessing core services</summary>
    IServiceProvider Services { get; }
}
