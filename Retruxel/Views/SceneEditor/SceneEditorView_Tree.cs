using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Builds and refreshes the hierarchical project tree in the left panel.
///
/// Tree structure:
///
///  ▼ PROJECT
///      ▼ ASSETS              [+ Import]
///          forest_tiles      [✖]
///      ▼ MODULES  global     [+ Add]
///          Physics           [···] [✖]
///          Input             [···] [✖]
///      ▼ SCENES              [+ Add]
///          ▶ Scene1  (initial)
///          ▼ Scene2  ↑  current, expanded
///              ▼ PALETTE     (fixed by target)
///                  BG        [EDIT]
///                  SP        [EDIT]
///              ▼ PLANES    (count fixed by target)
///                  ▼ Plane 0           [+ Add Layer]
///                      Layer 0           [···] [✖]
///              ▼ MODULES  scene overrides [+ Add]
///                  Physics override      [···] [✖]
///              ▼ ENTITIES               [+ Add]
///                  Player               [···] [✖]
///                  Enemy                [···] [✖]
/// </summary>
public partial class SceneEditorView
{
    private readonly HashSet<string> _expandedNodes = new(StringComparer.Ordinal)
    {
        "project", "assets", "modules_global", "scenes",
        "palette", "planes", "modules_scene", "entities", "scene_behaviors"
    };

    // ── Entry point ────────────────────────────────────────────────────────────

    private void RebuildProjectTree()
    {
        ProjectTreePanel.Children.Clear();
        if (_project is null || _currentScene is null) return;
        BuildProjectNode();
    }

    // ── PROJECT ────────────────────────────────────────────────────────────────

    private void BuildProjectNode()
    {
        AddTreeSection("PROJECT", "project");
        if (!IsExpanded("project")) return;

        BuildAssetsSection();
        BuildGlobalModulesSection();
        BuildPrefabsSection();
        BuildScenesSection();
    }

    // ── ASSETS ─────────────────────────────────────────────────────────────────

    private void BuildAssetsSection()
    {
        AddTreeSubSection("ASSETS", "assets", indent: 1,
            onAdd: () => OpenAssetImporter("bg"));

        if (!IsExpanded("assets")) return;

        if (_project!.Assets.Count == 0)
        {
            AddTreeEmpty("No assets imported", indent: 2);
            return;
        }

        foreach (var asset in _project.Assets)
        {
            var tileCount = asset.GenerationParams?.TileCount ?? 0;

            AddTreeLeaf(
                label:    asset.Id,
                sublabel: $"{tileCount}t",
                indent:   2,
                onClick:  null,
                onDelete: () => DeleteAsset(asset));
        }
    }

    // ── PREFABS ────────────────────────────────────────────────────────────────

    private void BuildPrefabsSection()
    {
        AddTreeSubSection("PREFABS", "prefabs", indent: 1,
            onAdd: () => ShowPrefabPickerDialog());

        if (!IsExpanded("prefabs") || _project is null) return;

        if (_project.Prefabs.Count == 0)
        {
            AddTreeEmpty("No prefabs — click + to create", indent: 2);
            return;
        }

        foreach (var prefab in _project.Prefabs)
        {
            var p = prefab;
            AddTreeLeafWithEdit(
                label:    string.IsNullOrEmpty(p.DisplayName) ? p.PrefabId : p.DisplayName,
                sublabel: p.PrefabId,
                indent:   2,
                onEdit:   () => OpenPrefabEditor(p),
                onDelete: () => RemovePrefab(p));
        }
    }

