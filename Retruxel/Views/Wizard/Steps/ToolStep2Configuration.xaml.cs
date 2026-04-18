using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class ToolStep2Configuration : UserControl
{
    private readonly ToolWizardData _data;

    public ToolStep2Configuration(ToolWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    private void LoadData()
    {
        // Target ID
        if (string.IsNullOrEmpty(_data.TargetId))
        {
            TargetIdComboBox.SelectedIndex = 0; // Universal
        }
        else
        {
            var targetIndex = _data.TargetId switch
            {
                "sms" => 1,
                "nes" => 2,
                "gg" => 3,
                "sg1000" => 4,
                "coleco" => 5,
                _ => 0
            };
            TargetIdComboBox.SelectedIndex = targetIndex;
        }

        ModuleIdTextBox.Text = _data.ModuleId ?? string.Empty;
        IsSingletonCheckBox.IsChecked = _data.IsSingleton;
        RequiresProjectCheckBox.IsChecked = _data.RequiresProject;
    }

    private void AttachHandlers()
    {
        TargetIdComboBox.SelectionChanged += (s, e) =>
        {
            var selected = (TargetIdComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selected == "Universal (all targets)")
            {
                _data.TargetId = null;
            }
            else
            {
                _data.TargetId = selected;
            }
        };

        ModuleIdTextBox.TextChanged += (s, e) =>
        {
            var text = ModuleIdTextBox.Text.Trim();
            _data.ModuleId = string.IsNullOrEmpty(text) ? null : text;
        };

        IsSingletonCheckBox.Checked += (s, e) => _data.IsSingleton = true;
        IsSingletonCheckBox.Unchecked += (s, e) => _data.IsSingleton = false;

        RequiresProjectCheckBox.Checked += (s, e) => _data.RequiresProject = true;
        RequiresProjectCheckBox.Unchecked += (s, e) => _data.RequiresProject = false;
    }
}
