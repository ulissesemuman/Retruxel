using System.Windows.Controls;
using Retruxel.Tool.Wizard.Models;

namespace Retruxel.Tool.Wizard.Views.Steps;

public partial class TargetStep1BasicInfo : UserControl
{
    private readonly TargetWizardData _data;

    public TargetStep1BasicInfo(TargetWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    private void LoadData()
    {
        TargetIdTextBox.Text = _data.TargetId;
        DisplayNameTextBox.Text = _data.DisplayName;
        ManufacturerTextBox.Text = _data.Manufacturer;
        ReleaseYearTextBox.Text = _data.ReleaseYear.ToString();
        DescriptionTextBox.Text = _data.Description;
        StatusComboBox.SelectedIndex = _data.Status switch
        {
            "Active" => 0,
            "Scaffolding" => 1,
            "Planned" => 2,
            _ => 1
        };
    }

    private void AttachHandlers()
    {
        TargetIdTextBox.TextChanged += (s, e) => _data.TargetId = TargetIdTextBox.Text.ToLower().Trim();
        DisplayNameTextBox.TextChanged += (s, e) => _data.DisplayName = DisplayNameTextBox.Text.Trim();
        ManufacturerTextBox.TextChanged += (s, e) => _data.Manufacturer = ManufacturerTextBox.Text.Trim();
        ReleaseYearTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(ReleaseYearTextBox.Text, out int year))
                _data.ReleaseYear = year;
        };
        DescriptionTextBox.TextChanged += (s, e) => _data.Description = DescriptionTextBox.Text.Trim();
        StatusComboBox.SelectionChanged += (s, e) =>
        {
            _data.Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Scaffolding";
        };
    }
}
