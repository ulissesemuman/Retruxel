using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;

namespace Retruxel.Views;

public partial class NewProjectDialog : Window
{
    private readonly ITarget _target;
    private ProjectTemplate? _selectedTemplate;
    public RetruxelProject? CreatedProject { get; private set; }

    public NewProjectDialog(ITarget target)
    {
        InitializeComponent();
        _target = target;
        TxtTarget.Text = target.DisplayName.ToUpper();
        TxtLocation.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Retruxel");
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        foreach (var template in _target.GetTemplates())
        {
            var isSelected = _selectedTemplate is null;
            if (isSelected) _selectedTemplate = template;

            var card = BuildTemplateCard(template, isSelected);
            TemplatesPanel.Children.Add(card);
        }
    }

    private Border BuildTemplateCard(ProjectTemplate template, bool selected)
    {
        var card = new Border
        {
            Width = 160,
            Height = 80,
            Background = selected
                ? new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26))
                : new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12),
            Cursor = Cursors.Hand,
            Tag = template
        };

        if (selected)
        {
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
            card.BorderThickness = new Thickness(0, 2, 0, 0);
        }

        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = template.DisplayName.ToUpper(),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        });

        panel.Children.Add(new TextBlock
        {
            Text = template.Description,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            TextWrapping = TextWrapping.Wrap
        });

        card.Child = panel;
        card.MouseLeftButtonDown += (_, _) => SelectTemplate(template);
        return card;
    }

    private void SelectTemplate(ProjectTemplate template)
    {
        _selectedTemplate = template;

        foreach (Border card in TemplatesPanel.Children)
        {
            var isSelected = card.Tag is ProjectTemplate t && t.TemplateId == template.TemplateId;
            card.Background = new SolidColorBrush(isSelected
                ? Color.FromRgb(0x26, 0x26, 0x26)
                : Color.FromRgb(0x1E, 0x1E, 0x1E));
            card.BorderBrush = isSelected
                ? new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71))
                : Brushes.Transparent;
            card.BorderThickness = isSelected
                ? new Thickness(0, 2, 0, 0)
                : new Thickness(0);
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select project location"
        };

        if (dialog.ShowDialog() == true)
            TxtLocation.Text = dialog.FolderName;
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtProjectName.Text))
        {
            MessageBox.Show("Please enter a project name.", "Retruxel",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLocation.Text))
        {
            MessageBox.Show("Please select a project location.", "Retruxel",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var manager = new ProjectManager();
        CreatedProject = manager.CreateProject(
            TxtProjectName.Text.Trim(),
            TxtLocation.Text.Trim(),
            _target,
            _selectedTemplate ?? _target.GetTemplates().First());

        DialogResult = true;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}