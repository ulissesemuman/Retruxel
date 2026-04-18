using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Retruxel.Core.Models.Wizard;
using Retruxel.Views.Wizard.Steps;

namespace Retruxel.Views.Wizard;

public partial class CodeGenWizardWindow : Window
{
    private readonly CodeGenWizardData _data = new();
    private int _currentStep = 0;
    private readonly List<UserControl> _steps = new();

    public CodeGenWizardWindow()
    {
        InitializeComponent();
        InitializeSteps();
        ShowStep(0);
    }

    private void InitializeSteps()
    {
        _steps.Add(new CodeGenStep1BasicInfo(_data));
        _steps.Add(new CodeGenStep2Code(_data));
        _steps.Add(new CodeGenStep3VariableMapping(_data));
        _steps.Add(new CodeGenStep4Dependencies(_data));
    }

    private void ShowStep(int stepIndex)
    {
        _currentStep = stepIndex;
        ContentArea.Content = _steps[stepIndex];

        // Update header
        StepIndicator.Text = $"{stepIndex + 1} / {_steps.Count}";
        StepTitle.Text = stepIndex switch
        {
            0 => "Step 1: Basic Information",
            1 => "Step 2: Code Template",
            2 => "Step 3: Variable Mapping",
            3 => "Step 4: Dependencies",
            _ => "Unknown Step"
        };

        // Update buttons
        BackButton.IsEnabled = stepIndex > 0;
        NextButton.Content = stepIndex == _steps.Count - 1 ? "Generate" : "Next →";
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0)
        {
            ShowStep(_currentStep - 1);
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        // Validate current step
        if (!ValidateCurrentStep())
        {
            MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentStep < _steps.Count - 1)
        {
            // Move to next step
            ShowStep(_currentStep + 1);
        }
        else
        {
            // Final step - generate files
            GenerateCodeGen();
        }
    }

    private bool ValidateCurrentStep()
    {
        return _currentStep switch
        {
            0 => !string.IsNullOrWhiteSpace(_data.ModuleId) && 
                 !string.IsNullOrWhiteSpace(_data.TargetId) &&
                 !string.IsNullOrWhiteSpace(_data.DisplayName),
            1 => !string.IsNullOrWhiteSpace(_data.CodeContent),
            2 => true, // Variable mapping is optional
            3 => true, // Dependencies are optional
            _ => true
        };
    }

    private void GenerateCodeGen()
    {
        try
        {
            // Choose output directory
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Choose output directory",
                FileName = "codegen_folder",
                DefaultExt = ".folder",
                Filter = "Folder|*.folder"
            };

            if (dialog.ShowDialog() != true)
                return;

            var outputDir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (string.IsNullOrEmpty(outputDir))
                return;

            _data.OutputPath = System.IO.Path.Combine(outputDir, $"{_data.ModuleId}.{_data.TargetId}");

            // Generate files
            var generator = new Core.Services.Wizard.CodeGenGenerator();
            generator.Generate(_data);

            MessageBox.Show(
                $"CodeGen generated successfully!\n\nOutput: {_data.OutputPath}\n\nNext steps:\n1. Review the generated codegen.json\n2. Test the template with sample module data\n3. Copy to Retruxel/Plugins/CodeGens/{_data.ModuleId}/{_data.TargetId}/",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating CodeGen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to cancel? All progress will be lost.", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Close();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
