using System.Windows;
using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class CodeGenStep1BasicInfo : UserControl
{
    private readonly CodeGenWizardData _data;
    private readonly List<string> _availableTargets = new() { "sms", "nes", "gg", "sg1000", "coleco" };

    public CodeGenStep1BasicInfo(CodeGenWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadTargets();
        LoadData();
        AttachHandlers();
    }

    private void LoadTargets()
    {
        // TODO: Load from TargetRegistry when available
        foreach (var target in _availableTargets)
        {
            TargetIdComboBox.Items.Add(target);
        }

        if (TargetIdComboBox.Items.Count > 0)
            TargetIdComboBox.SelectedIndex = 0;
    }

    private void LoadData()
    {
        ModuleIdTextBox.Text = _data.ModuleId;
        DisplayNameTextBox.Text = _data.DisplayName;
        DescriptionTextBox.Text = _data.Description;

        if (!string.IsNullOrEmpty(_data.TargetId))
        {
            var index = _availableTargets.IndexOf(_data.TargetId);
            if (index >= 0)
                TargetIdComboBox.SelectedIndex = index;
        }
    }

    private void AttachHandlers()
    {
        ModuleIdTextBox.TextChanged += (s, e) => _data.ModuleId = ModuleIdTextBox.Text.ToLower().Trim();
        DisplayNameTextBox.TextChanged += (s, e) => _data.DisplayName = DisplayNameTextBox.Text.Trim();
        DescriptionTextBox.TextChanged += (s, e) => _data.Description = DescriptionTextBox.Text.Trim();
        TargetIdComboBox.SelectionChanged += (s, e) =>
        {
            if (TargetIdComboBox.SelectedItem != null)
                _data.TargetId = TargetIdComboBox.SelectedItem.ToString() ?? string.Empty;
        };
    }

    private void InstallTarget_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Target installation is not yet implemented in the wizard.\n\nPlease install targets manually through the main Retruxel interface.",
            "Not Implemented",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
