using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class CodeGenStep3VariableMapping : UserControl
{
    private readonly CodeGenWizardData _data;
    private readonly ObservableCollection<CodeGenVariableMappingItem> _variables = new();

    public CodeGenStep3VariableMapping(CodeGenWizardData data)
    {
        InitializeComponent();
        _data = data;
        DetectVariables();
        VariablesListBox.ItemsSource = _variables;
    }

    private void DetectVariables()
    {
        // Regex to find {{variableName}} patterns
        var regex = new Regex(@"\{\{(\w+)\}\}");
        var matches = regex.Matches(_data.CodeContent);

        var uniqueVariables = matches
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(v => v);

        foreach (var variable in uniqueVariables)
        {
            VariableMapping? existingMapping = null;
            if (_data.VariableMappings.ContainsKey(variable))
            {
                existingMapping = _data.VariableMappings[variable];
            }

            var item = new CodeGenVariableMappingItem
            {
                VariableName = $"{{{{{variable}}}}}",
                JsonPath = existingMapping?.JsonPath ?? GetDefaultJsonPath(variable),
                Type = existingMapping?.Type ?? GetDefaultType(variable),
                Transformation = existingMapping?.Transformation ?? string.Empty
            };

            item.PropertyChanged += (s, e) =>
            {
                _data.VariableMappings[variable] = new VariableMapping
                {
                    VariableName = variable,
                    JsonPath = item.JsonPath,
                    Type = item.Type,
                    Transformation = item.Transformation
                };
            };

            _variables.Add(item);
            
            // Initialize in data
            _data.VariableMappings[variable] = new VariableMapping
            {
                VariableName = variable,
                JsonPath = item.JsonPath,
                Type = item.Type,
                Transformation = item.Transformation
            };
        }
    }

    private string GetDefaultJsonPath(string variable)
    {
        return variable.ToLower() switch
        {
            "text" => "parameters.text",
            "x" => "parameters.x",
            "y" => "parameters.y",
            "color" => "parameters.color",
            "width" => "parameters.width",
            "height" => "parameters.height",
            "enabled" => "parameters.enabled",
            "instanceid" => "instanceId",
            _ => $"parameters.{variable.ToLower()}"
        };
    }

    private string GetDefaultType(string variable)
    {
        return variable.ToLower() switch
        {
            "text" => "string",
            "x" or "y" or "width" or "height" or "color" => "int",
            "enabled" or "visible" => "bool",
            _ => "string"
        };
    }
}

public class CodeGenVariableMappingItem : INotifyPropertyChanged
{
    private string _variableName = string.Empty;
    private string _jsonPath = string.Empty;
    private string _type = "string";
    private string _transformation = string.Empty;

    public string VariableName
    {
        get => _variableName;
        set
        {
            _variableName = value;
            OnPropertyChanged(nameof(VariableName));
        }
    }

    public string JsonPath
    {
        get => _jsonPath;
        set
        {
            _jsonPath = value;
            OnPropertyChanged(nameof(JsonPath));
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

    public string Transformation
    {
        get => _transformation;
        set
        {
            _transformation = value;
            OnPropertyChanged(nameof(Transformation));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
