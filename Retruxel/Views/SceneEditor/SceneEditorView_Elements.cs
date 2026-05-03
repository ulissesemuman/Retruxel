using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    private SceneElement CreateSceneElement(string moduleId, int tileX, int tileY)
    {
        if (_moduleRegistry is null)
            throw new InvalidOperationException("ModuleRegistry not initialized");

        IModule? moduleTemplate = null;

        if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
            moduleTemplate = am;

        if (moduleTemplate is null)
            throw new InvalidOperationException($"Module not found: {moduleId}");

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;

        UpdateModulePosition(module, tileX, tileY);

        return new SceneElement
        {
            ElementId = Guid.NewGuid().ToString(),
            ModuleId = moduleId,
            Module = module,
            TileX = tileX,
            TileY = tileY,
            Data = new SceneElementData
            {
                ElementId = Guid.NewGuid().ToString(),
                ModuleId = moduleId,
                TileX = tileX,
                TileY = tileY
            }
        };
    }

    private void RemoveElement(SceneElement element)
    {
        var displayName = element.Module is IModule m ? m.DisplayName : element.ModuleId;

        var change = new StateChange
        {
            Description = $"Remove {displayName}",
            Type = ChangeType.Large,
            Execute = () =>
            {
                RemoveElementCore(element);
                RefreshModulePalette();
                RefreshStructurePanel();
            },
            IsUndoable = true,
            UndoCommand = new RemoveElementCommand(
                description: $"Remove {displayName}",
                remove: () =>
                {
                    RemoveElementCore(element);
                    RefreshModulePalette();
                    RefreshStructurePanel();
                },
                restore: () =>
                {
                    _elements.Add(element);

                    if (element.CanvasVisual is not null)
                    {
                        Canvas.SetLeft(element.CanvasVisual, element.TileX * 8);
                        Canvas.SetTop(element.CanvasVisual, element.TileY * 8);
                        SceneCanvas.Children.Add(element.CanvasVisual);
                    }

                    RefreshModulePalette();
                    RefreshStructurePanel();
                }
            )
        };

        _stateManager?.ApplyChange(change);
    }

    private void RemoveElementCore(SceneElement element)
    {
        _elements.Remove(element);

        if (element.CanvasVisual is not null)
            SceneCanvas.Children.Remove(element.CanvasVisual);

        if (_selectedElement == element)
            SelectElement(null);
        
        RefreshStructurePanel();
    }

    private void UpdateElementLabel(SceneElement element)
    {
        if (element.CanvasVisual is Border cv && cv.Child is not System.Windows.Controls.Image)
        {
            cv.Padding = new Thickness(4, 2, 4, 2);
            cv.Child = BuildModuleLabel(element);
        }

        if (element.EventVisual is Border ev && ev.Child is Grid g)
        {
            var label = g.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
            if (label is not null)
                label.Text = GetModuleDisplayText(element);
        }
    }

    private void RefreshElementVisual(SceneElement element)
    {
        if (element.CanvasVisual is not Border border) return;

        var assetVisual = TryBuildAssetVisual(element);
        if (assetVisual is not null)
        {
            border.Padding = new Thickness(0);
            border.Child = assetVisual;
        }
        else
        {
            border.Padding = new Thickness(4, 2, 4, 2);
            border.Child = BuildModuleLabel(element);
        }

        if (element.EventVisual is Border ev && ev.Child is Grid g)
        {
            var label = g.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
            if (label is not null)
                label.Text = GetModuleDisplayText(element);
        }
    }

    private string GetElementDisplayLabel(SceneElement element)
    {
        if (!string.IsNullOrEmpty(element.UserId))
            return $"[{element.UserId}]";

        var shortId = element.ElementId.Length > 8 ? element.ElementId[..8] : element.ElementId;
        return $"[{shortId}...]";
    }

    private void UpdateElementPosition(SceneElement element)
    {
        if (element.CanvasVisual is Border border)
        {
            Canvas.SetLeft(border, element.TileX * 8);
            Canvas.SetTop(border, element.TileY * 8);
        }
    }

    private void UpdateModulePosition(object? module, int x, int y)
    {
        if (module is not IModule imodule) return;
        SetModuleParameterValue(imodule, "x", x.ToString(), ParameterType.Int);
        SetModuleParameterValue(imodule, "y", y.ToString(), ParameterType.Int);
    }



    private void SelectElement(SceneElement? element)
    {
        if (_selectedElement is not null)
        {
            if (_selectedElement.CanvasVisual is Border cv)
            {
                cv.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
                cv.BorderThickness = new Thickness(1);
            }
            if (_selectedElement.EventVisual is Border ev)
                ev.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        }

        _selectedElement = element;

        if (element is null)
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            PropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (element.CanvasVisual is Border cvSelected)
        {
            cvSelected.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
            cvSelected.BorderThickness = new Thickness(2);
        }
        if (element.EventVisual is Border evSelected)
            evSelected.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));

        TxtNoSelection.Visibility = Visibility.Collapsed;
        PropertiesPanel.Visibility = Visibility.Visible;

        if (!_isLoadingProject)
            BuildPropertiesPanel(element);

        Focus();
    }

    public void AddElementFromData(SceneElementData elementData)
    {
        if (_currentScene is null || _moduleRegistry is null) return;

        try
        {
            var module = DeserializeModule(elementData.ModuleId, elementData.ModuleState);
            if (module is null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize module: {elementData.ModuleId}");
                return;
            }

            var element = new SceneElement
            {
                ElementId = elementData.ElementId,
                UserId = elementData.UserId,
                ModuleId = elementData.ModuleId,
                Module = module,
                TileX = elementData.TileX,
                TileY = elementData.TileY,
                Trigger = elementData.Trigger,
                Data = elementData
            };

            UpdateModulePosition(module, elementData.TileX, elementData.TileY);
            _elements.Add(element);

            System.Diagnostics.Debug.WriteLine($"Added element: {elementData.ModuleId}, Trigger: {elementData.Trigger}, TileX: {elementData.TileX}, TileY: {elementData.TileY}");

            // Create canvas visual only for canvas modules (entity, enemy, scroll)
            var destination = GetModuleDestination(elementData.ModuleId);
            if (destination == "CANVAS")
            {
                var visual = BuildCanvasElement(element);
                Canvas.SetLeft(visual, element.TileX * 8);
                Canvas.SetTop(visual, element.TileY * 8);
                SceneCanvas.Children.Add(visual);
                element.CanvasVisual = visual;
            }

            RefreshModulePalette();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in AddElementFromData: {ex.Message}");
        }
    }

    private IModule? DeserializeModule(string moduleId, System.Text.Json.JsonElement moduleState)
    {
        if (_moduleRegistry is null) return null;

        IModule? moduleTemplate = null;

        if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
            moduleTemplate = am;

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

    private void OpenVisualToolForElement(SceneElement element)
    {
        if (_project is null || _target is null || _currentScene is null) return;
        if (element.Module is not IModule module) return;
        if (string.IsNullOrEmpty(module.VisualToolId)) return;

        var elementData = _currentScene.Elements.FirstOrDefault(e => e.ElementId == element.ElementId);

        var toolResult = VisualToolInvoker.OpenVisualTool(
            module,
            _target,
            _project,
            _project.ProjectPath,
            _currentScene,
            elementData,
            async () =>
            {
                _projectManager?.MarkDirty();
                await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
            },
            this);
        
        if (toolResult)
        {
            var displayName = module.DisplayName;
            var change = new StateChange
            {
                Description = $"Edit {displayName}",
                Type = ChangeType.Large,
                Execute = () => { },
                IsUndoable = false
            };
            _stateManager?.RegisterChange(change);
        }

        RefreshElementVisual(element);
        if (_selectedElement == element)
            BuildPropertiesPanel(element);
    }
}
