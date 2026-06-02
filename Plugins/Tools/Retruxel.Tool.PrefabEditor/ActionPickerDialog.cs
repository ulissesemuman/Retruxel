using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.PrefabEditor;

/// <summary>
/// Small modal that lists all available ActionDefinitions for the user to pick one.
/// </summary>
public class ActionPickerDialog : Window
{
    public string? SelectedActionId { get; private set; }

    public ActionPickerDialog(ActionRegistry registry, Window owner)
    {
        Owner                 = owner;
        Title                 = "Add Action";
        Width                 = 400;
        SizeToContent         = SizeToContent.Height;
        WindowStyle           = WindowStyle.ToolWindow;
        ResizeMode            = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background            = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13));

        var panel = new StackPanel { Margin = new Thickness(16) };

        var header = new TextBlock
        {
            Text       = "SELECT ACTION",
            FontFamily = new FontFamily("Consolas"),
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8e, 0xff, 0x71)),
            Margin     = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(header);

        // Group by category
        var grouped = registry.Actions.Values
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var catLabel = new TextBlock
            {
                Text       = group.Key.ToUpperInvariant(),
                FontFamily = new FontFamily("Consolas"),
                FontSize   = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
                Margin     = new Thickness(0, 8, 0, 4)
            };
            panel.Children.Add(catLabel);

            foreach (var action in group.OrderBy(a => a.ActionId))
            {
                var a = action;
                var row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                    Padding    = new Thickness(10, 8, 10, 8),
                    Margin     = new Thickness(0, 0, 0, 2),
                    Cursor     = System.Windows.Input.Cursors.Hand
                };

                var info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text       = a.DisplayName,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Colors.White)
                });

                if (a.Parameters.Length > 0)
                {
                    var paramNames = string.Join(", ", a.Parameters.Select(p => p.Name));
                    info.Children.Add(new TextBlock
                    {
                        Text       = paramNames,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize   = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
                        Margin     = new Thickness(0, 2, 0, 0)
                    });
                }

                row.Child = info;
                row.MouseEnter += (_, _) => row.Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
                row.MouseLeave += (_, _) => row.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a));
                row.MouseLeftButtonDown += (_, _) =>
                {
                    SelectedActionId = a.ActionId;
                    DialogResult     = true;
                    Close();
                };

                panel.Children.Add(row);
            }
        }

        // Cancel button
        var cancelBtn = new Button
        {
            Content    = "CANCEL",
            Margin     = new Thickness(0, 16, 0, 0),
            Height     = 32,
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xad, 0xaa, 0xaa)),
            BorderThickness = new Thickness(0)
        };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };
        panel.Children.Add(cancelBtn);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            MaxHeight                     = 500,
            Content                       = panel
        };
    }
}
