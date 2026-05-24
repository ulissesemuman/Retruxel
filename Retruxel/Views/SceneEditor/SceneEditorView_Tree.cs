using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
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
///          forest_tiles      [✕]
///      ▼ MODULES  global     [+ Add]
///          Physics           [···] [✕]
///          Input             [···] [✕]
///      ▼ SCENES              [+ Add]
///          ▶ Scene1  (initial)
///          ▼ Scene2  ← current, expanded
///              ▼ PALETTE     (fixed by target)
///                  BG        [EDIT]
///                  SP        [EDIT]
///              ▼ PLANES    (count fixed by target)
///                  ▼ Plane 0           [+ Add Layer]
///                      Layer 0           [···] [✕]
///              ▼ MODULES  scene overrides [+ Add]
///                  Physics override      [···] [✕]
///              ▼ ENTITIES               [+ Add]
///                  Player               [···] [✕]
///                  Enemy                [···] [✕]
/// </summary>
public partial class SceneEditorView
{
    private readonly HashSet<string> _expandedNodes = new(StringComparer.Ordinal)
    {
        "project", "assets", "modules_global", "scenes",
        "palette", "planes", "modules_scene", "entities"
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
        var sublabel = string.IsNullOrEmpty(layer.AssetId) ? null : layer.AssetId;

        var row = new Border
        {
            Padding = new Thickness(indent * 8, 3, 8, 3),
            Cursor  = Cursors.Hand
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✏
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 👁
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ✕

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

        // ✏ Edit button — opens PlaneEditor for this layer
        var editBtn = new TextBlock
        {
            Text = "✏", FontSize = 11, Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 4, 0)
        };
        editBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        editBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; OpenTilemapEditor(layer); };
        Grid.SetColumn(editBtn, 1);
        grid.Children.Add(editBtn);

        // 👁 Visibility toggle
        var visBtn = new TextBlock
        {
            Text = layer.Visible ? "👁" : "□", FontSize = 10, Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
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
        Grid.SetColumn(visBtn, 2);
        grid.Children.Add(visBtn);

        // ✕ Delete button
        var delBtn = new TextBlock
        {
            Text = "✕", FontSize = 10, Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        delBtn.MouseEnter += (_, _) => delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
        delBtn.MouseLeave += (_, _) => delBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
        delBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; RemovePlaneLayer(layer); };
        Grid.SetColumn(delBtn, 3);
        grid.Children.Add(delBtn);

        row.Child = grid;

        // Double-click — inline rename
        row.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) { e.Handled = true; StartInlineLayerRename(row, lbl, layer); }
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
            onAdd: () => ShowEntityPickerDialog());
        if (!IsExpanded("entities")) return;

        if (scene.Entities.Count == 0)
        {
            AddTreeEmpty("No entities", indent: 4);
            return;
        }

        foreach (var entity in scene.Entities)
        {
            AddTreeLeafWithEdit(
                label:    entity.Label,
                sublabel: entity.EntityType,
                indent:   4,
                onEdit:   () => OpenEntityEditor(entity),
                onDelete: () => RemoveEntity(entity));
        }
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
            var del = new TextBlock { Text = "✕", FontSize = 10, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };
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
        // TODO: replace with a picker window listing available modules
        MessageBox.Show(
            isGlobal ? "Select a module to add as a global default."
                     : "Select a module to add as a scene override.",
            "Add Module", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowEntityPickerDialog()
    {
        // TODO: replace with an entity type picker
        AddEntity("entity");
    }
}
