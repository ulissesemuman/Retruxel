using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class ToolStep3Parameters : UserControl
{
    private readonly ToolWizardData _data;
    private readonly ObservableCollection<ToolParameterItem> _parameters = new();

    public ToolStep3Parameters(ToolWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadParameters();
        ParametersListBox.ItemsSource = _parameters;
    }

    private void LoadParameters()
    {
        foreach (var param in _data.Parameters)
        {
            var item = new ToolParameterItem
            {
                Name = param.Name,
                Type = param.Type,
                Description = param.Description
            };

            item.PropertyChanged += OnParameterChanged;
            _parameters.Add(item);
        }
    }

    private void AddParameter_Click(object sender, RoutedEventArgs e)
    {
        var item = new ToolParameterItem
        {
            Name = "newParameter",
            Type = "string",
            Description = string.Empty
        };

        item.PropertyChanged += OnParameterChanged;
        _parameters.Add(item);

        _data.Parameters.Add(new ToolParameter
        {
            Name = item.Name,
            Type = item.Type,
            Description = item.Description
        });
    }

    private void RemoveParameter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ToolParameterItem item)
        {
            _parameters.Remove(item);
            
            var paramToRemove = _data.Parameters.FirstOrDefault(p => p.Name == item.Name);
            if (paramToRemove != null)
            {
                _data.Parameters.Remove(paramToRemove);
            }
        }
    }

    private void OnParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Sync changes back to data model
        _data.Parameters.Clear();
        foreach (var item in _parameters)
        {
            _data.Parameters.Add(new ToolParameter
            {
                Name = item.Name,
                Type = item.Type,
                Description = item.Description
            });
        }
    }
}

public class ToolParameterItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _type = "string";
    private string _description = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged(nameof(Description));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
