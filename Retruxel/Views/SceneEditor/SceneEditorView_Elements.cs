using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Retruxel.Views;

/// <summary>
/// CRUD operations for the typed model — PlaneLayerData, EntityData, ProjectModuleData.
/// No canvas drag-drop. All placement happens through the project tree.
/// </summary>
public partial class SceneEditorView
{
    // ── Plane layer ──────────────────────────────────────────────────────────

    internal void AddPlaneLayer(PlaneData plane)
    {
        if (_currentScene is null) return;

        var layer = new PlaneLayerData
        {
            LayerId   = Guid.NewGuid().ToString(),
            LayerName = $"Layer {plane.Layers.Count}",
            Visible   = true
        };

        var change = new StateChange
        {
            Description = $"Add layer to '{plane.Label}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                plane.Layers.Add(layer);
                _projectManager?.MarkDirty();
                RebuildProjectTree();
                RefreshPreview();

                // Open tilemap editor immediately
                Dispatcher.InvokeAsync(
                    () => OpenTilemapEditor(layer),
                    System.Windows.Threading.DispatcherPriority.Background);
            },
            IsUndoable  = true,
            UndoCommand = new AddElementCommand(
                description: $"Add layer to '{plane.Label}'",
                add:    () => { plane.Layers.Add(layer);    _projectManager?.MarkDirty(); RebuildProjectTree(); RefreshPreview(); },
                remove: () => { plane.Layers.Remove(layer); _projectManager?.MarkDirty(); RebuildProjectTree(); RefreshPreview(); })
        };

