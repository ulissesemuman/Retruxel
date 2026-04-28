using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using Retruxel.Tool.AssetImporter;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Partial class handling assets panel — manages tile and sprite assets,
/// import, delete, and drag-drop to canvas.
/// </summary>
public partial class SceneEditorView
{
    /// <summary>
    /// Rebuilds the asset panel from the current project's asset list.
    /// Separates tiles (background) and sprites into different sections.
    /// </summary>
    private void RefreshAssetPanel()
    {
        if (_project is null) return;

        AssetTilesPanel.Children.Clear();
        AssetSpritesPanel.Children.Clear();

        foreach (var asset in _project.Assets)
        {
            // Determine panel based on VramRegionId
            var panel = asset.VramRegionId.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        asset.VramRegionId.Contains("bg", StringComparison.OrdinalIgnoreCase) ||
                        asset.VramRegionId.Contains("tiles", StringComparison.OrdinalIgnoreCase)
                ? AssetTilesPanel
                : AssetSpritesPanel;

            panel.Children.Add(BuildAssetRow(asset));
        }
    }

    /// <summary>
    /// Builds a single asset row for the asset panel.
    /// Shows color indicator, name, tile count, and delete button.
    /// Supports drag to canvas.
    /// </summary>
    private FrameworkElement BuildAssetRow(AssetEntry asset)
    {
        var border = new Border
        {
            Margin = new Thickness(12, 0, 12, 4),
            Padding = new Thickness(8, 6, 8, 6),
            Cursor = Cursors.Hand,
            Tag = asset.Id
        };
        border.SetResourceReference(Border.BackgroundProperty, "BrushSurfaceContainerHigh");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Color indicator
        var indicator = new Border
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        indicator.SetResourceReference(Border.BackgroundProperty,
            asset.VramRegionId.Contains("background", StringComparison.OrdinalIgnoreCase) ||
            asset.VramRegionId.Contains("bg", StringComparison.OrdinalIgnoreCase) ||
            asset.VramRegionId.Contains("tiles", StringComparison.OrdinalIgnoreCase)
                ? "BrushSecondary" : "BrushTertiary");
        Grid.SetColumn(indicator, 0);

        // Asset name
        var name = new TextBlock
        {
            Text = asset.Id,
            VerticalAlignment = VerticalAlignment.Center
        };
        name.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        Grid.SetColumn(name, 1);

        // Tile count
        var count = new TextBlock
        {
            Text = $"{asset.TileCount}t",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 10,
            Margin = new Thickness(0, 0, 8, 0)
        };
        count.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        Grid.SetColumn(count, 2);
        
        // Delete button
        var deleteBtn = new TextBlock
        {
            Text = "✕",
            FontSize = 12,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Delete asset"
        };
        deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        deleteBtn.MouseEnter += (s, e) => deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
        deleteBtn.MouseLeave += (s, e) => deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        deleteBtn.MouseLeftButtonDown += (s, e) =>
        {
            e.Handled = true;
            DeleteAsset(asset);
        };
        Grid.SetColumn(deleteBtn, 3);

        grid.Children.Add(indicator);
        grid.Children.Add(name);
        grid.Children.Add(count);
        grid.Children.Add(deleteBtn);
        border.Child = grid;

        // Enable drag from asset panel to canvas
        border.MouseMove += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var data = new DataObject("RetruxelAssetDrop", asset);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
            }
        };

        return border;
    }

    /// <summary>
    /// Opens asset importer for tiles (background region).
    /// </summary>
    private void BtnImportTiles_Click(object sender, RoutedEventArgs e)
        => OpenAssetImporter("background");

    /// <summary>
    /// Opens asset importer for sprites.
    /// </summary>
    private void BtnImportSprites_Click(object sender, RoutedEventArgs e)
        => OpenAssetImporter("sprites");

    /// <summary>
    /// Opens the asset importer window with pre-selected VRAM region.
    /// Adds imported asset to project and refreshes panel.
    /// </summary>
    private void OpenAssetImporter(string vramRegionId)
    {
        if (_project is null || _target is null) return;

        var window = new AssetImporterWindow(_target, _project.ProjectPath)
        {
            Owner = Window.GetWindow(this)
        };

        // Pre-select VRAM region
        window.PreSelectRegion(vramRegionId);

        if (window.ShowDialog() == true && window.ImportedAsset is not null)
        {
            // Check for duplicate ID
            if (_project.Assets.Any(a => a.Id == window.ImportedAsset.Id))
            {
                MessageBox.Show(
                    $"An asset named '{window.ImportedAsset.Id}' already exists.",
                    "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var asset = window.ImportedAsset;

            // Create state change for importing asset (Large change — auto-saves)
            var change = new StateChange
            {
                Description = $"Import asset '{asset.Id}'",
                Type = ChangeType.Large,
                Execute = () =>
                {
                    _project.Assets.Add(asset);
                    RefreshAssetPanel();
                },
                IsUndoable = false // Asset import is not undoable (file already copied)
            };

            _stateManager?.ApplyChange(change);
        }
    }
    
    /// <summary>
    /// Deletes an asset from the project after confirmation.
    /// Checks if the asset is in use by any modules before deletion.
    /// </summary>
    private void DeleteAsset(AssetEntry asset)
    {
        if (_project is null) return;
        
        // Check if asset is in use
        var usedBy = new List<string>();
        foreach (var scene in _project.Scenes)
        {
            foreach (var element in scene.Elements)
            {
                if (element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    element.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Null)
                    continue;
                    
                if (element.ModuleState.TryGetProperty("tilesAssetId", out var assetId))
                {
                    if (assetId.GetString() == asset.Id)
                    {
                        var label = !string.IsNullOrEmpty(element.UserId) ? element.UserId : element.ElementId[..8];
                        usedBy.Add($"{scene.SceneName}/{label}");
                    }
                }
            }
        }
        
        if (usedBy.Count > 0)
        {
            var modules = string.Join("\n  • ", usedBy);
            var result = MessageBox.Show(
                $"Asset '{asset.Id}' is currently used by:\n  • {modules}\n\nDelete anyway?",
                "Asset In Use",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        else
        {
            var result = MessageBox.Show(
                $"Delete asset '{asset.Id}'?\n\nThis action cannot be undone.",
                "Delete Asset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        
        // Create state change for deleting asset (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Delete asset '{asset.Id}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Assets.Remove(asset);
                RefreshAssetPanel();
            },
            IsUndoable = false // Asset deletion not undoable (file operations)
        };

        _stateManager?.ApplyChange(change);
    }

    /// <summary>
    /// Handles an asset dropped from the asset panel onto the canvas.
    /// Resolves the correct module for the asset's VRAM region, creates it, and
    /// sets tilesAssetId automatically — no manual typing required.
    ///
    /// Tilemap  ← background/bg/tiles region (module with "tilemap" in ID + tilesAssetId param)
    /// Sprite   ← sprites region (module with "sprite" in ID + tilesAssetId param)
    /// </summary>
    private void DropAssetOnCanvas(AssetEntry asset, int tileX, int tileY)
    {
        var moduleId = ResolveModuleForAsset(asset.VramRegionId);

        if (moduleId is null)
        {
            MessageBox.Show(
                $"No module available for VRAM region '{asset.VramRegionId}'. " +
                $"Make sure the active target supports this asset type.",
                "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create and place the module — uses existing undo stack, events panel, etc.
        AddModuleToCanvas(moduleId, tileX, tileY);

        // The new element is always the last one added
        var element = _elements.LastOrDefault();
        if (element?.Module is not IModule module) return;

        // Inject the asset ID directly into the module
        SetModuleParameterValue(module, "tilesAssetId", asset.Id, ParameterType.String);

        // Refresh the canvas visual (gray box → actual asset image)
        RefreshElementVisual(element);

        // Refresh properties panel to show the pre-filled tilesAssetId
        if (_selectedElement == element)
            BuildPropertiesPanel(element);

        SyncProjectModules();

        // Open visual tool if available (e.g., tilemap editor)
        OpenVisualToolForElement(element);
    }

    /// <summary>
    /// Finds the appropriate module ID for the given VRAM region by searching
    /// all loaded modules for one whose ID contains the expected keyword
    /// and whose manifest declares a tilesAssetId parameter.
    /// This approach is target-agnostic — works for SMS, GG, NES, etc.
    /// </summary>
    private string? ResolveModuleForAsset(string vramRegionId)
    {
        if (_moduleRegistry is null) return null;

        // Determine keyword based on VRAM region
        var keyword = vramRegionId.Contains("sprite", StringComparison.OrdinalIgnoreCase)
            ? "sprite"
            : "tilemap";

        foreach (var (id, m) in _moduleRegistry.LogicModules)
        {
            if (!id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (m is ILogicModule lm && lm.GetManifest().Parameters.Any(p => p.Name == "tilesAssetId"))
                return id;
        }

        foreach (var (id, m) in _moduleRegistry.GraphicModules)
        {
            if (!id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (m is IGraphicModule gm && gm.GetManifest().Parameters.Any(p => p.Name == "tilesAssetId"))
                return id;
        }

        return null;
    }
}
