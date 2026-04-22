using System.Windows.Controls;
using Retruxel.Tool.Wizard.Models;

namespace Retruxel.Tool.Wizard.Views.Steps;

public partial class ToolStep1BasicInfo : UserControl
{
    private readonly ToolWizardData _data;

    public ToolStep1BasicInfo(ToolWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    private void LoadData()
    {
        ToolIdTextBox.Text = _data.ToolId;
        DisplayNameTextBox.Text = _data.DisplayName;
        DescriptionTextBox.Text = _data.Description;
        CategoryComboBox.SelectedIndex = _data.Category switch
        {
            "Graphic" => 0,
            "Logic" => 1,
            "Audio" => 2,
            "Utility" => 3,
            _ => 3
        };
    }

    private void AttachHandlers()
    {
        ToolIdTextBox.TextChanged += (s, e) => _data.ToolId = ToolIdTextBox.Text.ToLower().Trim();
        DisplayNameTextBox.TextChanged += (s, e) => _data.DisplayName = DisplayNameTextBox.Text.Trim();
        DescriptionTextBox.TextChanged += (s, e) => _data.Description = DescriptionTextBox.Text.Trim();
        CategoryComboBox.SelectionChanged += (s, e) =>
        {
            _data.Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Utility";
        };
    }
}
