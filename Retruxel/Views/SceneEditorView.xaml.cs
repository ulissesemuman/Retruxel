using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
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
    private ProjectManager? _projectManager;
    private ModuleLoader? _moduleLoader;
    private bool _isUpdatingUI;
    private bool _isLoadingProject;

    public event Action<RetruxelProject>? OnGenerateRomRequested;

    public SceneEditorView()
    {
        InitializeComponent();
        KeyDown += SceneEditorView_KeyDown;
        Focusable = true;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance;
        TxtSceneTitle.Text = loc.Get("scene.title");
        BtnGenerateRom.Content = loc.Get("scene.generate_rom");
        TxtSystemStatus.Text = loc.Get("scene.system_status");
        ModulePaletteHeader.Text = loc.Get("scene.modules");
        BtnNewAsset.Content = loc.Get("scene.new_asset");
        TxtDocumentation.Text = loc.Get("scene.documentation");
        TxtPropertiesTitle.Text = loc.Get("scene.properties");
        TxtNoSelection.Text = loc.Get("scene.no_selection");
        TxtEventsTitle.Text = loc.Get("scene.events");
    }

    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
    }

    public void SetModuleLoader(ModuleLoader loader)
    {
        _moduleLoader = loader;
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
        ApplyTargetSpecs(target);
        LoadModulePalette(target);
        LoadEvents();
        LoadFromProject();
    }

    private void ApplyTargetSpecs(ITarget target)
    {
        var specs = target.Specs;
        SceneCanvas.Width  = specs.ScreenWidth;
        SceneCanvas.Height = specs.ScreenHeight;
        TxtCanvasSize.Text = $"{specs.ScreenWidth} × {specs.ScreenHeight} px";
    }

    // ===== MODULE PALETTE =====

    /// <summary>
    /// Populates the left panel with available modules for this target.
    /// </summary>
    private void LoadModulePalette(ITarget target)
    {
        ModulePalettePanel.Children.Clear();

        if (_moduleLoader is null) return;

        var modules = new List<(string Category, string ModuleId, string DisplayName)>();

        foreach (var (moduleId, module) in _moduleLoader.LogicModules)
        {
            var m = (IModule)module;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        foreach (var (moduleId, module) in _moduleLoader.GraphicModules)
        {
            var m = (IModule)module;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        foreach (var (moduleId, module) in _moduleLoader.AudioModules)
        {
            var m = (IModule)module;
            modules.Add((m.Category, moduleId, m.DisplayName));
        }

        var grouped = modules.GroupBy(m => m.Category);

        foreach (var group in grouped)
        {
            var header = new TextBlock
            {
                Text = group.Key,
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(12, 8, 12, 4)
            };
            ModulePalettePanel.Children.Add(header);

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

                var maxTileX = (int)(SceneCanvas.Width  / 8) - 1;
                var maxTileY = (int)(SceneCanvas.Height / 8) - 1;

                var tileX = Math.Clamp((int)adjustedX / 8, 0, maxTileX);
                var tileY = Math.Clamp((int)adjustedY / 8, 0, maxTileY);
                
                Canvas.SetLeft(border, tileX * 8);
                Canvas.SetTop(border, tileY * 8);
                
                element.TileX = tileX;
                element.TileY = tileY;
                
                UpdateModulePosition(element.Module, tileX, tileY);
                
                if (_selectedElement == element)
                    BuildPropertiesPanel(element);
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
        var loc = LocalizationService.Instance;
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
            Text = loc.Get("scene.event.when"),
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
            Text = loc.Get("scene.event.add_action"),
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

        foreach (var param in manifest.Parameters)
        {
            AddParameterField(element, module, param);
        }
        
        _isUpdatingUI = false;
    }

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
        {
            textBox.Loaded += (s, e) => textBox.Focus();
        }

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
                
                UpdateElementLabel(element);
                SyncProjectModules();
            }
        };
        
        input.Child = textBox;
        PropertiesPanel.Children.Add(input);
    }

    // ===== HELPERS =====

    private SceneElement CreateSceneElement(string moduleId, int tileX, int tileY)
    {
        if (_moduleLoader is null)
            throw new InvalidOperationException("ModuleLoader not initialized");

        IModule? moduleTemplate = null;

        if (_moduleLoader.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleLoader.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleLoader.AudioModules.TryGetValue(moduleId, out var am))
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
            TileY = tileY
        };
    }

    private void UpdateElementLabel(SceneElement element)
    {
        var label = GetElementDisplayLabel(element);

        if (element.CanvasVisual is Border cv && cv.Child is TextBlock ctb)
            ctb.Text = label;
        if (element.EventVisual is Border ev && ev.Child is Grid g &&
            g.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock etb)
            etb.Text = label;
    }

    private string GetElementDisplayLabel(SceneElement element)
    {
        if (element.Module is not IModule module)
            return element.ModuleId;

        var textValue = GetModuleParameterValue(module, "text");
        if (textValue is not null)
            return $"[{element.ModuleId.ToUpper()}] {textValue}";

        return element.ModuleId.ToUpper();
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

                UpdateModulePosition(module, elementData.TileX, elementData.TileY);

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
        
        // If there are elements, select the first one and show properties
        if (_elements.Count > 0)
        {
            SelectElement(_elements[0]);
            BuildPropertiesPanel(_elements[0]);
        }
    }

    /// <summary>
    /// Deserializes a module from its JSON state.
    /// </summary>
    private IModule? DeserializeModule(string moduleId, string moduleState)
    {
        if (_moduleLoader is null) return null;

        IModule? moduleTemplate = null;

        if (_moduleLoader.LogicModules.TryGetValue(moduleId, out var lm))
            moduleTemplate = lm;
        else if (_moduleLoader.GraphicModules.TryGetValue(moduleId, out var gm))
            moduleTemplate = gm;
        else if (_moduleLoader.AudioModules.TryGetValue(moduleId, out var am))
            moduleTemplate = am;

        if (moduleTemplate is null) return null;

        var moduleType = moduleTemplate.GetType();
        var module = (IModule)Activator.CreateInstance(moduleType)!;
        
        if (!string.IsNullOrEmpty(moduleState))
            module.Deserialize(moduleState);
        
        return module;
    }

    private void UpdateModulePosition(object? module, int x, int y)
    {
        if (module is not IModule imodule) return;
        SetModuleParameterValue(imodule, "x", x.ToString(), ParameterType.Int);
        SetModuleParameterValue(imodule, "y", y.ToString(), ParameterType.Int);
    }

    private object? GetModuleParameterValue(IModule module, string paramName)
    {
        var prop = module.GetType().GetProperty(paramName, 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.IgnoreCase);
        
        return prop?.GetValue(module);
    }

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

    public string DisplayLabel
    {
        get
        {
            if (Module is IModule module)
            {
                var textValue = module.GetType().GetProperty("Text", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.IgnoreCase)?.GetValue(module);
                if (textValue is not null)
                    return $"[{ModuleId.ToUpper()}] {textValue}";
            }
            return ModuleId.ToUpper();
        }
    }
}