        _stateManager?.ApplyChange(change);
    }

    internal void RemovePlaneLayer(PlaneLayerData layer)
    {
        if (_currentScene is null) return;

        var parentPlane = _currentScene.Planes
            .FirstOrDefault(tm => tm.Layers.Contains(layer));
        if (parentPlane is null) return;

        var change = new StateChange
        {
            Description = $"Remove layer '{layer.LayerName}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                parentPlane.Layers.Remove(layer);
                if (_selectedItem == layer) SelectItem(null);
                _projectManager?.MarkDirty();
                RebuildProjectTree();
                RefreshPreview();
            },
            IsUndoable  = true,
            UndoCommand = new AddElementCommand(
                description: $"Remove layer '{layer.LayerName}'",
                add:    () => { parentPlane.Layers.Remove(layer); _projectManager?.MarkDirty(); RebuildProjectTree(); RefreshPreview(); },
                remove: () => { parentPlane.Layers.Add(layer);    _projectManager?.MarkDirty(); RebuildProjectTree(); RefreshPreview(); })
        };

        _stateManager?.ApplyChange(change);
    }

    // ── Entity ─────────────────────────────────────────────────────────────────

    internal void AddEntity(string entityType)
    {
        if (_currentScene is null) return;

        var entity = new EntityData
        {
            EntityId    = Guid.NewGuid().ToString(),
            Label       = entityType,
            EntityType  = entityType,
            PaletteSlot = 1
        };

        var change = new StateChange
        {
            Description = $"Add entity '{entityType}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                _currentScene.Entities.Add(entity);
                _projectManager?.MarkDirty();
                RebuildProjectTree();
            },
            IsUndoable  = true,
            UndoCommand = new AddElementCommand(
                description: $"Add entity '{entityType}'",
                add:    () => { _currentScene.Entities.Add(entity);    _projectManager?.MarkDirty(); RebuildProjectTree(); },
                remove: () => { _currentScene.Entities.Remove(entity); _projectManager?.MarkDirty(); RebuildProjectTree(); })
        };

        _stateManager?.ApplyChange(change);
    }

    internal void RemoveEntity(EntityData entity)
    {
        if (_currentScene is null) return;

        var change = new StateChange
        {
            Description = $"Remove entity '{entity.Label}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                _currentScene.Entities.Remove(entity);
                if (_selectedItem == entity) SelectItem(null);
                _projectManager?.MarkDirty();
                RebuildProjectTree();
            },
            IsUndoable  = true,
            UndoCommand = new AddElementCommand(
                description: $"Remove entity '{entity.Label}'",
                add:    () => { _currentScene.Entities.Remove(entity); _projectManager?.MarkDirty(); RebuildProjectTree(); },
                remove: () => { _currentScene.Entities.Add(entity);    _projectManager?.MarkDirty(); RebuildProjectTree(); })
        };

        _stateManager?.ApplyChange(change);
    }

    // ── Project-level module ───────────────────────────────────────────────────

    internal void AddProjectModule(string moduleId, bool isGlobal)
    {
        if (_project is null || _currentScene is null) return;

        var mod = new ProjectModuleData
        {
            ModuleId = moduleId,
            Label    = moduleId,
            Enabled  = true
        };

        if (isGlobal)
        {
            var change = new StateChange
            {
                Description = $"Add global module '{moduleId}'",
                Type        = ChangeType.Large,
                Execute     = () =>
                {
                    _project.Modules.Add(mod);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                },
                IsUndoable  = true,
                UndoCommand = new AddElementCommand(
                    description: $"Add global module '{moduleId}'",
                    add:    () => { _project.Modules.Add(mod);    _projectManager?.MarkDirty(); RebuildProjectTree(); },
                    remove: () => { _project.Modules.Remove(mod); _projectManager?.MarkDirty(); RebuildProjectTree(); })
            };
            _stateManager?.ApplyChange(change);
        }
        else
        {
            var change = new StateChange
            {
                Description = $"Add scene module override '{moduleId}'",
                Type        = ChangeType.Large,
                Execute     = () =>
                {
                    _currentScene.ModuleOverrides.Add(mod);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                },
                IsUndoable  = true,
                UndoCommand = new AddElementCommand(
                    description: $"Add scene module override '{moduleId}'",
                    add:    () => { _currentScene.ModuleOverrides.Add(mod);    _projectManager?.MarkDirty(); RebuildProjectTree(); },
                    remove: () => { _currentScene.ModuleOverrides.Remove(mod); _projectManager?.MarkDirty(); RebuildProjectTree(); })
            };
            _stateManager?.ApplyChange(change);
        }
    }

    internal void RemoveProjectModule(ProjectModuleData mod, bool isGlobal)
    {
        if (_project is null || _currentScene is null) return;

        if (isGlobal)
        {
            var change = new StateChange
            {
                Description = $"Remove global module '{mod.ModuleId}'",
                Type        = ChangeType.Large,
                Execute     = () =>
                {
                    _project.Modules.Remove(mod);
                    if (_selectedItem == mod) SelectItem(null);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                },
                IsUndoable = false
            };
            _stateManager?.ApplyChange(change);
        }
        else
        {
            var change = new StateChange
            {
                Description = $"Remove scene module '{mod.ModuleId}'",
                Type        = ChangeType.Large,
                Execute     = () =>
                {
                    _currentScene.ModuleOverrides.Remove(mod);
                    if (_selectedItem == mod) SelectItem(null);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                },
                IsUndoable = false
            };
            _stateManager?.ApplyChange(change);
        }
    }

    // ── Open editors ───────────────────────────────────────────────────────────

    internal void OpenTilemapEditor(PlaneLayerData layer)
    {
        if (_project is null || _target is null || _currentScene is null) return;

        var result = VisualToolInvoker.OpenTilemapEditor(
            layer, _target, _project, _project.ProjectPath, _currentScene,
            async () =>
            {
                _projectManager?.MarkDirty();
                await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
            },
            this);

        if (result)
        {
            var change = new StateChange
            {
                Description = $"Edit plane layer '{layer.LayerName}'",
                Type        = ChangeType.Large,
                Execute     = () => { },
                IsUndoable  = false
            };
            _stateManager?.RegisterChange(change);
        }

        RefreshPreview();
        if (_selectedItem == layer)
            BuildPropertiesPanel(layer);
    }

    internal void OpenEntityEditor(EntityData entity)
    {
        if (_project is null || _target is null || _currentScene is null) return;

        var result = VisualToolInvoker.OpenSpriteEditor(
            entity, _target, _project, _project.ProjectPath, _currentScene,
            async () =>
            {
                _projectManager?.MarkDirty();
                await (_stateManager?.SaveNowAsync() ?? Task.CompletedTask);
            },
            this);

        if (result)
        {
            var change = new StateChange
            {
                Description = $"Edit entity '{entity.Label}'",
                Type        = ChangeType.Large,
                Execute     = () => { },
                IsUndoable  = false
            };
            _stateManager?.RegisterChange(change);
        }

        if (_selectedItem == entity)
            BuildPropertiesPanel(entity);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private string GetPlaneAssetLabel(PlaneLayerData layer)
    {
        if (string.IsNullOrEmpty(layer.AssetId)) return string.Empty;
        var asset = _project?.Assets.FirstOrDefault(a => a.Id == layer.AssetId);
        return asset is null ? layer.AssetId : $"{layer.AssetId} · {asset.GenerationParams?.TileCount ?? 0}t";
    }
}
