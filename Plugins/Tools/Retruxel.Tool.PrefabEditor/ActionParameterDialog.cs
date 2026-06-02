using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.PrefabEditor;

/// <summary>
/// Small modal for editing the parameter values of an ActionInstance.
/// </summary>
public class ActionParameterDialog : Window
{
    private readonly ActionInstance _instance;
    private readonly Dictionary<string, Func<object>> _readers = new();

    public ActionParameterDialog(ActionInstance instance, ActionDefinition def, Window owner)
    {
        _instance = instance;

        Owner                 = owner;
        Title                 = $"Configure: {def.DisplayName}";
        Width                 = 400;
        SizeToContent         = SizeToContent.Height;
        WindowStyle           = WindowStyle.ToolWindow;
        ResizeMode            = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background            = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13));

        var root = new StackPanel { Margin = new Thickness(16) };

        var header = new TextBlock
        {
            Text       = def.ActionId.ToUpperInvariant() + " — PARAMETERS",
            FontFamily = new FontFamily("Consolas"),
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            Margin     = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(header);

        foreach (var param in def.Parameters)
        {
            var p = param;

            root.Children.Add(new TextBlock
            {
                Text       = p.Label.ToUpperInvariant(),
                FontFamily = new FontFamily("Consolas"),
                FontSize   = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
                Margin     = new Thickness(0, 0, 0, 4)
            });

            instance.Parameters.TryGetValue(p.Name, out var currentVal);
            var currentStr = currentVal?.ToString() ?? p.Default?.ToString() ?? string.Empty;

            if (p.Type == "bool")
            {
                var combo = new ComboBox
                {
                    Height          = 32,
                    Margin          = new Thickness(0, 0, 0, 12),
                    Background      = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                    Foreground      = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0)
                };
                combo.Items.Add(new ComboBoxItem { Content = "Yes", Tag = true  });
                combo.Items.Add(new ComboBoxItem { Content = "No",  Tag = false });
                combo.SelectedIndex = currentStr.Equals("true", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                root.Children.Add(combo);
                _readers[p.Name] = () => combo.SelectedItem is ComboBoxItem ci ? ci.Tag : false;
            }
            else
            {
                var border = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x48, 0x48, 0x47)),
                    Height          = 32,
                    Margin          = new Thickness(0, 0, 0, 12)
                };
                var tb = new TextBox
                {
                    Text                     = currentStr,
                    Background               = Brushes.Transparent,
                    Foreground               = new SolidColorBrush(Colors.White),
                    FontFamily               = new FontFamily("Consolas"),
                    FontSize                 = 12,
                    BorderThickness          = new Thickness(0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding                  = new Thickness(6, 0, 6, 0)
                };
                border.Child = tb;
                root.Children.Add(border);

                var localType = p.Type;
                _readers[p.Name] = () => localType switch
                {
                    "int"   => int.TryParse(tb.Text.Trim(),   out var i) ? (object)i  : 0,
                    "float" => float.TryParse(tb.Text.Trim(), out var f) ? (object)f  : 0f,
                    _       => tb.Text.Trim()
                };
            }
        }

        // Footer
        var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cancelBtn = new Button
        {
            Content         = "CANCEL",
            Width           = 88, Height = 32,
            Margin          = new Thickness(0, 0, 8, 0),
            Background      = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Foreground      = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
            BorderThickness = new Thickness(0)
        };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };

        var saveBtn = new Button
        {
            Content         = "SAVE",
            Width           = 88, Height = 32,
            Background      = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            Foreground      = new SolidColorBrush(Color.FromRgb(0x0d, 0x61, 0x00)),
            FontWeight      = FontWeights.Bold,
            BorderThickness = new Thickness(0)
        };
        saveBtn.Click += (_, _) =>
        {
            foreach (var (key, reader) in _readers)
                _instance.Parameters[key] = reader();
            DialogResult = true;
            Close();
        };

        Grid.SetColumn(cancelBtn, 1);
        Grid.SetColumn(saveBtn,   2);
        footer.Children.Add(cancelBtn);
        footer.Children.Add(saveBtn);
        root.Children.Add(footer);

        Content = root;
    }
}