    private void OpenPrefabEditor(PrefabData prefab)
    {
        if (_project is null || _target is null || _actionRegistry is null) return;

        Dispatcher.BeginInvoke(() =>
        {
            var window = new Retruxel.Tool.PrefabEditor.PrefabEditorWindow(
                prefab, _project, _target, _actionRegistry,
                saveCallback: saved =>
                {
                    var idx = _project.Prefabs.FindIndex(p => p.PrefabId == saved.PrefabId
                                                           || p.PrefabId == prefab.PrefabId);
                    if (idx >= 0) _project.Prefabs[idx] = saved;
                    else          _project.Prefabs.Add(saved);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                    RefreshPreview();
                },
                owner: Window.GetWindow(this));
            window.ShowDialog();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void RemovePrefab(PrefabData prefab)
    {
        if (_project is null) return;

        // Check if any entity in any scene references this prefab
        var inUse = _project.Scenes
            .SelectMany(s => s.Entities)
            .Any(e => e.PrefabId == prefab.PrefabId);

        if (inUse)
        {
            System.Windows.MessageBox.Show(
                $"Prefab '{prefab.PrefabId}' is used by one or more entities and cannot be deleted.",
                "Retruxel", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        _project.Prefabs.Remove(prefab);
        _projectManager?.MarkDirty();
        RebuildProjectTree();
    }

    // ── GLOBAL MODULES ─────────────────────────────────────────────────────────

    private void BuildGlobalModulesSection()
    {
        AddTreeSubSection("MODULES", "modules_global", indent: 1,
            sublabel: "global defaults",
            onAdd:    () => ShowModulePickerDialog(isGlobal: true));

        if (!IsExpanded("modules_global")) return;

        if (_project!.Modules.Count == 0)
        {
            AddTreeEmpty("No global modules", indent: 2);
            return;
        }

        foreach (var mod in _project.Modules)
        {
            var label = string.IsNullOrEmpty(mod.Label) ? mod.ModuleId : mod.Label;
            if (!mod.Enabled) label += "  · disabled";

            AddTreeLeafWithEdit(
                label:    label,
                sublabel: mod.ModuleId,
                indent:   2,
                onEdit:   () => { SelectItem(mod); ShowPropertiesForItem(mod); },
                onDelete: () => RemoveProjectModule(mod, isGlobal: true));
        }
    }

    // ── SCENES ─────────────────────────────────────────────────────────────────

    private void BuildScenesSection()
    {
        AddTreeSubSection("SCENES", "scenes", indent: 1, onAdd: () => BtnNewScene_Click());
        if (!IsExpanded("scenes") || _project is null) return;

        foreach (var scene in _project.Scenes)
        {
            bool isCurrent = scene.SceneId == _currentScene!.SceneId;
            bool isInitial = scene.SceneId == _project.InitialSceneId;
            var  key       = $"scene_{scene.SceneId}";

            if (isCurrent)
            {
                _expandedNodes.Add(key);
                BuildCurrentSceneNode(scene, key, isInitial);
            }
            else
            {
                AddTreeSceneRow(scene, key, isInitial);
            }
        }
    }

    // ── SCENE ROW (collapsed) ──────────────────────────────────────────────────

    private void AddTreeSceneRow(SceneData scene, string key, bool isInitial)
    {
        var label = isInitial ? $"▶  {scene.SceneName}  ·  initial" : $"▶  {scene.SceneName}";

        var row = BuildTreeRow(
            indent: 2, expandKey: null, label: label, sublabel: null,
            foreground: (Brush)FindResource("BrushOnSurface"),
            onExpand: null, onAdd: null, onEdit: null,
            onDelete: () => DeleteScene(scene));

        row.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) StartInlineRename(row, scene);
            else                   ActivateScene(scene);
            e.Handled = true;
        };

        var menu = new ContextMenu();
        AddMenuItemTo(menu, "Switch to Scene",      () => ActivateScene(scene));
        AddMenuItemTo(menu, "Rename",               () => StartInlineRename(row, scene));
        AddMenuItemTo(menu, "Set as Initial Scene", () => SetInitialScene(scene), disabled: isInitial);
        AddMenuItemTo(menu, "Delete",               () => DeleteScene(scene),
            disabled: _project!.Scenes.Count <= 1);
        row.ContextMenu = menu;

        ProjectTreePanel.Children.Add(row);
    }

    // ── CURRENT SCENE (expanded) ───────────────────────────────────────────────

    private void BuildCurrentSceneNode(SceneData scene, string key, bool isInitial)
    {
        var label = isInitial ? $"▼  {scene.SceneName}  ·  initial" : $"▼  {scene.SceneName}";

        var header = BuildTreeRow(
            indent: 2, expandKey: key, label: label, sublabel: "current scene",
            foreground: (Brush)FindResource("BrushPrimary"),
            onExpand: RebuildProjectTree, onAdd: null, onEdit: null,
            onDelete: () => DeleteScene(scene));
        ProjectTreePanel.Children.Add(header);

        if (!IsExpanded(key)) return;

        BuildPaletteSection(scene);
        BuildPlanesSection(scene);
        BuildSceneModulesSection(scene);
        BuildEntitiesSection(scene);
        BuildSceneBehaviorsSection(scene);
        BuildVramUsageBar(scene);
    }

    // ── PALETTE ────────────────────────────────────────────────────────────────

    private void BuildPaletteSection(SceneData scene)
    {
        AddTreeSubSection("PALETTE", "palette", indent: 3, sublabel: "defined by target");
        if (!IsExpanded("palette")) return;

        if (scene.PaletteSlots.Count == 0)
        {
            AddTreeEmpty("No palette slots", indent: 4);
            return;
        }

        for (int i = 0; i < scene.PaletteSlots.Count; i++)
        {
            var slot     = scene.PaletteSlots[i];
            var slotType = _target?.GetPaletteSlotType(i).ToString() ?? "Palette";
            var label    = string.IsNullOrEmpty(slot.Label) ? slotType : slot.Label;
            BuildPaletteSlotRow(slot, label, i, indent: 4);
        }
    }

    private void BuildPaletteSlotRow(PaletteSlotData slot, string label, int slotIndex, int indent)
    {
        var row = new Border { Padding = new Thickness(indent * 8, 3, 8, 3), Cursor = Cursors.Hand };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelBlock = new TextBlock { Text = label, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        labelBlock.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        Grid.SetColumn(labelBlock, 0);

        var strip = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 6, 0) };
        foreach (var hex in slot.Colors.Take(8))
        {
            strip.Children.Add(new Border
            {
                Width  = 6, Height = 6,
                Background = ParseHexBrush(hex),
                Margin = new Thickness(0, 0, 1, 0)
            });
        }
        Grid.SetColumn(strip, 1);

        var editBtn = new TextBlock { Text = "EDIT", FontSize = 9, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
        editBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        editBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; OpenPaletteSlotEditor(slotIndex); };
        Grid.SetColumn(editBtn, 2);

        grid.Children.Add(labelBlock);
        grid.Children.Add(strip);
        grid.Children.Add(editBtn);
        row.Child = grid;

        ProjectTreePanel.Children.Add(row);
    }

    // ── PLANES ───────────────────────────────────────────────────────────────

    private void BuildPlanesSection(SceneData scene)
    {
        var hardwarePlanes = _target?.Specs.Planes ?? [];
        var planeLabel     = hardwarePlanes.Length > 0 ? $"{hardwarePlanes.Length} plane(s)" : "hardware planes";
        AddTreeSubSection("PLANES", "planes", indent: 3, sublabel: planeLabel);
        if (!IsExpanded("planes")) return;

        if (hardwarePlanes.Length == 0)
        {
            AddTreeEmpty("No planes — target may not support BG layers", indent: 4);
            return;
        }

        // Always iterate the hardware plane specs so every plane defined by the
        // target is visible in the tree, even if the scene PlaneData hasn't been
        // created yet (e.g. older project files loaded before EnsureDefaultPlanes ran).
        foreach (var specs in hardwarePlanes)
        {
            // Find or create the matching PlaneData in the scene.
            var plane = scene.Planes.FirstOrDefault(p => p.PlaneId == specs.Id);
            if (plane is null)
            {
                plane = new PlaneData { PlaneId = specs.Id };
                scene.Planes.Add(plane);
                _projectManager?.MarkDirty();
            }

            var tmKey = $"plane_{plane.PlaneId}";
            AddTreeSubSection(specs.Label, tmKey, indent: 4,
                onAdd: () => AddPlaneLayer(plane));

            if (!IsExpanded(tmKey)) continue;

            if (plane.Layers.Count == 0)
            {
                AddTreeEmpty("No layers — click + to add", indent: 5);
                continue;
            }

            foreach (var layer in plane.Layers)
                BuildPlaneLayerRow(plane, layer, indent: 5);
        }
    }

    private void BuildPlaneLayerRow(PlaneData plane, PlaneLayerData layer, int indent)
    {
        var layerName = string.IsNullOrEmpty(layer.LayerName)
            ? $"Layer {plane.Layers.IndexOf(layer)}"
            : layer.LayerName;

        // Sublabel: asset id + collision action when active
        string? sublabel = null;
        if (!string.IsNullOrEmpty(layer.AssetId))
        {
            sublabel = layer.HasCollision
                ? $"{layer.AssetId}  ·  {layer.CollisionActionLabel}"
                : layer.AssetId;
        }

        var row = new Border
        {
            Padding = new Thickness(indent * 8, 3, 8, 3),
            Cursor  = Cursors.Hand
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // collision
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✍
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 👀
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✖

        // Label + sublabel
        var textStack = new StackPanel();
        var lbl = new TextBlock { Text = layerName, FontSize = 11 };
        lbl.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        lbl.SetResourceReference(TextBlock.ForegroundProperty,
            layer.Visible ? "BrushOnSurface" : "BrushOnSurfaceVariant");
        textStack.Children.Add(lbl);
        if (sublabel is not null)
        {
            var sl = new TextBlock { Text = sublabel, FontSize = 9 };
            sl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            textStack.Children.Add(sl);
        }
        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        // Collision toggle — icon reflects state:
        //   no collision  →  ○  (dim)
        //   solid         →  ⬛  (warning color)
        //   with action   →  ⚡  (primary color, action name in sublabel)
        string collIcon  = layer.HasCollision
            ? (string.IsNullOrEmpty(layer.CollisionAction) ? "⬛" : "⚡")
            : "○";
        string collColor = layer.HasCollision
            ? (string.IsNullOrEmpty(layer.CollisionAction) ? "BrushWarning" : "BrushPrimary")
            : "BrushOnSurfaceVariant";

        var collBtn = new TextBlock
        {
            Text              = collIcon,
            FontSize          = 10,
            Cursor            = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(6, 0, 4, 0),
            ToolTip           = layer.HasCollision
                ? $"Collision: {layer.CollisionActionLabel} (click to toggle)"
                : "No collision (click to enable)"
        };
        collBtn.SetResourceReference(TextBlock.ForegroundProperty, collColor);
        collBtn.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            layer.HasCollision = !layer.HasCollision;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
            // Refresh properties panel if this layer is currently selected
            if (_selectedItem == layer) BuildPropertiesPanel(layer);
        };
        Grid.SetColumn(collBtn, 1);
        grid.Children.Add(collBtn);

        // ✍ Edit button — opens PlaneEditor for this layer
        var editBtn = new TextBlock
        {
            Text              = "✍",
            FontSize          = 11,
            Cursor            = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 4, 0)
        };
        editBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        editBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; OpenTilemapEditor(layer); };
        Grid.SetColumn(editBtn, 2);
        grid.Children.Add(editBtn);

        // 👀 Visibility toggle
        var visBtn = new TextBlock
        {
            Text              = layer.Visible ? "👀" : "□",
            FontSize          = 10,
            Cursor            = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 4, 0)
        };
        visBtn.SetResourceReference(TextBlock.ForegroundProperty,
            layer.Visible ? "BrushOnSurface" : "BrushOnSurfaceVariant");
        visBtn.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            layer.Visible = !layer.Visible;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
            RefreshPreview();
        };
        Grid.SetColumn(visBtn, 3);
        grid.Children.Add(visBtn);

        // ✖ Delete button
        var delBtn = new TextBlock
        {
            Text              = "✖",
            FontSize          = 10,
            Cursor            = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        delBtn.MouseEnter += (_, _) => delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
        delBtn.MouseLeave += (_, _) => delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        delBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; RemovePlaneLayer(layer); };
        Grid.SetColumn(delBtn, 4);
        grid.Children.Add(delBtn);

        row.Child = grid;

        // Single-click — select + show properties
        row.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
                StartInlineLayerRename(row, lbl, layer);
            }
            else if (e.Source is not TextBlock)
            {
                SelectItem(layer);
                ShowPropertiesForItem(layer);
            }
        };

        ProjectTreePanel.Children.Add(row);
    }

    private void StartInlineLayerRename(Border row, TextBlock lbl, PlaneLayerData layer)
    {
        var textBox = new TextBox
        {
            Text              = layer.LayerName,
            FontSize          = 11,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth          = 80
        };
        textBox.SetResourceReference(TextBox.StyleProperty, "RetruxelTextBox");
        textBox.SelectAll();

        // Swap label for textbox inside the row's grid
        var grid = (Grid)row.Child;
        var oldStack = (StackPanel)grid.Children[0];
        grid.Children.RemoveAt(0);
        Grid.SetColumn(textBox, 0);
        grid.Children.Insert(0, textBox);
        textBox.Focus();

        void Confirm()
        {
            var newName = textBox.Text.Trim();
            layer.LayerName = string.IsNullOrEmpty(newName) ? string.Empty : newName;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
        }

        textBox.KeyDown  += (_, e) =>
        {
            if (e.Key == Key.Return) { Confirm(); e.Handled = true; }
            if (e.Key == Key.Escape) { RebuildProjectTree(); e.Handled = true; }
        };
        textBox.LostFocus += (_, _) => Confirm();
    }

    // ── SCENE MODULES ──────────────────────────────────────────────────────────

    private void BuildSceneModulesSection(SceneData scene)
    {
        AddTreeSubSection("MODULES", "modules_scene", indent: 3,
            sublabel: "scene overrides",
            onAdd:    () => ShowModulePickerDialog(isGlobal: false));
        if (!IsExpanded("modules_scene")) return;

        if (scene.ModuleOverrides.Count == 0)
        {
            AddTreeEmpty("No scene overrides", indent: 4);
            return;
        }

        foreach (var mod in scene.ModuleOverrides)
        {
            var label = string.IsNullOrEmpty(mod.Label) ? mod.ModuleId : mod.Label;
            if (!mod.Enabled) label += "  · disabled";

            AddTreeLeafWithEdit(
                label:    label,
                sublabel: mod.ModuleId,
                indent:   4,
                onEdit:   () => { SelectItem(mod); ShowPropertiesForItem(mod); },
                onDelete: () => RemoveProjectModule(mod, isGlobal: false));
        }
    }

    // ── ENTITIES ───────────────────────────────────────────────────────────────

    private void BuildEntitiesSection(SceneData scene)
    {
        AddTreeSubSection("ENTITIES", "entities", indent: 3,
            onAdd: () => ShowPrefabPickerDialog());

        if (IsExpanded("entities"))
        {
            // Group entities by PrefabId (or legacy EntityType) — each type is a collapsible parent node.
            var byType = scene.Entities
                .GroupBy(e => !string.IsNullOrEmpty(e.PrefabId) ? e.PrefabId
                            : !string.IsNullOrEmpty(e.EntityType) ? e.EntityType
                            : "entity")
                .ToList();

            if (byType.Count == 0)
                AddTreeEmpty("No entities", indent: 4);
            else
                foreach (var group in byType)
                    BuildEntityTypeGroup(group.Key, group.ToList(), scene, indent: 4);
        }

        var maxSprites  = _target?.Specs.MaxSpritesOnScreen ?? 64;
        var usedSprites = scene.Entities
            .Where(e => !string.IsNullOrEmpty(e.SpriteAssetId ?? e.PrefabId))
            .Sum(e =>
            {
                var prefab = _project?.Prefabs.FirstOrDefault(p => p.PrefabId == e.PrefabId);
                return (prefab?.WidthTiles ?? e.WidthTiles ?? 2) *
                       (prefab?.HeightTiles ?? e.HeightTiles ?? 2);
            });
        BuildSpriteUsageBar(usedSprites, maxSprites, indent: 3);
    }

    /// <summary>
    /// Renders one entity type group: a collapsible parent row showing the type name,
    /// asset and dimensions, followed by variant rows (one per EntityData instance).
    /// </summary>
    private void BuildEntityTypeGroup(string entityType, List<EntityData> variants,
        SceneData scene, int indent)
    {
        // Use the first variant to read shared type-level data (asset, dimensions).
        var first = variants[0];
        var typeKey = $"entity_type_{entityType}";

        // ── Parent row ────────────────────────────────────────────────────────
        var parentRow = new Border
        {
            Padding = new Thickness(indent * 8, 3, 8, 3),
            Cursor  = Cursors.Hand
        };

        var parentGrid = new Grid();
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✍ sprite editor
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // + variant
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✖ delete type

        // Arrow + type name + sublabel
        var typeStack = new StackPanel();
        var typeHeader = new StackPanel { Orientation = Orientation.Horizontal };
        var arrow = new TextBlock
        {
            Text = IsExpanded(typeKey) ? "▼" : "▶",
            FontSize = 8, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 5, 0)
        };
        arrow.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        var typeLbl = new TextBlock { Text = entityType, FontSize = 11 };
        typeLbl.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        typeLbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurface");
        typeHeader.Children.Add(arrow);
        typeHeader.Children.Add(typeLbl);
        typeStack.Children.Add(typeHeader);

        var assetSublabel = new TextBlock
        {
            Text = string.IsNullOrEmpty(first.SpriteAssetId ?? first.PrefabId)
                ? "no asset assigned"
                : $"{first.SpriteAssetId ?? first.PrefabId}  ·  {(first.WidthTiles ?? 2) * 8}×{(first.HeightTiles ?? 2) * 8}px",
            FontSize = 9
        };
        assetSublabel.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        typeStack.Children.Add(assetSublabel);
        Grid.SetColumn(typeStack, 0);
        parentGrid.Children.Add(typeStack);

        // ✍ Open sprite editor for the type (uses first variant's asset)
        var editBtn = MakeIconButton("✍", 11, "BrushPrimary",
            () => OpenEntityEditor(first));
        Grid.SetColumn(editBtn, 1);
        parentGrid.Children.Add(editBtn);

        // + Add variant
        var addVariantBtn = MakeIconButton("+", 13, "BrushPrimary",
            () => AddEntityVariant(entityType, first, scene));
        Grid.SetColumn(addVariantBtn, 2);
        parentGrid.Children.Add(addVariantBtn);

        // ✖ Delete entire type (all variants)
        var delTypeBtn = MakeIconButton("✖", 10, "BrushOnSurfaceVariant",
            () => RemoveEntityType(entityType, variants, scene), hoverColor: "BrushError");
        Grid.SetColumn(delTypeBtn, 3);
        parentGrid.Children.Add(delTypeBtn);

        parentRow.Child = parentGrid;
        parentRow.MouseLeftButtonDown += (_, e) =>
        {
            if (e.Source is TextBlock tb && (tb.Text == "✍" || tb.Text == "+" || tb.Text == "✖"))
                return;
            ToggleExpand(typeKey);
            RebuildProjectTree();
            e.Handled = true;
        };

        // Click on type row → show type-level properties
        parentRow.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 1 && e.Source is not TextBlock)
            {
                SelectItem(first);
                ShowPropertiesForItem(first);
            }
        };

        ProjectTreePanel.Children.Add(parentRow);

        // ── Variant rows ──────────────────────────────────────────────────────
        if (!IsExpanded(typeKey)) return;

        foreach (var variant in variants)
            BuildEntityVariantRow(variant, indent + 1);
    }

    /// <summary>
    /// Renders one variant row under an entity type group.
    /// Shows: label | palette slot badge | 👀 | ⚙ | ✖
    /// </summary>
    private void BuildEntityVariantRow(EntityData entity, int indent)
    {
        var row = new Border { Padding = new Thickness(indent * 8, 2, 8, 2), Cursor = Cursors.Hand };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // palette badge
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 👀
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ⚙
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✖

        // Label + position sublabel
        var textStack = new StackPanel();
        var lbl = new TextBlock { Text = entity.Label, FontSize = 11 };
        lbl.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        lbl.SetResourceReference(TextBlock.ForegroundProperty,
            entity.Visible ? "BrushOnSurface" : "BrushOnSurfaceVariant");
        textStack.Children.Add(lbl);

        var posLabel = new TextBlock
        {
            Text = $"x:{entity.StartTileX}  y:{entity.StartTileY}",
            FontSize = 9
        };
        posLabel.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        textStack.Children.Add(posLabel);
        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        // Palette slot badge
        var paletteBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(4, 1, 4, 1),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var paletteLbl = new TextBlock
        {
            Text = $"P{entity.PaletteSlot ?? 1}",
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center
        };
        paletteLbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        paletteBadge.Child = paletteLbl;
        Grid.SetColumn(paletteBadge, 1);
        grid.Children.Add(paletteBadge);

        // 👀 Visibility toggle
        var visBtn = MakeIconButton(entity.Visible ? "👀" : "□", 10,
            entity.Visible ? "BrushOnSurface" : "BrushOnSurfaceVariant",
            () =>
            {
                entity.Visible = !entity.Visible;
                _projectManager?.MarkDirty();
                RebuildProjectTree();
                RefreshPreview();
            });
        Grid.SetColumn(visBtn, 2);
        grid.Children.Add(visBtn);

        // ⚙ Properties
        var propBtn = MakeIconButton("⚙", 11, "BrushOnSurfaceVariant",
                    () => { SelectItem(entity); ShowPropertiesForItem(entity); });
        Grid.SetColumn(propBtn, 3);
        grid.Children.Add(propBtn);

        // ✖ Delete variant
        var delBtn = MakeIconButton("✖", 10, "BrushOnSurfaceVariant",
            () => RemoveEntity(entity), hoverColor: "BrushError");
        Grid.SetColumn(delBtn, 4);
        grid.Children.Add(delBtn);

        row.Child = grid;
        row.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) { e.Handled = true; StartInlineEntityRename(row, lbl, entity); }
            else if (e.Source is not TextBlock)
            {
                SelectItem(entity);
                ShowPropertiesForItem(entity);
            }
        };
        ProjectTreePanel.Children.Add(row);
    }

    // ── SCENE BEHAVIORS ─────────────────────────────────────────────────────

    private void BuildSceneBehaviorsSection(SceneData scene)
    {
        AddTreeSubSection("SCENE BEHAVIORS", "scene_behaviors", indent: 3,
            sublabel: "applied to all entities",
            onAdd: () => ShowSceneActionPickerDialog(scene));

        if (!IsExpanded("scene_behaviors")) return;

        if (scene.SceneBehaviors.Count == 0)
        {
            AddTreeEmpty("No scene behaviors — click + to add", indent: 4);
            return;
        }

        foreach (var action in scene.SceneBehaviors)
        {
            var a = action;
            var def = _actionRegistry?.GetById(a.ActionId);
            var displayName = def?.DisplayName ?? a.ActionId;
            var scopeLabel  = def?.Scope == "entity" ? "per entity" : "scene";

            AddTreeLeafWithEdit(
                label:    displayName,
                sublabel: $"{a.ActionId}  ·  {scopeLabel}",
                indent:   4,
                onEdit:   def is not null ? () =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        var dialog = new Retruxel.Tool.PrefabEditor.ActionParameterDialog(
                            a, def, Window.GetWindow(this));
                        if (dialog.ShowDialog() == true)
                            _projectManager?.MarkDirty();
                    }, System.Windows.Threading.DispatcherPriority.Input);
                } : null,
                onDelete: () =>
                {
                    scene.SceneBehaviors.Remove(a);
                    _projectManager?.MarkDirty();
                    RebuildProjectTree();
                });
        }
    }

    private void ShowSceneActionPickerDialog(SceneData scene)
    {
        if (_actionRegistry is null) return;

        // Only show actions with scope="scene" or those explicitly designed for scene use
        var available = _actionRegistry.Actions.Values
            .Where(d => d.Scope == "scene" || d.Scope == "entity") // all actions can be scene-scoped
            .Where(d => !scene.SceneBehaviors.Any(a =>
                a.ActionId.Equals(d.ActionId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(d => d.Category)
            .ThenBy(d => d.DisplayName)
            .ToList();

        if (available.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "All available actions are already added to this scene.",
                "Retruxel", System.Windows.MessageBoxButton.OK);
            return;
        }

        // Simple picker: context menu with available actions
        var menu = new ContextMenu { IsOpen = true };
        foreach (var def in available)
        {
            var d = def;
            var item = new MenuItem
            {
                Header = $"{d.DisplayName}  ({d.ActionId})",
                Tag    = d
            };
            item.Click += (_, _) =>
            {
                scene.SceneBehaviors.Add(new ActionInstance { ActionId = d.ActionId });
                _projectManager?.MarkDirty();
                _expandedNodes.Add("scene_behaviors");
                RebuildProjectTree();
            };
            menu.Items.Add(item);
        }
        menu.PlacementTarget = ProjectTreePanel;
        menu.IsOpen = true;
    }

    private void BuildVramUsageBar(SceneData scene)
    {
        if (_project is null || _target is null) return;

        var fontTileCount = _project.Modules
            .Concat(_currentScene?.ModuleOverrides ?? [])
            .Any(m => m.ModuleId == "text.display" || m.ModuleId == "text.array")
            ? 128 : 0;

        var report = VramAllocator.Analyze(scene, _target, _project.Assets, fontTileCount);

        var ratio    = report.TotalBytesAvailable > 0
            ? Math.Min((double)report.TotalBytesUsed / report.TotalBytesAvailable, 1.0)
            : 0;
        var colorKey = ratio < 0.5 ? "BrushSuccess" : ratio < 0.75 ? "BrushWarning" : "BrushError";
        var prefix = ratio >= 1.0 ? "✖ " : ratio >= 0.75 ? "⚠ " : "";

        var usedKb = report.TotalBytesUsed / 1024.0;
        var maxKb  = report.TotalBytesAvailable / 1024.0;
        var countText = $"  {usedKb:F1} KB / {maxKb:F1} KB";

        var container = new StackPanel { Margin = new Thickness(2 * 8, 4, 8, 4) };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
        var prefixBlock = new TextBlock { Text = $"{prefix}VRAM:", FontSize = 9 };
        prefixBlock.SetResourceReference(TextBlock.ForegroundProperty,
            ratio >= 0.75 ? colorKey : "BrushOnSurfaceVariant");
        var countBlock = new TextBlock { Text = countText, FontSize = 9 };
        countBlock.SetResourceReference(TextBlock.ForegroundProperty,
            ratio >= 0.75 ? colorKey : "BrushOnSurfaceVariant");
        header.Children.Add(prefixBlock);
        header.Children.Add(countBlock);
        container.Children.Add(header);

        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = report.TotalBytesAvailable,
            Value   = report.TotalBytesUsed,
            Height  = 4
        };
        bar.SetResourceReference(ProgressBar.ForegroundProperty, colorKey);
        container.Children.Add(bar);

        ProjectTreePanel.Children.Add(container);
    }

    private void BuildSpriteUsageBar(int used, int max, int indent)
    {
        var ratio = max > 0 ? Math.Min((double)used / max, 1.0) : 0;
        var colorKey = ratio < 0.5 ? "BrushSuccess" : ratio < 0.75 ? "BrushWarning" : "BrushError";
        var prefix = ratio >= 1.0 ? "✖ " : ratio >= 0.75 ? "⚠ " : "";

        var container = new StackPanel { Margin = new Thickness(indent * 8, 4, 8, 4) };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };

        var prefixBlock = new TextBlock { Text = $"{prefix}Sprites:", FontSize = 9 };
        prefixBlock.SetResourceReference(TextBlock.ForegroundProperty,
            ratio >= 0.75 ? colorKey : "BrushOnSurfaceVariant");

        var countBlock = new TextBlock { Text = $"  {used} / {max}", FontSize = 9 };
        countBlock.SetResourceReference(TextBlock.ForegroundProperty,
            ratio >= 0.75 ? colorKey : "BrushOnSurfaceVariant");

        header.Children.Add(prefixBlock);
        header.Children.Add(countBlock);
        container.Children.Add(header);

        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = max,
            Value   = used,
            Height  = 4
        };
        bar.SetResourceReference(ProgressBar.ForegroundProperty, colorKey);
        container.Children.Add(bar);

        ProjectTreePanel.Children.Add(container);
    }

    private void StartInlineEntityRename(Border row, TextBlock lbl, EntityData entity)
    {
        var textBox = new TextBox
        {
            Text = entity.Label,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 80
        };
        textBox.SetResourceReference(TextBox.StyleProperty, "RetruxelTextBox");
        textBox.SelectAll();

        var grid = (Grid)row.Child;
        grid.Children.RemoveAt(0);
        Grid.SetColumn(textBox, 0);
        grid.Children.Insert(0, textBox);
        textBox.Focus();

        void Confirm()
        {
            var name = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(name)) entity.Label = name;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
        }

        textBox.KeyDown  += (_, e) => { if (e.Key == Key.Return) { Confirm(); e.Handled = true; } if (e.Key == Key.Escape) { RebuildProjectTree(); e.Handled = true; } };
        textBox.LostFocus += (_, _) => Confirm();
    }

    private TextBlock MakeIconButton(string icon, int fontSize, string colorKey,
        Action onClick, string? hoverColor = null)
    {
        var btn = new TextBlock
        {
            Text = icon, FontSize = fontSize, Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        btn.SetResourceReference(TextBlock.ForegroundProperty, colorKey);
        if (hoverColor is not null)
        {
            btn.MouseEnter += (_, _) => btn.SetResourceReference(TextBlock.ForegroundProperty, hoverColor);
            btn.MouseLeave += (_, _) => btn.SetResourceReference(TextBlock.ForegroundProperty, colorKey);
        }
        btn.MouseLeftButtonDown += (_, e) => { e.Handled = true; onClick(); };
        return btn;
    }

    // ── TREE PRIMITIVES ────────────────────────────────────────────────────────

    private void AddTreeSection(string text, string key)
    {
        var header = new Border { Padding = new Thickness(8, 6, 8, 4), Cursor = Cursors.Hand };
        var label  = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold };
        label.SetResourceReference(TextBlock.StyleProperty, "TextLabelCaps");
        label.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        header.Child = label;
        header.MouseLeftButtonDown += (_, e) => { ToggleExpand(key); RebuildProjectTree(); e.Handled = true; };
        ProjectTreePanel.Children.Add(header);
    }

    private void AddTreeSubSection(string text, string key, int indent,
        string? sublabel = null, Action? onAdd = null)
    {
        var row = new Border { Padding = new Thickness(indent * 8, 4, 8, 2), Cursor = Cursors.Hand };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (onAdd != null)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Orientation = Orientation.Horizontal };
        var arrow = new TextBlock { Text = IsExpanded(key) ? "▼" : "▶", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
        arrow.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");

        var lbl = new TextBlock { Text = text, FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        lbl.SetResourceReference(TextBlock.StyleProperty, "TextLabelCaps");
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        textStack.Children.Add(arrow);
        textStack.Children.Add(lbl);

        if (sublabel != null)
        {
            var sl = new TextBlock { Text = $"  {sublabel}", FontSize = 9, VerticalAlignment = VerticalAlignment.Center };
            sl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            textStack.Children.Add(sl);
        }

        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        if (onAdd != null)
        {
            var addBtn = new TextBlock { Text = "+", FontSize = 14, Width = 20, TextAlignment = TextAlignment.Center, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
            addBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
            addBtn.MouseLeftButtonDown += (s, e) => { onAdd.Invoke(); e.Handled = true; };
            Grid.SetColumn(addBtn, 1);
            grid.Children.Add(addBtn);
        }

        row.Child = grid;
        row.MouseLeftButtonDown += (_, e) =>
        {
            if (e.Source is TextBlock tb && tb.Text == "+") return;
            ToggleExpand(key);
            RebuildProjectTree();
            e.Handled = true;
        };
        ProjectTreePanel.Children.Add(row);
    }

    private void AddTreeLeaf(string label, string? sublabel, int indent, Action? onClick, Action? onDelete)
    {
        var row = BuildTreeRow(indent, null, label, sublabel,
            (Brush)FindResource("BrushOnSurface"),
            onExpand: null, onAdd: null, onEdit: null, onDelete: onDelete);
        if (onClick != null)
            row.MouseLeftButtonDown += (_, e) => { onClick(); e.Handled = true; };
        ProjectTreePanel.Children.Add(row);
    }

    private void AddTreeLeafWithEdit(string label, string? sublabel, int indent, Action? onEdit, Action? onDelete)
    {
        var row = BuildTreeRow(indent, null, label, sublabel,
            (Brush)FindResource("BrushOnSurface"),
            onExpand: null, onAdd: null, onEdit: onEdit, onDelete: onDelete);
        ProjectTreePanel.Children.Add(row);
    }

    private void AddTreeEmpty(string text, int indent)
    {
        var t = new TextBlock { Text = text, FontSize = 10, Margin = new Thickness(indent * 8, 2, 8, 2) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        ProjectTreePanel.Children.Add(t);
    }

    private Border BuildTreeRow(int indent, string? expandKey, string label, string? sublabel,
        Brush foreground, Action? onExpand, Action? onAdd, Action? onEdit, Action? onDelete)
    {
        var row = new Border
        {
            Padding = new Thickness(indent * 8, 3, 8, 3),
            Cursor  = onEdit != null || onExpand != null ? Cursors.Hand : Cursors.Arrow
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (onEdit   != null) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        if (onDelete != null) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        int col = 0;

        var textStack = new StackPanel();
        var lbl = new TextBlock { Text = label, FontSize = 11, Foreground = foreground };
        lbl.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        textStack.Children.Add(lbl);

        if (!string.IsNullOrEmpty(sublabel))
        {
            var sl = new TextBlock { Text = sublabel, FontSize = 9 };
            sl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            textStack.Children.Add(sl);
        }

        Grid.SetColumn(textStack, col++);
        grid.Children.Add(textStack);

        if (onEdit != null)
        {
            var editBtn = new TextBlock { Text = "···", FontSize = 11, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 4, 0) };
            editBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
            editBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; onEdit(); };
            Grid.SetColumn(editBtn, col++);
            grid.Children.Add(editBtn);
        }

        if (onDelete != null)
        {
            var del = new TextBlock { Text = "✖", FontSize = 10, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
            del.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            del.MouseEnter += (_, _) => del.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
            del.MouseLeave += (_, _) => del.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            del.MouseLeftButtonDown += (_, e) => { e.Handled = true; onDelete(); };
            Grid.SetColumn(del, col);
            grid.Children.Add(del);
        }

        row.Child = grid;
        return row;
    }

    // ── Expand / collapse ──────────────────────────────────────────────────────

    private bool IsExpanded(string key) => _expandedNodes.Contains(key);

    private void ToggleExpand(string key)
    {
        if (_expandedNodes.Contains(key)) _expandedNodes.Remove(key);
        else _expandedNodes.Add(key);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void AddMenuItemTo(ContextMenu menu, string header, Action action, bool disabled = false)
    {
        var item = new MenuItem { Header = header, IsEnabled = !disabled };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private Brush ParseHexBrush(string hex)
    {
        try { return (Brush)new BrushConverter().ConvertFromString(hex)!; }
        catch { return Brushes.Black; }
    }

    private void ShowModulePickerDialog(bool isGlobal)
    {
        if (_moduleRegistry is null || _project is null || _currentScene is null) return;

        var scope = isGlobal ? ModuleScope.Project : ModuleScope.Scene;

        // IDs already present at the target level
        var existing = isGlobal
            ? _project.Modules.Select(m => m.ModuleId).ToList()
            : _currentScene.ModuleOverrides.Select(m => m.ModuleId).ToList();

        // IDs already added at project level (for OVERRIDE badge in scene context)
        var projectModules = _project.Modules.Select(m => m.ModuleId).ToList();

        // Defer to after the current mouse event is fully processed.
        // ShowDialog() called directly from MouseLeftButtonDown can cause
        // a Win32 reentrancy issue when the owner has AllowsTransparency=True.
        Dispatcher.BeginInvoke(() =>
        {
            ModulePickerWindow.Open(
                registry:       _moduleRegistry,
                scope:          scope,
                existing:       existing,
                projectModules: projectModules,
                onSelected:     moduleId => AddModuleAndOpenProperties(moduleId, isGlobal),
                owner:          Window.GetWindow(this));
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void AddModuleAndOpenProperties(string moduleId, bool isGlobal)
    {
        AddProjectModule(moduleId, isGlobal);

        // Open properties panel for the newly added module
        Dispatcher.InvokeAsync(() =>
        {
            var mod = isGlobal
                ? _project?.Modules.LastOrDefault(m => m.ModuleId == moduleId)
                : _currentScene?.ModuleOverrides.LastOrDefault(m => m.ModuleId == moduleId);

            if (mod is not null)
            {
                SelectItem(mod);
                ShowPropertiesForItem(mod);
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void ShowPrefabPickerDialog()
    {
        if (_project is null || _currentScene is null) return;

        Dispatcher.BeginInvoke(() =>
        {
            var picker = new Retruxel.Tool.PrefabEditor.PrefabPickerDialog(
                _project,
                onSelected: prefabId =>
                {
                    AddEntityFromPrefab(prefabId);
                },
                onCreateNew: (prefabId, displayName, assetId, w, h) =>
                {
                    var newPrefab = new PrefabData
                    {
                        PrefabId      = prefabId,
                        DisplayName   = displayName,
                        SpriteAssetId = assetId,
                        WidthTiles    = w,
                        HeightTiles   = h
                    };
                    _project.Prefabs.Add(newPrefab);
                    _projectManager?.MarkDirty();
                    AddEntityFromPrefab(prefabId);
                    RebuildProjectTree();
                },
                owner: Window.GetWindow(this));
            picker.ShowDialog();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void AddEntityFromPrefab(string prefabId)
    {
        if (_currentScene is null) return;

        var entity = new EntityData
        {
            EntityId = Guid.NewGuid().ToString(),
            Label    = prefabId,
            PrefabId = prefabId,
            Visible  = true
        };

        _currentScene.Entities.Add(entity);
        _projectManager?.MarkDirty();
        RebuildProjectTree();
        SelectItem(entity);
        ShowPropertiesForItem(entity);
    }
}
