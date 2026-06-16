using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.PrefabEditor;

/// <summary>
/// Floating window for editing a PrefabData — identity, sprite, actions, and input mapping.
/// Works on a deep copy of the PrefabData; changes are committed only on Save.
/// </summary>
public partial class PrefabEditorWindow : Window
{
    private readonly RetruxelProject _project;
    private readonly ITarget _target;
    private readonly ActionRegistry _actionRegistry;
    private readonly ToolRegistry? _toolRegistry;
    private readonly string _projectPath;
    private readonly Action<PrefabData>? _saveCallback;

    // Working copy — never modify the original until Save is confirmed
    private PrefabData _prefab;

    // The original PrefabId before editing (to detect rename for uniqueness check)
    private readonly string _originalPrefabId;

    // Guards against SelectionChanged firing during programmatic combo population
    private bool _suppressPortSelectionChanged;
    private bool _suppressSpriteSelectionChanged;

    public PrefabEditorWindow(
        PrefabData prefab,
        RetruxelProject project,
        ITarget target,
        ActionRegistry actionRegistry,
        ToolRegistry? toolRegistry = null,
        string projectPath = "",
        Action<PrefabData>? saveCallback = null,
        Window? owner = null)
    {
        InitializeComponent();

        _project          = project;
        _target           = target;
        _actionRegistry   = actionRegistry;
        _toolRegistry     = toolRegistry;
        _projectPath      = projectPath;
        _saveCallback     = saveCallback;
        _originalPrefabId = prefab.PrefabId;

        // Work on a deep copy
        _prefab = DeepCopy(prefab);

        if (owner is not null)
            Owner = owner;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulateIdentity();
        PopulateSprite();
        PopulateActions();
        PopulateVBlankActions();
        PopulateStartActions();
        PopulateInputMapping();
        UpdateTitleBar();
        ValidateAll();
    }

    private void UpdateTitleBar()
    {
        TxtPrefabId.Text = string.IsNullOrEmpty(_prefab.PrefabId)
            ? "// new prefab"
            : $"// {_prefab.PrefabId}";
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    /// <summary>Deep copies a PrefabData via JSON round-trip.</summary>
    private static PrefabData DeepCopy(PrefabData source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<PrefabData>(json) ?? new PrefabData();
    }
}
