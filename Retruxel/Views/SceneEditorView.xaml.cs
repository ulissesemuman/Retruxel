using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Modules.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView : UserControl
{
    private RetruxelProject? _project;
    private ITarget? _target;
    private SceneData? _currentScene;
    private readonly List<SceneElement> _elements = [];
    private SceneElement? _selectedElement;
    private bool _isDragging;
    private Point _dragOffset;
    private SceneElement? _draggedElement;
    private Retruxel.Core.Services.ProjectManager? _projectManager;
    private bool _isUpdatingUI;
    private bool _isLoadingProject;

    public event Action<RetruxelProject>? OnGenerateRomRequested;

    public SceneEditorView()
    {
        InitializeComponent();
        KeyDown += SceneEditorView_KeyDown;
        Focusable = true;
    }

    public void SetProjectManager(Retruxel.Core.Services.ProjectManager manager)
    {
        _projectManager = manager;
    }

    private void SceneEditorView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _selectedElement is not null)
        {
            RemoveElement(_selectedElement);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Initializes the editor with the given project and target.
    /// </summary>
    public void Initialize(RetruxelProject project, ITarget target)
    {
        _project = project;
        _target = target;
        
        // Clear previous session data
        _elements.Clear();
        SceneCanvas.Children.Clear();
        _selectedElement = null;
        
        // Get or create main scene
        _currentScene = project.Scenes.FirstOrDefault();
        if (_currentScene is null)
        {
            _currentScene = new SceneData
            {
                SceneId = Guid.NewGuid().ToString(),
                SceneName = "Main",
                Elements = []
            };
            project.Scenes.Add(_currentScene);
        }
        
        TxtSceneName.Text = _currentScene.SceneName;
        LoadModulePalette(target);
        LoadEvents();
        LoadFromProject();
    }

    // ===== MODULE PALETTE =====

    /// <summary>
    /// Populates the left panel with available modules for this target.
    /// </summary>
    private void LoadModulePalette(ITarget target)
    {
        ModulePalettePanel.Children.Clear();

        var modules = new List<(string Category, string ModuleId, string DisplayName)>
        {
            ("OUTPUT", "text.display", "Text Display"),
            // More modules will be added as they are implemented
        };

        var grouped = modules.GroupBy(m => m.Category);

        foreach (var group in grouped)
        {
            // Category header
            var header = new TextBlock
            {
                Text = group.Key,
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(12, 8, 12, 4)
            };
            ModulePalettePanel.Children.Add(header);

            // Module items
            foreach (var module in group)
            {
                var item = BuildModulePaletteItem(module.ModuleId, module.DisplayName);
                ModulePalettePanel.Children.Add(item);
            }
        }
    }

    private Border BuildModulePaletteItem(string moduleId, string displayName)
    {
        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(8, 0, 8, 4),
            Cursor = Cursors.Hand,
            Tag = moduleId
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        panel.Children.Add(new TextBlock
        {
            Text = "⬛ ",
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = displayName,
            Style = (Style)FindResource("TextBody"),
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        item.Child = panel;

        // Enable drag from palette to canvas or events
        item.MouseMove += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(item, moduleId, DragDropEffects.Copy);
            }
        };

        return item;
    }

    // ===== CANVAS =====

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(SceneCanvas);
        TxtCoordinates.Text = $"X: {(int)pos.X} | Y: {(int)pos.Y}";
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Deselect when clicking empty canvas area
        SelectElement(null);
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(string)) is not string moduleId) return;

        var pos = e.GetPosition(SceneCanvas);
        var tileX = (int)pos.X / 8;
        var tileY = (int)pos.Y / 8;

        AddModuleToCanvas(moduleId, tileX, tileY);
    }

    /// <summary>
    /// Adds a module element to the canvas at the given tile position.
    /// </summary>
    private void AddModuleToCanvas(string moduleId, int tileX, int tileY)
    {
        var element = CreateSceneElement(moduleId, tileX, tileY);
        _elements.Add(element);

        var visual = BuildCanvasElement(element);
        Canvas.SetLeft(visual, tileX * 8);
        Canvas.SetTop(visual, tileY * 8);
        SceneCanvas.Children.Add(visual);

        // Also add to OnStart event
        AddModuleToEvent("OnStart", moduleId, element);
        SelectElement(element);
    }

    private Border BuildCanvasElement(SceneElement element)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 2, 4, 2),
            Cursor = Cursors.Hand,
            Tag = element
        };

        var label = new TextBlock
        {
            Text = element.DisplayLabel,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71))
        };

        border.Child = label;
        border.MouseLeftButtonDown += (s, e) =>
        {
            SelectElement(element);
            _isDragging = true;
            var clickPos = e.GetPosition(border);
            _dragOffset = clickPos;
            _draggedElement = element;
            border.CaptureMouse();
            e.Handled = true;
        };

        border.MouseMove += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                var canvasPos = e.GetPosition(SceneCanvas);
                var adjustedX = canvasPos.X - _dragOffset.X;
                var adjustedY = canvasPos.Y - _dragOffset.Y;
                
                var tileX = Math.Clamp((int)adjustedX / 8, 0, 31);
                var tileY = Math.Clamp((int)adjustedY / 8, 0, 23);
                
                Canvas.SetLeft(border, tileX * 8);
                Canvas.SetTop(border, tileY * 8);
                
                element.TileX = tileX;
                element.TileY = tileY;
                
                if (element.Module is TextDisplayModule textModule)
                {
                    textModule.X = tileX;
                    textModule.Y = tileY;
                    if (_selectedElement == element)
                        BuildPropertiesPanel(element);
                }
            }
        };

        border.MouseLeftButtonUp += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                _isDragging = false;
                _draggedElement = null;
                border.ReleaseMouseCapture();
                SyncProjectModules();
                e.Handled = true;
            }
        };

        element.CanvasVisual = border;
        return border;
    }

    // ===== EVENTS PANEL =====

    private void LoadEvents()
    {
        EventsPanel.Children.Clear();

        var triggers = new[] { "OnStart", "OnVBlank", "OnInput_Button1", "OnInput_Button2" };

        foreach (var trigger in triggers)
            AddEventBlock(trigger);
    }

    private void AddEventBlock(string trigger)
    {
        var block = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 4),
            Tag = trigger
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var whenLabel = new TextBlock
        {
            Text = "WHEN",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var triggerLabel = new TextBlock
        {
            Text = trigger,
            FontSize = 10,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var actionsPanel = new StackPanel
        {
            Tag = $"actions_{trigger}"
        };

        var addAction = new TextBlock
        {
            Text = "+ ADD ACTION",
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 0)
        };
        addAction.MouseLeftButtonDown += (s, e) => ShowModulePicker(trigger);

        Grid.SetColumn(whenLabel, 0);
        Grid.SetColumn(triggerLabel, 1);

        grid.Children.Add(whenLabel);
        grid.Children.Add(triggerLabel);

        var outerPanel = new StackPanel();
        outerPanel.Children.Add(grid);
        outerPanel.Children.Add(actionsPanel);
        outerPanel.Children.Add(addAction);

        block.Child = outerPanel;

        // Allow drop from module palette
        block.AllowDrop = true;
        block.Drop += (s, e) =>
        {
            if (e.Data.GetData(typeof(string)) is string moduleId)
                AddModuleToEvent(trigger, moduleId);
        };

        EventsPanel.Children.Add(block);
    }

    private void ShowModulePicker(string trigger)
    {
        // For now, directly add text.display — will become a picker dialog
        AddModuleToEvent(trigger, "text.display");
    }

    /// <summary>
    /// Adds a module action to an event block.
    /// </summary>
    private void AddModuleToEvent(string trigger, string moduleId,
        SceneElement? existingElement = null)
    {
        var element = existingElement ?? CreateSceneElement(moduleId, 0, 0);
        element.Trigger = trigger;

        if (existingElement is null)
            _elements.Add(element);

        // Find the actions panel for this trigger
        foreach (Border block in EventsPanel.Children)
        {
            if (block.Tag?.ToString() != trigger) continue;
            if (block.Child is not StackPanel outer) continue;

            var actionsPanel = outer.Children.OfType<StackPanel>()
                .FirstOrDefault(p => p.Tag?.ToString() == $"actions_{trigger}");

            if (actionsPanel is null) continue;

            var action = BuildEventAction(element);
            actionsPanel.Children.Add(action);
            break;
        }

        SelectElement(element);
        
        // Only mark dirty if this is a new element, not during load
        if (existingElement is null)
            SyncProjectModules();
    }

    private Border BuildEventAction(SceneElement element)
    {
        var action = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 2, 0, 2),
            Cursor = Cursors.Hand,
            Tag = element
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = element.DisplayLabel,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var remove = new TextBlock
        {
            Text = "✕",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        remove.MouseLeftButtonDown += (s, e) =>
        {
            RemoveElement(element);
            e.Handled = true;
        };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(remove, 1);
        grid.Children.Add(label);
        grid.Children.Add(remove);
        action.Child = grid;

        action.MouseLeftButtonDown += (s, e) =>
        {
            SelectElement(element);
            e.Handled = true;
        };

        element.EventVisual = action;
        return action;
    }

    // ===== SELECTION & PROPERTIES =====

    private void SelectElement(SceneElement? element)
    {
        // Deselect previous
        if (_selectedElement is not null)
        {
            if (_selectedElement.CanvasVisual is Border cv)
            {
                cv.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
                cv.BorderThickness = new Thickness(1);
            }
            if (_selectedElement.EventVisual is Border ev)
                ev.Background = new SolidColorBrush(
                    Color.FromRgb(0x26, 0x26, 0x26));
        }

        _selectedElement = element;

        if (element is null)
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            PropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        // Highlight selected with thicker yellow border
        if (element.CanvasVisual is Border cvSelected)
        {
            cvSelected.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
            cvSelected.BorderThickness = new Thickness(2);
        }
        if (element.EventVisual is Border evSelected)
            evSelected.Background = new SolidColorBrush(
                Color.FromRgb(0x1E, 0x1E, 0x2E));

        TxtNoSelection.Visibility = Visibility.Collapsed;
        PropertiesPanel.Visibility = Visibility.Visible;
        
        // Don't build properties panel during project load
        if (!_isLoadingProject)
            BuildPropertiesPanel(element);
        
        // Ensure keyboard focus for Delete key
        Focus();
    }

    private void BuildPropertiesPanel(SceneElement element)
    {
        _isUpdatingUI = true;
        PropertiesPanel.Children.Clear();

        if (element.Module is not TextDisplayModule textModule)
        {
            _isUpdatingUI = false;
            return;
        }

        // Module name
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "TEXT DISPLAY",
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Margin = new Thickness(0, 0, 0, 16)
        });

        // X position
        AddPropertyField("X POSITION", textModule.X.ToString(), value =>
        {
            if (int.TryParse(value, out var x))
            {
                textModule.X = Math.Clamp(x, 0, 31);
                element.TileX = textModule.X;
                UpdateElementPosition(element);
                UpdateElementLabel(element);
                SyncProjectModules();
            }
        });

        // Y position
        AddPropertyField("Y POSITION", textModule.Y.ToString(), value =>
        {
            if (int.TryParse(value, out var y))
            {
                textModule.Y = Math.Clamp(y, 0, 23);
                element.TileY = textModule.Y;
                UpdateElementPosition(element);
                UpdateElementLabel(element);
                SyncProjectModules();
            }
        });

        // Text
        AddPropertyField("TEXT", textModule.Text, value =>
        {
            textModule.Text = value;
            UpdateElementLabel(element);
            SyncProjectModules();
        });
        
        _isUpdatingUI = false;
    }

    private void AddPropertyField(string label, string value, Action<string> onChange)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = label,
            Style = (Style)FindResource("TextLabel"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var input = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 0, 8, 0),
            Height = 32,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var textBox = new TextBox
        {
            Text = value,
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Auto-focus on TEXT field
        if (label == "TEXT")
        {
            textBox.Loaded += (s, e) => textBox.Focus();
        }

        textBox.TextChanged += (s, e) =>
        {
            if (!_isUpdatingUI)
                onChange(textBox.Text);
        };
        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
    }

    // ===== HELPERS =====

    private SceneElement CreateSceneElement(string moduleId, int tileX, int tileY)
    {
        var module = moduleId switch
        {
            "text.display" => (object)new TextDisplayModule { X = tileX, Y = tileY },
            _ => throw new InvalidOperationException($"Unknown module: {moduleId}")
        };

        return new SceneElement
        {
            ElementId = Guid.NewGuid().ToString(),
            ModuleId = moduleId,
            Module = module,
            TileX = tileX,
            TileY = tileY
        };
    }

    private void UpdateElementLabel(SceneElement element)
    {
        var label = element.DisplayLabel;

        if (element.CanvasVisual is Border cv && cv.Child is TextBlock ctb)
            ctb.Text = label;
        if (element.EventVisual is Border ev && ev.Child is Grid g &&
            g.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock etb)
            etb.Text = label;
    }

    private void UpdateElementPosition(SceneElement element)
    {
        if (element.CanvasVisual is Border border)
        {
            Canvas.SetLeft(border, element.TileX * 8);
            Canvas.SetTop(border, element.TileY * 8);
        }
    }

    private void RemoveElement(SceneElement element)
    {
        _elements.Remove(element);

        if (element.CanvasVisual is not null)
            SceneCanvas.Children.Remove(element.CanvasVisual);

        foreach (Border block in EventsPanel.Children)
        {
            if (block.Child is not StackPanel outer) continue;
            var actionsPanel = outer.Children.OfType<StackPanel>().FirstOrDefault();
            if (actionsPanel is null) continue;
            if (element.EventVisual is not null)
                actionsPanel.Children.Remove(element.EventVisual);
        }

        if (_selectedElement == element)
            SelectElement(null);

        SyncProjectModules();
    }

    /// <summary>
    /// Syncs the current scene elements back to the project model.
    /// </summary>
    private void SyncProjectModules()
    {
        if (_project is null || _currentScene is null) return;

        _currentScene.Elements = _elements.Select(e => new SceneElementData
        {
            ElementId = e.ElementId,
            ModuleId = e.ModuleId,
            TileX = e.TileX,
            TileY = e.TileY,
            Trigger = e.Trigger,
            ModuleState = e.Module is Retruxel.Core.Interfaces.IModule module
                ? module.Serialize()
                : string.Empty
        }).ToList();

        _project.DefaultModules = _elements
            .Select(e => e.ModuleId)
            .Distinct()
            .ToList();
        
        _projectManager?.MarkDirty();
    }

    private void GenerateRom_Click(object sender, RoutedEventArgs e)
    {
        SyncProjectModules();
        if (_project is not null)
            OnGenerateRomRequested?.Invoke(_project);
    }

    /// <summary>
    /// Loads scene elements from the project and reconstructs the UI.
    /// </summary>
    private void LoadFromProject()
    {
        if (_currentScene is null || _currentScene.Elements.Count == 0)
            return;

        _isLoadingProject = true;

        foreach (var elementData in _currentScene.Elements)
        {
            try
            {
                var module = DeserializeModule(elementData.ModuleId, elementData.ModuleState);
                if (module is null) continue;

                var element = new SceneElement
                {
                    ElementId = elementData.ElementId,
                    ModuleId = elementData.ModuleId,
                    Module = module,
                    TileX = elementData.TileX,
                    TileY = elementData.TileY,
                    Trigger = elementData.Trigger
                };

                // Sync module coordinates with element coordinates
                if (module is TextDisplayModule textModule)
                {
                    textModule.X = elementData.TileX;
                    textModule.Y = elementData.TileY;
                }

                _elements.Add(element);

                // Add to canvas if it has position
                if (elementData.TileX >= 0 && elementData.TileY >= 0)
                {
                    var visual = BuildCanvasElement(element);
                    Canvas.SetLeft(visual, element.TileX * 8);
                    Canvas.SetTop(visual, element.TileY * 8);
                    SceneCanvas.Children.Add(visual);
                }

                // Add to event panel
                if (!string.IsNullOrEmpty(elementData.Trigger))
                {
                    AddModuleToEvent(elementData.Trigger, elementData.ModuleId, element);
                }
            }
            catch
            {
                // Skip corrupted elements
            }
        }
        
        _isLoadingProject = false;
    }

    /// <summary>
    /// Deserializes a module from its JSON state.
    /// </summary>
    private object? DeserializeModule(string moduleId, string moduleState)
    {
        if (moduleId == "text.display")
        {
            var module = new TextDisplayModule();
            if (!string.IsNullOrEmpty(moduleState))
                module.Deserialize(moduleState);
            return module;
        }
        
        return null;
    }
}

/// <summary>
/// Represents an element placed in the scene — a module instance with position and visuals.
/// </summary>
public class SceneElement
{
    public string ElementId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public object? Module { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Trigger { get; set; } = "OnStart";
    public UIElement? CanvasVisual { get; set; }
    public UIElement? EventVisual { get; set; }

    public string DisplayLabel => Module switch
    {
        TextDisplayModule m => $"[TEXT] {m.Text}",
        _ => ModuleId
    };
}