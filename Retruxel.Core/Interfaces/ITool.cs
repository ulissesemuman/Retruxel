using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Interface for Retruxel tools (plugins that provide utility functionality).
/// Tools are discovered at runtime and can be invoked programmatically or from UI.
/// </summary>
public interface ITool
{
    /// <summary>Unique identifier for the tool (e.g., "retruxel.tool.pngtotiles.sms")</summary>
    string ToolId { get; }

    /// <summary>Display name shown in the UI (e.g., "PNG to Tiles (SMS)")</summary>
    string DisplayName { get; }

    /// <summary>Short description of what the tool does</summary>
    string Description { get; }

    /// <summary>Icon image embedded in the tool</summary>
    object? Icon { get; }

    /// <summary>Category for grouping tools (e.g., "ImageProcessing", "Audio", "Utilities")</summary>
    string Category { get; }

    /// <summary>Keyboard shortcut (optional, e.g., "Ctrl+Shift+A")</summary>
    string? Shortcut { get; }

    /// <summary>Whether the tool can run standalone without module context</summary>
    bool IsStandalone { get; }

    /// <summary>Target-specific tool? If set, only available when that target is active</summary>
    string? TargetId { get; }

    /// <summary>Whether the tool requires an open project to run</summary>
    bool RequiresProject { get; }

    /// <summary>
    /// Optional target-specific extension ID.
    /// If set, ModuleRenderer will look for an IToolExtension in the target assembly
    /// with matching ToolId and merge its output with this tool's output.
    /// Null = tool is fully generic with no target-specific extension.
    /// </summary>
    string? TargetExtensionId => null;

    /// <summary>
    /// Execute the tool with input parameters.
    /// Returns output data as dictionary.
    /// </summary>
    Dictionary<string, object> Execute(Dictionary<string, object> input);
}
