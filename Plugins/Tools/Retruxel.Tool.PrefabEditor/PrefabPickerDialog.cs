using Retruxel.Core.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Tool.PrefabEditor;

/// <summary>
/// Popup for selecting an existing Prefab or creating a new one inline.
/// Used when clicking + in the Entities section of the project tree.
/// </summary>
public class PrefabPickerDialog : Window
{
    private static readonly Regex _idRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private readonly RetruxelProject _project;
    private readonly Action<string> _onSelected;
    private readonly Action<string, string, string, int, int> _onCreateNew;

    private TextBox? _txtId, _txtDisplay, _txtWidth, _txtHeight;
    private ComboBox? _cmbAsset;
    private TextBlock? _txtError;
    private StackPanel? _createForm;

    public PrefabPickerDialog(
        RetruxelProject project,
        Action<string> onSelected,
        Action<string, string, string, int, int> onCreateNew,
        Window? owner = null)
    {
        _project     = project;
        _onSelected  = onSelected;
        _onCreateNew = onCreateNew;

        if (owner is not null) Owner = owner;
        Title                 = "Select Prefab";
        Width                 = 400;
        SizeToContent         = SizeToContent.Height;
        WindowStyle           = WindowStyle.ToolWindow;
        ResizeMode            = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background            = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13));

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 600,
            Content   = BuildContent()
        };
    }

    private StackPanel BuildContent()
    {
        var root = new StackPanel { Margin = new Thickness(16) };

        root.Children.Add(MakeLabel("SELECT PREFAB", 10, Color.FromRgb(0x8e, 0xff, 0x71), new Thickness(0, 0, 0, 12)));

        // Existing prefabs list
        if (_project.Prefabs.Count == 0)
        {
            root.Children.Add(MakeLabel("No prefabs yet.", 10, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 8)));
        }
        else
        {
            foreach (var prefab in _project.Prefabs)
            {
                var p   = prefab;
                var row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                    Padding    = new Thickness(10, 8, 10, 8),
                    Margin     = new Thickness(0, 0, 0, 2),
                    Cursor     = Cursors.Hand,
                    Child      = new TextBlock
                    {
                        Text       = string.IsNullOrEmpty(p.DisplayName) ? p.PrefabId : $"{p.DisplayName}  ·  {p.PrefabId}",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize   = 11,
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                };
                row.MouseEnter           += (_, _) => row.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
                row.MouseLeave           += (_, _) => row.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a));
                row.MouseLeftButtonDown  += (_, _) => { _onSelected(p.PrefabId); DialogResult = true; Close(); };
                root.Children.Add(row);
            }
        }

        // Divider
        root.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Margin     = new Thickness(0, 12, 0, 12)
        });

        // + NEW PREFAB button
        var newBtn = new Button
        {
            Content         = "+ NEW PREFAB",
            Height          = 36,
            Background      = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
            Foreground      = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            FontFamily      = new FontFamily("Consolas"),
            FontSize        = 11,
            FontWeight      = FontWeights.Bold,
            BorderThickness = new Thickness(1),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            Margin          = new Thickness(0, 0, 0, 0)
        };
        root.Children.Add(newBtn);

        // Inline creation form
        _createForm = BuildCreateForm();
        _createForm.Visibility = Visibility.Collapsed;
        root.Children.Add(_createForm);

        newBtn.Click += (_, _) =>
        {
            _createForm.Visibility = Visibility.Visible;
            newBtn.Visibility      = Visibility.Collapsed;
            _txtId?.Focus();
            _txtId?.SelectAll();
        };

        return root;
    }

    private StackPanel BuildCreateForm()
    {
        var form = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };

        form.Children.Add(MakeLabel("NAME (IDENTIFIER) *", 9, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 4)));
        _txtId = new TextBox { Text = "player" };
        form.Children.Add(WrapInput(_txtId));

        form.Children.Add(MakeLabel("DISPLAY NAME", 9, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 4)));
        _txtDisplay = new TextBox { Text = string.Empty };
        form.Children.Add(WrapInput(_txtDisplay));

        form.Children.Add(MakeLabel("ASSET (optional)", 9, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 4)));
        _cmbAsset = new ComboBox
        {
            Height = 32, Margin = new Thickness(0, 0, 0, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
            Foreground = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0)
        };
        _cmbAsset.Items.Add(new ComboBoxItem { Content = "— none —", Tag = string.Empty });
        foreach (var asset in _project.Assets)
            _cmbAsset.Items.Add(new ComboBoxItem { Content = asset.FileName, Tag = asset.Id });
        _cmbAsset.SelectedIndex = 0;
        form.Children.Add(_cmbAsset);

        // Width / Height
        var dimGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftCol  = new StackPanel();
        leftCol.Children.Add(MakeLabel("WIDTH (tiles)", 9, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 4)));
        _txtWidth = new TextBox { Text = "2" };
        leftCol.Children.Add(WrapInput(_txtWidth));

        var rightCol = new StackPanel();
        rightCol.Children.Add(MakeLabel("HEIGHT (tiles)", 9, Color.FromRgb(0xad, 0xaa, 0xaa), new Thickness(0, 0, 0, 4)));
        _txtHeight = new TextBox { Text = "2" };
        rightCol.Children.Add(WrapInput(_txtHeight));

        Grid.SetColumn(leftCol,  0);
        Grid.SetColumn(rightCol, 2);
        dimGrid.Children.Add(leftCol);
        dimGrid.Children.Add(rightCol);
        form.Children.Add(dimGrid);

        _txtError = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x6e, 0x84)),
            FontSize   = 10,
            Margin     = new Thickness(0, 0, 0, 8),
            Visibility = Visibility.Collapsed
        };
        form.Children.Add(_txtError);

        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancelBtn = new Button
        {
            Content = "CANCEL", Width = 80, Height = 32, Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
            BorderThickness = new Thickness(0)
        };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };

        var createBtn = new Button
        {
            Content = "CREATE", Width = 80, Height = 32,
            Background = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x0d, 0x61, 0x00)),
            FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0)
        };
        createBtn.Click += OnCreateClick;

        btnRow.Children.Add(cancelBtn);
        btnRow.Children.Add(createBtn);
        form.Children.Add(btnRow);

        return form;
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        var id = _txtId!.Text.Trim().ToLowerInvariant().Replace(' ', '_');

        if (string.IsNullOrEmpty(id) || !_idRegex.IsMatch(id))
        { ShowError("Name must be a valid C identifier."); return; }

        if (_project.Prefabs.Any(p => p.PrefabId == id))
        { ShowError($"A prefab named '{id}' already exists."); return; }

        var display = _txtDisplay!.Text.Trim();
        var assetId = (_cmbAsset!.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty;
        int.TryParse(_txtWidth!.Text,  out var w); if (w < 1) w = 2;
        int.TryParse(_txtHeight!.Text, out var h); if (h < 1) h = 2;

        _onCreateNew(id, display, assetId, w, h);
        DialogResult = true;
        Close();
    }

    private void ShowError(string msg)
    {
        if (_txtError is null) return;
        _txtError.Text       = msg;
        _txtError.Visibility = Visibility.Visible;
    }

    private static Border WrapInput(TextBox tb)
    {
        tb.Background               = Brushes.Transparent;
        tb.Foreground               = new SolidColorBrush(Colors.White);
        tb.FontFamily               = new FontFamily("Consolas");
        tb.FontSize                 = 12;
        tb.BorderThickness          = new Thickness(0);
        tb.VerticalContentAlignment = VerticalAlignment.Center;
        tb.Padding                  = new Thickness(6, 0, 6, 0);

        return new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x47)),
            Height          = 32,
            Margin          = new Thickness(0, 0, 0, 8),
            Child           = tb
        };
    }

    private static TextBlock MakeLabel(string text, int size, Color color, Thickness margin) => new()
    {
        Text       = text,
        FontFamily = new FontFamily("Consolas"),
        FontSize   = size,
        Foreground = new SolidColorBrush(color),
        Margin     = margin
    };
}
