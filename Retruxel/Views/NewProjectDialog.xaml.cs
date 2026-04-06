using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        var settings = SettingsService.Load();
        TxtLocation.Text = settings.General.LastProjectLocation;
        ApplyLocalization();
        LoadTemplates();
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance;
        Title = loc.Get("newproject.title");
        TxtDialogTitle.Text = loc.Get("newproject.title");
        TxtProjectNameLabel.Text = loc.Get("newproject.name");
        TxtLocationLabel.Text = loc.Get("newproject.location");
        BtnBrowse.Content = loc.Get("newproject.browse");
        TxtTargetLabel.Text = loc.Get("newproject.target");
        TxtTemplateLabel.Text = loc.Get("newproject.template");
        BtnCancel.Content = loc.Get("newproject.cancel");
        BtnCreate.Content = loc.Get("newproject.create");
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
        var loc = LocalizationService.Instance;
        var settings = SettingsService.Load();
        var dialog = new OpenFolderDialog
        {
            Title = loc.Get("newproject.browse.title"),
            InitialDirectory = settings.General.LastProjectLocation
        };

        if (dialog.ShowDialog() == true)
        {
            TxtLocation.Text = dialog.FolderName;
            settings.General.LastProjectLocation = dialog.FolderName;
            SettingsService.Save(settings);
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        
        if (string.IsNullOrWhiteSpace(TxtProjectName.Text))
        {
            MessageBox.Show(loc.Get("newproject.error.name"), "Retruxel",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLocation.Text))
        {
            MessageBox.Show(loc.Get("newproject.error.location"), "Retruxel",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var projectName = TxtProjectName.Text.Trim();
        var baseLocation = TxtLocation.Text.Trim();
        var projectPath = Path.Combine(baseLocation, projectName);

        // Check if project folder already exists
        if (Directory.Exists(projectPath))
        {
            var result = MessageBox.Show(
                string.Format(loc.Get("newproject.error.exists"), projectName),
                "Retruxel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;
        }

        var manager = new ProjectManager();
        CreatedProject = manager.CreateProject(
            projectName,
            projectPath,
            _target,
            _selectedTemplate ?? _target.GetTemplates().First());

        var settings = SettingsService.Load();
        settings.General.LastProjectLocation = TxtLocation.Text.Trim();
        SettingsService.Save(settings);

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