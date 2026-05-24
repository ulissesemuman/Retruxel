using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Right panel — builds and refreshes property editors for the selected element.
/// </summary>
public partial class SceneEditorView
{
    internal void BuildPropertiesPanel(object item)
    {
        PropertiesPanel.Children.Clear();

        switch (item)
        {
            case PlaneLayerData layer:
                BuildPlaneLayerProperties(layer);
                break;
            case EntityData entity:
                BuildEntityProperties(entity);
                break;
            case PaletteSlotData slot:
                PropertiesPanel.Children.Add(new TextBlock
                {
                    Text         = $"Palette slot {slot.SlotIndex} — click EDIT in the tree to change colors.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize     = 10,
                    Margin       = new Thickness(0, 0, 0, 8)
                });
                break;
            case ProjectModuleData mod:
                BuildModuleProperties(mod);
                break;
        }
    }

    private void BuildPlaneLayerProperties(PlaneLayerData layer)
    {
        AddPropertyLabel("PLANE LAYER");
        AddPropertyRow("LayerName", layer.LayerName, val => { layer.LayerName = val; _projectManager?.MarkDirty(); RebuildProjectTree(); });
        AddPropertyRow("Asset ID", layer.AssetId, val => { layer.AssetId = val; _projectManager?.MarkDirty(); RefreshPreview(); });

        // PaletteSlot is a property of PlaneData (the hardware plane), not of the layer.
        // Find the parent plane and expose its PaletteSlot here as a convenience.
        var parentPlane = _currentScene?.Planes.FirstOrDefault(p => p.Layers.Contains(layer));
        if (parentPlane is not null)
        {
            AddPropertyRow("Palette Slot (plane)", parentPlane.PaletteSlot.ToString(), val =>
            {
                if (int.TryParse(val, out var s)) { parentPlane.PaletteSlot = s; _projectManager?.MarkDirty(); RefreshPreview(); }
            });
        }
    }

    private void BuildEntityProperties(EntityData entity)
    {
        AddPropertyLabel("ENTITY");
        AddPropertyRow("LayerName", entity.Label, val => { entity.Label = val; _projectManager?.MarkDirty(); RebuildProjectTree(); });
        AddPropertyRow("Type", entity.EntityType, val => { entity.EntityType = val; _projectManager?.MarkDirty(); });
        AddPropertyRow("Sprite Asset", entity.SpriteAssetId, val => { entity.SpriteAssetId = val; _projectManager?.MarkDirty(); RefreshPreview(); });
        AddPropertyRow("Start X (tile)", entity.StartTileX.ToString(), val =>
        {
            if (int.TryParse(val, out var x)) { entity.StartTileX = x; _projectManager?.MarkDirty(); RefreshPreview(); }
        });
        AddPropertyRow("Start Y (tile)", entity.StartTileY.ToString(), val =>
        {
            if (int.TryParse(val, out var y)) { entity.StartTileY = y; _projectManager?.MarkDirty(); RefreshPreview(); }
        });
    }

    private void BuildModuleProperties(ProjectModuleData mod)
    {
        AddPropertyLabel($"MODULE — {mod.ModuleId.ToUpper()}");
        AddPropertyRow("LayerName", mod.Label, val => { mod.Label = val; _projectManager?.MarkDirty(); RebuildProjectTree(); });
        AddPropertyRow("Enabled", mod.Enabled.ToString(), val =>
        {
            if (bool.TryParse(val, out var b)) { mod.Enabled = b; _projectManager?.MarkDirty(); RebuildProjectTree(); }
        });
    }

    private void AddPropertyLabel(string text)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text       = text,
            FontSize   = 10,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 12)
        });
    }

    private void AddPropertyRow(string label, string value, Action<string> onChange)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text   = label,
            Style  = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Height     = 32,
            Margin     = new Thickness(0, 0, 0, 12)
        };

        var textBox = new TextBox
        {
            Text              = value,
            Foreground        = Brushes.White,
            FontFamily        = new FontFamily("Consolas"),
            FontSize          = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        textBox.LostFocus += (_, _) => onChange(textBox.Text);
        border.Child = textBox;
        PropertiesPanel.Children.Add(border);
    }

    // ── Palette slot editor ───────────────────────────────────────────────────

    private void OpenPaletteSlotEditor(int slotIndex)
    {
        if (_currentScene is null || _target is null) return;

        var slot   = _currentScene.PaletteSlots[slotIndex];
        var window = new Retruxel.Tool.PaletteEditor.PaletteEditorWindow(_target, slot)
        {
            Owner = Window.GetWindow(this),
            Title = $"Palette — Slot {slotIndex} ({_target.GetPaletteSlotType(slotIndex)})"
        };

        if (window.ShowDialog() == true)
        {
            RebuildProjectTree();
            RefreshPreview();
        }
    }
}
