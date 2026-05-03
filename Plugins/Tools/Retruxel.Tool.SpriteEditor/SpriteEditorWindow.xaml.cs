using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.SpriteEditor.Helpers;
using Retruxel.Tool.SpriteEditor.Models;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow : Window
{
    private readonly ITarget _target;
    private readonly RetruxelProject _project;
    private readonly string _projectPath;
    private readonly ToolRegistry? _toolRegistry;
    private readonly Func<System.Threading.Tasks.Task>? _saveProjectCallback;
    private readonly object? _sceneEditor;

    private readonly SpriteState _state = new();
    private int _selectedTileIndex = -1;

    public Dictionary<string, object>? ModuleData { get; private set; }

    public SpriteEditorWindow(
        ITarget target,
        RetruxelProject project,
        string projectPath,
        ToolRegistry? toolRegistry = null,
        Func<System.Threading.Tasks.Task>? saveProjectCallback = null,
        object? sceneEditor = null)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;
        _toolRegistry = toolRegistry;
        _saveProjectCallback = saveProjectCallback;
        _sceneEditor = sceneEditor;

        InitializeUI();
        
        _state.Frames.Add(new SpriteFrame { Name = "Frame 1" });
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
