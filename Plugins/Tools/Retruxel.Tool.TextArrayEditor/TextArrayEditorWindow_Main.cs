using Retruxel.Core.Text;
using Retruxel.Modules.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.TextArrayEditor;

/// <summary>
/// Text Array Editor Window - manages multilingual string arrays with font preview.
/// Main partial class with initialization and state management.
/// </summary>
public partial class TextArrayEditorWindow : Window
{
    private readonly TextArrayModule _module;
    private readonly string _projectPath;
    private TextArrayState _state;
    private int _activeLanguageIndex = 0;
    private int _selectedStringIndex = -1;

    /// <summary>
    /// Module data to be returned to the invoker.
    /// </summary>
    public Dictionary<string, object>? ModuleData { get; private set; }

    public TextArrayEditorWindow(TextArrayModule module, string projectPath)
    {
        InitializeComponent();
        _module = module;
        _projectPath = projectPath;

        // Deserialize module state
        var json = _module.Serialize();
        _state = System.Text.Json.JsonSerializer.Deserialize<TextArrayState>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        }) ?? new TextArrayState();

        // Initialize UI
        TxtArrayNameInput.Text = _state.Name;
        TxtArrayName.Text = $"— {_state.Name}";

        RefreshLanguageTabs();
        RefreshStringsList();
        PopulateFontCategories();
        PopulateAsciiMap();

        // Set initial tab
        ActivateTab(TabStrings, BtnTabStrings);
    }

    #region State Classes

    private class TextArrayState
    {
        public string Name { get; set; } = "strings";
        public List<TextLanguage> Languages { get; set; } = new()
        {
            new TextLanguage { Code = "default", Strings = [""] }
        };
        public string? FontAssetId { get; set; }
    }

    private class TextLanguage
    {
        public string Code { get; set; } = "default";
        public List<string> Strings { get; set; } = [];
    }

    #endregion
}

/// <summary>
/// Simple text input dialog for language code entry.
/// </summary>
public class TextInputDialog : Window
{
    public string InputText { get; private set; } = "";

    public TextInputDialog(string title, string prompt)
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)FindResource("BrushSurface");

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var txtPrompt = new TextBlock
        {
            Text = prompt,
            Style = (Style)FindResource("TextBody"),
            Foreground = (System.Windows.Media.Brush)FindResource("BrushOnSurface")
        };
        Grid.SetRow(txtPrompt, 0);
        grid.Children.Add(txtPrompt);

        var txtInput = new TextBox();
        Grid.SetRow(txtInput, 2);
        grid.Children.Add(txtInput);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var btnCancel = new Button
        {
            Content = "CANCEL",
            Style = (Style)FindResource("ButtonGhost"),
            Padding = new Thickness(16, 0, 16, 0),
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0)
        };
        btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(btnCancel);

        var btnOk = new Button
        {
            Content = "OK",
            Style = (Style)FindResource("ButtonPrimary"),
            Padding = new Thickness(24, 0, 24, 0),
            Height = 32
        };
        btnOk.Click += (s, e) =>
        {
            InputText = txtInput.Text;
            DialogResult = true;
            Close();
        };
        btnPanel.Children.Add(btnOk);

        Grid.SetRow(btnPanel, 4);
        grid.Children.Add(btnPanel);

        Content = grid;
    }
}
