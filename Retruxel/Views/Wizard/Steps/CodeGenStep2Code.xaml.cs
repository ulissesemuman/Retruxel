using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class CodeGenStep2Code : UserControl
{
    private readonly CodeGenWizardData _data;

    public CodeGenStep2Code(CodeGenWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    private void LoadData()
    {
        CodeTextBox.Text = _data.CodeContent;
    }

    private void AttachHandlers()
    {
        CodeTextBox.TextChanged += (s, e) => _data.CodeContent = CodeTextBox.Text;
    }

    private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Prevent execution before InitializeComponent completes
        if (FilePickerPanel == null || CodeTextBox == null)
            return;

        var selectedIndex = SourceComboBox.SelectedIndex;
        
        switch (selectedIndex)
        {
            case 0: // Type code now
                FilePickerPanel.Visibility = Visibility.Collapsed;
                CodeTextBox.IsReadOnly = false;
                _data.CodeSource = "type";
                break;
            
            case 1: // Attach existing file
                FilePickerPanel.Visibility = Visibility.Visible;
                CodeTextBox.IsReadOnly = true;
                _data.CodeSource = "attach";
                break;
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select C file",
            Filter = "C Files (*.c)|*.c|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                FilePathTextBox.Text = dialog.FileName;
                var content = File.ReadAllText(dialog.FileName);
                CodeTextBox.Text = content;
                _data.CodeContent = content;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
