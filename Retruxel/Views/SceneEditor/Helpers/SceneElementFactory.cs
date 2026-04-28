using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Windows;

namespace Retruxel.Views.SceneEditor.Helpers;

/// <summary>
/// Factory for creating SceneElement instances from modules.
/// </summary>
public class SceneElementFactory
{
    private readonly ModuleRegistry _moduleRegistry;

    public SceneElementFactory(ModuleRegistry moduleRegistry)
    {
        _moduleRegistry = moduleRegistry;
    }

    public SceneElement CreateElement(string moduleId, int tileX, int tileY)
    {
        var moduleTemplate = FindModuleTemplate(moduleId);
        if (moduleTemplate is null)
            throw new InvalidOperationException($"Module not found: {moduleId}");

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;

        ModuleParameterHelper.UpdatePosition(module, tileX, tileY);

        return new SceneElement
        {
            ElementId = Guid.NewGuid().ToString(),
            ModuleId = moduleId,
            Module = module,
            TileX = tileX,
            TileY = tileY
        };
    }

    public IModule? DeserializeModule(string moduleId, System.Text.Json.JsonElement moduleState)
    {
        var moduleTemplate = FindModuleTemplate(moduleId);
        if (moduleTemplate is null) return null;

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;

        if (moduleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
            moduleState.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            var jsonString = moduleState.GetRawText();
            module.Deserialize(jsonString);
        }

        return module;
    }

    private IModule? FindModuleTemplate(string moduleId)
    {
        if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
            return lm;
        if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
            return gm;
        if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
            return am;
        return null;
    }
}

/// <summary>
/// Represents an element placed in the scene.
/// </summary>
public class SceneElement
{
    public string ElementId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public object? Module { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Trigger { get; set; } = "OnStart";
    public UIElement? CanvasVisual { get; set; }
    public UIElement? EventVisual { get; set; }

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(UserId))
                return $"[{UserId}]";

            var shortId = ElementId.Length > 8 ? ElementId[..8] : ElementId;
            return $"[{shortId}...]";
        }
    }
}
