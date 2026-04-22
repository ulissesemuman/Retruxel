using System.Windows.Controls;
using Retruxel.Core.Services;
using Retruxel.Services;
using Retruxel.Tool.Wizard.Models;

namespace Retruxel.Tool.Wizard.Views.Steps;

public partial class ToolStep2Configuration : UserControl
{
    private readonly ToolWizardData _data;
    private readonly List<string> _availableTargets;

    public ToolStep2Configuration(ToolWizardData data)
    {
        InitializeComponent();
        _data = data;
        _availableTargets = TargetRegistry.GetAllTargets().Select(t => t.TargetId).OrderBy(id => id).ToList();
        LoadTargets();
        LoadData();
        AttachHandlers();
    }

    private void LoadTargets()
    {
        // Add "Universal" option first
        TargetIdComboBox.Items.Add(new ComboBoxItem { Content = "Universal (all targets)" });
        
        // Add discovered targets
        foreach (var target in _availableTargets)
        {
            TargetIdComboBox.Items.Add(new ComboBoxItem { Content = target });
        }
        
        TargetIdComboBox.SelectedIndex = 0;
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
            var targetIndex = _availableTargets.IndexOf(_data.TargetId);
            if (targetIndex >= 0)
            {
                TargetIdComboBox.SelectedIndex = targetIndex + 1; // +1 because "Universal" is at index 0
            }
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
