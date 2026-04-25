using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Tool.PaletteEditor;

/// <summary>
/// Palette Editor Tool - Visual editor for palette modules.
/// Implements IVisualTool to integrate with the module system.
/// </summary>
public class PaletteEditorTool : IVisualTool
{
    public string ToolId => "palette_editor";
    public string DisplayName => "Palette Editor";
    public string Description => "Visual editor for creating and editing color palettes";
    public object? Icon => null;
    public string Category => "Visual Editors";
    public string? Shortcut => null;
    public bool IsStandalone => true;
    public string? TargetId => null;
    public bool RequiresProject => false;
    public bool HasUI => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Visual tools don't use Execute() - they use CreateWindow()
        return new Dictionary<string, object>();
    }

    public object CreateWindow(Dictionary<string, object> input)
    {
        var target = (ITarget)input["target"];
        var project = (RetruxelProject)input["project"];
        var toolRegistry = input.ContainsKey("toolRegistry") ? (Core.Services.ToolRegistry)input["toolRegistry"] : null;

        // Get palette provider extension from target
        if (toolRegistry == null)
        {
            throw new InvalidOperationException("ToolRegistry not available.");
        }

        var extension = toolRegistry.GetTool($"palette_editor_ext_{target.TargetId}");
        if (extension == null)
        {
            throw new InvalidOperationException(
                $"Target '{target.DisplayName}' does not support Palette Editor yet.");
        }

        var extensionResult = extension.Execute(new Dictionary<string, object>());
        if (!extensionResult.ContainsKey("paletteProvider"))
        {
            throw new InvalidOperationException(
                $"Target '{target.DisplayName}' palette extension is invalid.");
        }

        var paletteProvider = (IPaletteProvider)extensionResult["paletteProvider"];
        
        // Extract existing palette data if editing
        byte[]? initialColors = null;
        string? paletteName = null;
        string? paletteElementId = null;
        if (input.ContainsKey("moduleData"))
        {
            var moduleData = (Dictionary<string, object>)input["moduleData"];
            System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] moduleData keys: {string.Join(", ", moduleData.Keys)}");
            
            // Try to extract colors from different possible formats
            // Format 1: Editor format with "colors" key
            if (moduleData.ContainsKey("colors"))
            {
                System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] Found 'colors' key, type: {moduleData["colors"]?.GetType().Name}");
                initialColors = ExtractByteArray(moduleData["colors"]);
                if (initialColors != null)
                    System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] Extracted {initialColors.Length} colors from 'colors'");
            }
            // Format 2: Module format with "bgColors" key (fallback)
            else if (moduleData.ContainsKey("bgColors"))
            {
                System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] Found 'bgColors' key, type: {moduleData["bgColors"]?.GetType().Name}");
                initialColors = ExtractByteArray(moduleData["bgColors"]);
                if (initialColors != null)
                    System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] Extracted {initialColors.Length} colors from 'bgColors': [{string.Join(", ", initialColors)}]");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PaletteEditorTool] No color data found in moduleData");
            }
            
            if (moduleData.ContainsKey("name"))
            {
                paletteName = moduleData["name"]?.ToString();
                System.Diagnostics.Debug.WriteLine($"[PaletteEditorTool] Palette name: {paletteName}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PaletteEditorTool] No moduleData in input");
        }
        
        // Get palette element ID if editing existing module
        if (input.ContainsKey("elementId"))
        {
            paletteElementId = input["elementId"]?.ToString();
        }
        
        var window = new PaletteEditorWindow(paletteProvider, null, initialColors, paletteName, project, paletteElementId);
        return window;
    }
    
    private static byte[]? ExtractByteArray(object colorsObj)
    {
        if (colorsObj is byte[] byteArray)
        {
            return byteArray;
        }
        else if (colorsObj is System.Text.Json.JsonElement jsonElement)
        {
            // Handle array of numbers: [0, 63, 0, 0, ...]
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var colorsList = new List<byte>();
                foreach (var item in jsonElement.EnumerateArray())
                {
                    if (item.TryGetByte(out byte b))
                        colorsList.Add(b);
                }
                return colorsList.ToArray();
            }
            // Handle Base64 string: "AD8AAAAAAAAAAAAAAAAAAA=="
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                try
                {
                    var base64 = jsonElement.GetString();
                    if (!string.IsNullOrEmpty(base64))
                    {
                        return Convert.FromBase64String(base64);
                    }
                }
                catch (FormatException)
                {
                    System.Diagnostics.Debug.WriteLine("[PaletteEditorTool] Failed to decode Base64 string");
                }
            }
        }
        return null;
    }
}
