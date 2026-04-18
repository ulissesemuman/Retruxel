using System.Windows;
using System.Windows.Input;

namespace Retruxel.Views.Wizard;

public partial class WizardMainWindow : Window
{
    public WizardMainWindow()
    {
        InitializeComponent();
    }

    private void CreateTarget_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new TargetWizardWindow();
        wizard.ShowDialog();
    }

    private void CreateCodeGen_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new CodeGenWizardWindow();
        wizard.ShowDialog();
    }

    private void CreateTool_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new ToolWizardWindow();
        wizard.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
