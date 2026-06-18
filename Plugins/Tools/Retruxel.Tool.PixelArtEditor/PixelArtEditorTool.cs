using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using Retruxel.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Retruxel.Tool.PixelArtEditor;

/// <summary>
/// Integrated pixel art editor for creating and editing sprites and tiles.
/// </summary>
public class PixelArtEditorTool : ITool
{
    public string ToolId => "retruxel.tool.pixelarteditor";
    public string DisplayName => "Pixel Art Editor";
    public string Description => "Create and edit sprites and tiles with an integrated pixel art editor";
    public object? Icon => null;
    public string Category => "Graphics";
    public string? Shortcut => "Ctrl+Shift+P";

    // This editor works inside an active project (palette/assets/targets).
    public bool RequiresProject => true;

    public string? TargetId => null;
    public bool IsStandalone => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Open WPF window.
        // Retruxel's window invoker patterns live in the UI layer; for now we keep it direct.
        var window = new PixelArtEditorWindow();

        // Minimal defaults. Later we will wire from input/context (project assets, palette, tileset, etc.).
        window.Owner = Application.Current?.MainWindow;
        var result = window.ShowDialog();

        return new Dictionary<string, object>
        {
            ["ok"] = result == true
        };
    }
}

