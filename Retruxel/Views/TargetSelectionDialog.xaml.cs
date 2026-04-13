using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Views;

public partial class TargetSelectionDialog : Window
{
    public ITarget? SelectedTarget { get; private set; }

    public TargetSelectionDialog()
    {
        InitializeComponent();
    }

    private void TargetGrid_TargetSelected(ITarget target)
    {
        SelectedTarget = target;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
