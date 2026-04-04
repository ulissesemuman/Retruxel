using Retruxel.Core.Models;
using Retruxel.Views;
using System.Windows;
using System.Windows.Input;

namespace Retruxel;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WelcomeView.OnProjectCreated += OnProjectCreated;
    }

    /// <summary>
    /// Called when a project is created from the WelcomeView.
    /// Switches to the Build Console and starts the build.
    /// </summary>
    private async void OnProjectCreated(RetruxelProject project)
    {
        WelcomeView.Visibility = Visibility.Collapsed;
        BuildConsoleView.Visibility = Visibility.Visible;
        await BuildConsoleView.BuildAsync(project);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}