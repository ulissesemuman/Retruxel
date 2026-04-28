using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Partial class handling properties panel — builds and manages the right sidebar
/// that displays module parameters and User ID field.
/// </summary>
public partial class SceneEditorView
{
    /// <summary>
    /// Builds the properties panel for the selected element.
    /// Generates UI controls for User ID and all module parameters from the manifest.
    /// </summary>
    private void BuildPropertiesPanel(SceneElement element)
    {
        _isUpdatingUI = true;
        PropertiesPanel.Children.Clear();

        if (element.Module is not IModule module)
        {
            _isUpdatingUI = false;
            return;
        }

        ModuleManifest? manifest = module switch
        {
            ILogicModule lm => lm.GetManifest(),
            IGraphicModule gm => gm.GetManifest(),
            IAudioModule am => am.GetManifest(),
            _ => null
        };

        if (manifest is null)
        {
            _isUpdatingUI = false;
            return;
        }

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = manifest.ModuleId.ToUpper().Replace(".", " "),
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(0, 0, 0, 16)
        });

        // User ID field (required)
        AddUserIdField(element);

        foreach (var param in manifest.Parameters)
        {
            AddParameterField(element, module, param);
        }

        _isUpdatingUI = false;
    }

    /// <summary>
    /// Adds the User ID field to the properties panel.
    /// Shows placeholder when empty, validates on blur, supports undo/redo.
    /// </summary>
    private void AddUserIdField(SceneElement element)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "USER ID",
            Style = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var input = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(0),
            Height = 32,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var textBox = new TextBox
        {
            Text = element.UserId,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "placeholder"
        };

        // Show placeholder when empty
        if (string.IsNullOrEmpty(element.UserId))
        {
            var shortId = element.ElementId.Length >= 8 ? element.ElementId[..8] : element.ElementId;
            textBox.Text = $"(auto: {shortId}...)";
            textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
        }

        var validationText = new TextBlock
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };

        string valueOnFocus = element.UserId;

        textBox.GotFocus += (s, e) =>
        {
            valueOnFocus = element.UserId;
            if (string.IsNullOrEmpty(element.UserId))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.White;
            }
        };

        textBox.LostFocus += (s, e) =>
        {
            if (_isUpdatingUI) return;

            var newValue = textBox.Text.Trim();
            var previousValue = valueOnFocus;

            // Restore placeholder if empty
            if (string.IsNullOrEmpty(newValue))
            {
                var shortId = element.ElementId.Length >= 8 ? element.ElementId[..8] : element.ElementId;
                textBox.Text = $"(auto: {shortId}...)";
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                element.UserId = string.Empty;
                validationText.Text = string.Empty;
                UpdateElementLabel(element);
                SyncProjectModules();
                return;
            }

            if (previousValue == newValue) return;

            // Validate
            var validation = ValidateUserId(newValue, element.ElementId);
            if (!validation.IsValid)
            {
                validationText.Text = validation.Error;
                textBox.Text = string.IsNullOrEmpty(previousValue) ? string.Empty : previousValue;
                if (string.IsNullOrEmpty(previousValue))
                {
                    var shortId = element.ElementId.Length >= 8 ? element.ElementId[..8] : element.ElementId;
                    textBox.Text = $"(auto: {shortId}...)";
                    textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                }
                element.UserId = previousValue;
                return;
            }

            validationText.Text = string.Empty;

            // Create state change for User ID change (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = "Change User ID",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    _isUpdatingUI = true;
                    element.UserId = newValue;
                    if (string.IsNullOrEmpty(newValue))
                    {
                        var shortId = element.ElementId.Length >= 8 ? element.ElementId[..8] : element.ElementId;
                        textBox.Text = $"(auto: {shortId}...)";
                        textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                    }
                    else
                    {
                        textBox.Text = newValue;
                        textBox.Foreground = Brushes.White;
                    }
                    _isUpdatingUI = false;
                    UpdateElementLabel(element);
                },
                IsUndoable = true,
                UndoCommand = new ChangePropertyCommand(
                    description: "Change User ID",
                    apply: val =>
                    {
                        _isUpdatingUI = true;
                        element.UserId = val;
                        if (string.IsNullOrEmpty(val))
                        {
                            var shortId = element.ElementId.Length >= 8 ? element.ElementId[..8] : element.ElementId;
                            textBox.Text = $"(auto: {shortId}...)";
                            textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                        }
                        else
                        {
                            textBox.Text = val;
                            textBox.Foreground = Brushes.White;
                        }
                        _isUpdatingUI = false;
                        UpdateElementLabel(element);
                    },
                    previousValue: previousValue,
                    newValue: newValue)
            };

            _stateManager?.ApplyChange(change);
        };

        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
        PropertiesPanel.Children.Add(validationText);
    }

    /// <summary>
    /// Validates User ID: must start with letter, contain only alphanumeric + underscore,
    /// be at least 2 chars, and be unique across all scenes.
    /// </summary>
    private (bool IsValid, string Error) ValidateUserId(string userId, string currentElementId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return (true, string.Empty); // Optional field

        if (userId.Length < 2)
            return (false, "User ID must be at least 2 characters");

        if (!char.IsLetter(userId[0]))
            return (false, "User ID must start with a letter");

        if (!userId.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return (false, "User ID can only contain letters, numbers, and underscores");

        // Check uniqueness across ALL scenes in project
        if (_project is not null)
        {
            foreach (var scene in _project.Scenes)
            {
                foreach (var elem in scene.Elements)
                {
                    if (elem.ElementId != currentElementId &&
                        !string.IsNullOrEmpty(elem.UserId) &&
                        elem.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"User ID '{userId}' already exists in scene '{scene.SceneName}'");
                    }
                }
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Adds a parameter field to the properties panel.
    /// Generates a TextBox with validation, undo/redo support, and real-time updates.
    /// </summary>
    private void AddParameterField(SceneElement element, IModule module, ParameterDefinition param)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = param.DisplayName,
            Style = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var input = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(0),
            Height = 32,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var currentValue = GetModuleParameterValue(module, param.Name);
        var textBox = new TextBox
        {
            Text = currentValue?.ToString() ?? param.DefaultValue?.ToString() ?? "",
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (param.Type == ParameterType.String)
            textBox.Loaded += (s, e) => textBox.Focus();

        // Capture value when the TextBox gains focus — this is the "before" value
        string valueOnFocus = textBox.Text;
        textBox.GotFocus += (s, e) => valueOnFocus = textBox.Text;

        textBox.LostFocus += (s, e) =>
        {
            if (_isUpdatingUI) return;

            var previousValue = valueOnFocus;
            var newValue = textBox.Text;

            if (previousValue == newValue) return;

            var displayName = module.DisplayName;
            
            // Create state change for parameter change (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = $"Change {param.DisplayName} on {displayName}",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    _isUpdatingUI = true;
                    textBox.Text = newValue;
                    _isUpdatingUI = false;

                    SetModuleParameterValue(module, param.Name, newValue, param.Type);

                    if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(newValue, out var x))
                    {
                        element.TileX = x;
                        UpdateElementPosition(element);
                    }
                    else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(newValue, out var y))
                    {
                        element.TileY = y;
                        UpdateElementPosition(element);
                    }

                    UpdateElementLabel(element);
                    RefreshElementVisual(element);
                },
                IsUndoable = true,
                UndoCommand = new ChangePropertyCommand(
                    description: $"Change {param.DisplayName} on {displayName}",
                    apply: val =>
                    {
                        _isUpdatingUI = true;
                        textBox.Text = val;
                        _isUpdatingUI = false;

                        SetModuleParameterValue(module, param.Name, val, param.Type);

                        if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var x))
                        {
                            element.TileX = x;
                            UpdateElementPosition(element);
                        }
                        else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var y))
                        {
                            element.TileY = y;
                            UpdateElementPosition(element);
                        }

                        UpdateElementLabel(element);
                        RefreshElementVisual(element);
                    },
                    previousValue: previousValue,
                    newValue: newValue)
            };

            _stateManager?.ApplyChange(change);
        };

        textBox.TextChanged += (s, e) =>
        {
            if (!_isUpdatingUI)
            {
                SetModuleParameterValue(module, param.Name, textBox.Text, param.Type);

                if (param.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && int.TryParse(textBox.Text, out var x))
                {
                    element.TileX = x;
                    UpdateElementPosition(element);
                }
                else if (param.Name.Equals("y", StringComparison.OrdinalIgnoreCase) && int.TryParse(textBox.Text, out var y))
                {
                    element.TileY = y;
                    UpdateElementPosition(element);
                }
                RefreshElementVisual(element);
                
                // Mark dirty on text change (real-time feedback)
                _projectManager?.MarkDirty();
            }
        };

        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
    }

    /// <summary>
    /// Gets a module parameter value using reflection.
    /// </summary>
    private object? GetModuleParameterValue(IModule module, string paramName)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        return prop?.GetValue(module);
    }

    /// <summary>
    /// Sets a module parameter value using reflection with type conversion.
    /// </summary>
    private void SetModuleParameterValue(IModule module, string paramName, string value, ParameterType type)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        if (prop is null) return;

        try
        {
            object? convertedValue = type switch
            {
                ParameterType.Int => int.TryParse(value, out var i) ? i : null,
                ParameterType.Float => float.TryParse(value, out var f) ? f : null,
                ParameterType.Bool => bool.TryParse(value, out var b) ? b : null,
                ParameterType.String => value,
                _ => value
            };

            if (convertedValue is not null)
                prop.SetValue(module, convertedValue);
        }
        catch
        {
            // Ignore conversion errors
        }
    }
}
