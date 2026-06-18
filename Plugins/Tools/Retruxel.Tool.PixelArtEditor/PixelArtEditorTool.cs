using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Retruxel.Tool.PixelArtEditor;

/// <summary>
/// Integrated pixel art editor for creating and editing sprites and tiles.
/// Accepts optional input keys: "assetId", "projectRef", "targetRef", "sceneRef".
/// </summary>
public class PixelArtEditorTool : ITool
{
    public string ToolId      => "retruxel.tool.pixelarteditor";
    public string DisplayName => "Pixel Art Editor";
    public string Description => "Edit sprites and tiles pixel by pixel with hardware-limited palette.";
    public object? Icon       => null;
    public string Category    => "Graphics";
    public string? Shortcut   => "Ctrl+Shift+P";
    public bool RequiresProject => true;
    public string? TargetId   => null;
    public bool IsStandalone  => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var target  = input.TryGetValue("targetRef",  out var t) ? t as ITarget        : null;
        var project = input.TryGetValue("projectRef", out var p) ? p as RetruxelProject : null;
        var scene   = input.TryGetValue("sceneRef",   out var s) ? s as SceneData       : null;
        var assetId = input.TryGetValue("assetId",    out var a) ? a as string          : null;

        PixelArtEditorWindow window;

        if (target != null && project != null)
            window = new PixelArtEditorWindow(target, project, scene, assetId);
        else
            window = new PixelArtEditorWindow();

        window.Owner = Application.Current?.MainWindow;
        bool ok = window.ShowDialog() == true;

        var result = new Dictionary<string, object> { ["ok"] = ok };
        if (ok && window.SavedAsset != null)
            result["savedAsset"] = window.SavedAsset;

        return result;
    }
}
