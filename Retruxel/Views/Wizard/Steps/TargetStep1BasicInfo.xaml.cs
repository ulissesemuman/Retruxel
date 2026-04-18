using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;
using Retruxel.Core.Services;
using Retruxel.Core.Models;

namespace Retruxel.Views.Wizard.Steps;

public partial class TargetStep1BasicInfo : UserControl
{
    private readonly TargetWizardData _data;
    public ConsoleSpec? SelectedConsole { get; private set; }

    public TargetStep1BasicInfo(TargetWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadConsoleDatabase();
        LoadData();
        AttachHandlers();
    }

    private void LoadConsoleDatabase()
    {
        var consoles = ConsoleDatabaseService.GetAllConsoleNames();
        ConsoleComboBox.ItemsSource = consoles;
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
        ConsoleComboBox.SelectionChanged += ConsoleComboBox_SelectionChanged;
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

    private void ConsoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConsoleComboBox.SelectedItem is string consoleName)
        {
            SelectedConsole = ConsoleDatabaseService.FindByName(consoleName);
            if (SelectedConsole != null)
            {
                DisplayNameTextBox.Text = SelectedConsole.Name;
                ManufacturerTextBox.Text = SelectedConsole.Manufacturer;
                ReleaseYearTextBox.Text = SelectedConsole.ReleaseYear.ToString();
                DescriptionTextBox.Text = $"{SelectedConsole.CPU} @ {SelectedConsole.CpuClockHz / 1000000.0:F2}MHz — {SelectedConsole.RamBytes / 1024}KB RAM — {SelectedConsole.SoundChip} sound chip";
            }
        }
    }
}
