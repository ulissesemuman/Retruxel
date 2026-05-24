using Retruxel.Core.Models;
using Retruxel.Services;
using Retruxel.Tool.AssetImporter;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Retruxel.Views;

/// <summary>
/// Asset management — import and delete.
/// Assets are displayed in the project tree, not a separate panel.
/// </summary>
public partial class SceneEditorView
{
    private void OpenAssetImporter(string vramRegionId)
    {
        if (_project is null || _target is null) return;

        var window = new AssetImporterWindow(_target, _project.ProjectPath, _currentScene)
        {
            Owner = Window.GetWindow(this)
        };
        window.PreSelectRegion(vramRegionId);

        if (window.ShowDialog() == true && window.ImportedAsset is not null)
        {
            if (_project.Assets.Any(a => a.Id == window.ImportedAsset.Id))
            {
                MessageBox.Show(
                    $"An asset named '{window.ImportedAsset.Id}' already exists.",
                    "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var asset = window.ImportedAsset;

            var change = new StateChange
            {
                Description = $"Import asset '{asset.Id}'",
                Type        = ChangeType.Large,
                Execute     = () =>
                {
                    _project.Assets.Add(asset);
                    RebuildProjectTree();
                },
                IsUndoable = false
            };

            _stateManager?.ApplyChange(change);
        }
    }

    private void DeleteAsset(AssetEntry asset)
    {
        if (_project is null) return;

        // Check if asset is in use across all typed collections and legacy elements
        var usedBy = new List<string>();
        foreach (var scene in _project.Scenes)
        {
            // Check typed plane layers
            foreach (var plane in scene.Planes)
            {
                foreach (var layer in plane.Layers)
                {
                    if (layer.AssetId == asset.Id)
                    {
                        var label = !string.IsNullOrEmpty(layer.LayerName) ? layer.LayerName : layer.LayerId[..8];
                        usedBy.Add($"{scene.SceneName}/{label}");
                    }
                }
            }

            // Check typed entities
            foreach (var entity in scene.Entities)
            {
                if (entity.SpriteAssetId == asset.Id)
                {
                    var label = !string.IsNullOrEmpty(entity.Label) ? entity.Label : entity.EntityId[..8];
                    usedBy.Add($"{scene.SceneName}/{label}");
                }
            }

            // Check legacy flat elements (backward compat)
            foreach (var element in scene.Elements)
            {
                if (element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Null)
                    continue;

                if (element.ModuleState.TryGetProperty("tilesAssetId", out var assetId) &&
                    assetId.GetString() == asset.Id)
                {
                    var label = !string.IsNullOrEmpty(element.UserId)
                        ? element.UserId : element.ElementId[..8];
                    usedBy.Add($"{scene.SceneName}/{label}");
                }
            }
        }

        string confirmMsg;
        if (usedBy.Count > 0)
        {
            var modules = string.Join("\n  • ", usedBy);
            confirmMsg  = $"Asset '{asset.Id}' is used by:\n  • {modules}\n\nDelete anyway?";
        }
        else
        {
            confirmMsg = $"Delete asset '{asset.Id}'?\n\nThis cannot be undone.";
        }

        if (MessageBox.Show(confirmMsg, "Delete Asset",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var change = new StateChange
        {
            Description = $"Delete asset '{asset.Id}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                _project.Assets.Remove(asset);
                RebuildProjectTree();
            },
            IsUndoable = false
        };

        _stateManager?.ApplyChange(change);
    }
}
