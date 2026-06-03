using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    // ── Plane layer ───────────────────────────────────────────────────────────

    private void BuildPlaneLayerProperties(PlaneLayerData layer)
    {
        AddPropertyLabel("PLANE LAYER");
        AddPropertyRow("LayerName", layer.LayerName, val =>
        {
            layer.LayerName = val;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
        });
        AddPropertyRow("Asset ID", layer.AssetId, val =>
        {
            layer.AssetId = val;
            _projectManager?.MarkDirty();
            RefreshPreview();
        });

        // PaletteSlot belongs to PlaneData (hardware plane), not the layer.
        // Walk up to the parent plane and expose it as a combo.
        var parentPlane = _currentScene?.Planes.FirstOrDefault(p => p.Layers.Contains(layer));
        if (parentPlane is not null && _target is not null)
        {
            var paletteOptions = BuildPaletteSlotOptions(_target);
            AddPropertyCombo("Palette Slot (plane)", paletteOptions, parentPlane.PaletteSlot.ToString(), val =>
            {
                if (int.TryParse(val, out var s))
                {
                    parentPlane.PaletteSlot = s;
                    _projectManager?.MarkDirty();
                    RefreshPreview();
                }
            });
        }
    }

    // ── Entity ────────────────────────────────────────────────────────────────

    private void BuildEntityProperties(EntityData entity)
    {
        // Resolve Prefab for type-level properties
        var prefab = _project?.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId);
        var prefabId = entity.PrefabId ?? entity.EntityType ?? "entity";

        AddPropertyLabel($"PREFAB — {prefabId.ToUpper()}");

        // Open Prefab Editor button
        if (prefab is not null && _target is not null && _actionRegistry is not null)
        {
            var openBtn = new System.Windows.Controls.Button
            {
                Content = "✏ EDIT PREFAB",
                Height  = 32,
                Margin  = new System.Windows.Thickness(0, 0, 0, 12)
            };
            openBtn.SetResourceReference(System.Windows.Controls.Button.StyleProperty, "ButtonSecondary");
            openBtn.Click += (_, _) => OpenPrefabEditor(prefab);
            PropertiesPanel.Children.Add(openBtn);
        }

        AddPropertyLabel("INSTANCE");

        AddPropertyRow("Label", entity.Label, val =>
        {
            entity.Label = val;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
        });

        AddPropertyRow("Start X (tile)", entity.StartTileX.ToString(), val =>
        {
            if (int.TryParse(val, out var x)) { entity.StartTileX = x; _projectManager?.MarkDirty(); RefreshPreview(); }
        });

        AddPropertyRow("Start Y (tile)", entity.StartTileY.ToString(), val =>
        {
            if (int.TryParse(val, out var y)) { entity.StartTileY = y; _projectManager?.MarkDirty(); RefreshPreview(); }
        });
    }

    // ── Module (manifest-driven) ──────────────────────────────────────────────

    private void BuildModuleProperties(ProjectModuleData mod)
    {
        AddPropertyLabel($"MODULE — {mod.ModuleId.ToUpper()}");

        AddPropertyRow("Label", mod.Label, val =>
        {
            mod.Label = val;
            _projectManager?.MarkDirty();
            RebuildProjectTree();
        });

        AddPropertyCombo("Enabled", new Dictionary<string, string>
        {
            { "Yes", "True" },
            { "No",  "False" }
        }, mod.Enabled.ToString(), val =>
        {
            if (bool.TryParse(val, out var b)) { mod.Enabled = b; _projectManager?.MarkDirty(); RebuildProjectTree(); }
        });

        // Resolve the live module instance to read its manifest
        if (_moduleRegistry is null) return;
        var manifest = ResolveManifest(_moduleRegistry, mod.ModuleId);
        if (manifest is null || manifest.Parameters.Length == 0) return;

        AddPropertyLabel("PARAMETERS");

        // Re-serialize to a dict for easy key lookup
        var stateDict = ParseStateDict(mod.State);

        foreach (var param in manifest.Parameters)
        {
            stateDict.TryGetValue(param.Name, out var currentRaw);
            var currentValue = currentRaw ?? param.DefaultValue?.ToString() ?? string.Empty;

            switch (param.Type)
            {
                case ParameterType.Enum:
                    AddPropertyCombo(param.DisplayName, param.EnumOptions, currentValue, val =>
                        UpdateModuleParam(mod, param.Name, val));
                    break;

                case ParameterType.Bool:
                    AddPropertyCombo(param.DisplayName, new Dictionary<string, string>
                    {
                        { "Yes", "true" },
                        { "No",  "false" }
                    }, currentValue.ToLowerInvariant(), val =>
                        UpdateModuleParam(mod, param.Name, val));
                    break;

                case ParameterType.Int:
                case ParameterType.Float:
                case ParameterType.String:
                default:
                    AddPropertyRow(param.DisplayName, currentValue, val =>
                        UpdateModuleParam(mod, param.Name, val));
                    break;
            }
        }
    }

    /// <summary>
    /// Resolves a ModuleManifest from any of the three module registries.
    /// Returns null if the module is not found or has no manifest.
    /// </summary>
    private static ModuleManifest? ResolveManifest(ModuleRegistry registry, string moduleId)
    {
        if (registry.LogicModules.TryGetValue(moduleId, out var lm))
            return lm.GetManifest();
        if (registry.GraphicModules.TryGetValue(moduleId, out var gm))
            return gm.GetManifest();
        if (registry.AudioModules.TryGetValue(moduleId, out var am))
            return am.GetManifest();
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds palette slot options from the target's slot count and types.
    /// Key = display label ("Slot 0 — Sprites"), Value = slot index as string.
    /// </summary>
    private static Dictionary<string, string> BuildPaletteSlotOptions(ITarget target)
    {
        var options = new Dictionary<string, string>();
        int count = target.GetPaletteSlotCount();
        for (int i = 0; i < count; i++)
        {
            var slotType = target.GetPaletteSlotType(i);
            options[$"Slot {i} — {slotType}"] = i.ToString();
        }
        return options;
    }

    /// <summary>
    /// Builds input slot options from the project's input port bindings.
    /// Key = display label ("Controller 1"), Value = port index as string (-1 = none).
    /// </summary>
    private Dictionary<string, string> BuildInputSlotOptions()
    {
        var options = new Dictionary<string, string>
        {
            { "None", "-1" }
        };

        if (_project is null) return options;

        for (int i = 0; i < _project.InputPorts.Count; i++)
        {
            var port = _project.InputPorts[i];
            var label = string.IsNullOrWhiteSpace(port.Label) ? $"Port {i}" : port.Label;
            options[label] = i.ToString();
        }

        return options;
    }

    /// <summary>
    /// Updates a single parameter in a module's serialized state and persists it back to ProjectModuleData.
    /// </summary>
    private void UpdateModuleParam(ProjectModuleData mod, string paramName, string newValue)
    {
        // Read current state dict, update the key, re-serialize
        var dict = ParseStateDict(mod.State);
        dict[paramName] = newValue;

        var json = SerializeStateDict(dict);
        mod.State = JsonDocument.Parse(json).RootElement;

        _projectManager?.MarkDirty();
    }

    /// <summary>Parses a JsonElement state object into a string→string dict.</summary>
    private static Dictionary<string, string> ParseStateDict(JsonElement state)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (state.ValueKind != JsonValueKind.Object) return dict;

        foreach (var prop in state.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.Value.GetRawText(),
                JsonValueKind.True   => "true",
                JsonValueKind.False  => "false",
                _                    => prop.Value.GetRawText()
            };
        }
        return dict;
    }

    /// <summary>Serializes a string→string dict back to a JSON object string.</summary>
    private static string SerializeStateDict(Dictionary<string, string> dict)
    {
        var sb = new System.Text.StringBuilder("{");
        bool first = true;
        foreach (var (key, value) in dict)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append($"\"{JsonEncodedText.Encode(key)}\":");
            // Try to write numbers and booleans without quotes
            if (value is "true" or "false" || double.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                sb.Append(value);
            else
                sb.Append($"\"{JsonEncodedText.Encode(value)}\"");
        }
        sb.Append('}');
        return sb.ToString();
    }

    // ── UI primitives ─────────────────────────────────────────────────────────

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

    /// <summary>
    /// Adds a labeled ComboBox row to the properties panel.
    /// </summary>
    /// <param name="label">Row label shown above the combo.</param>
    /// <param name="options">Display label → internal value pairs.</param>
    /// <param name="currentValue">The internal value that should be selected initially.</param>
    /// <param name="onChange">Called with the internal value when selection changes.</param>
    private void AddPropertyCombo(string label, Dictionary<string, string> options, string currentValue, Action<string> onChange)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text   = label,
            Style  = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var combo = new ComboBox
        {
            Height     = 32,
            Margin     = new Thickness(0, 0, 0, 12),
            FontFamily = new FontFamily("Consolas"),
            FontSize   = 12
        };

        int selectedIndex = 0;
        int idx = 0;
        foreach (var (displayName, internalValue) in options)
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content = displayName,
                Tag     = internalValue
            });

            if (string.Equals(internalValue, currentValue, StringComparison.OrdinalIgnoreCase))
                selectedIndex = idx;

            idx++;
        }

        combo.SelectedIndex = selectedIndex;

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem selected && selected.Tag is string val)
                onChange(val);
        };

        PropertiesPanel.Children.Add(combo);
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
            _projectManager?.MarkDirty();
            RebuildProjectTree();
            RefreshPreview();
        }
    }
}
