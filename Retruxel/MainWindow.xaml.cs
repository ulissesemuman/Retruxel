using Retruxel.Core.Models;
using Retruxel.Views;
using System.Windows;
using System.Windows.Input;

namespace Retruxel;

public partial class MainWindow : Window
{
    private readonly BuildConsoleView _buildConsoleView = new();

    public MainWindow()
    {
        InitializeComponent();
        WelcomeView.OnProjectCreated += OnProjectCreated;
        WelcomeView.OnAboutRequested += () => ShowOverlay("ABOUT", new AboutView());
    }

    /// <summary>
    /// Shows a view as an overlay on top of the current screen.
    /// </summary>
    private void ShowOverlay(string title, object content)
    {
        OverlayTitle.Text = title;
        OverlayContentPresenter.Content = content;
        OverlayLayer.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Closes the current overlay and returns to the base layer.
    /// </summary>
    private void CloseOverlay_Click(object sender, RoutedEventArgs e)
    {
        OverlayLayer.Visibility = Visibility.Collapsed;
        OverlayContentPresenter.Content = null;
    }

    /// <summary>
    /// Called when a project is created — shows Build Console as overlay.
    /// </summary>
    private async void OnProjectCreated(RetruxelProject project)
    {
        var target = new Retruxel.Target.SMS.SmsTarget();

        // Show Scene Editor as main view
        WelcomeView.Visibility = Visibility.Collapsed;
        SceneEditorView.Visibility = Visibility.Visible;
        SceneEditorView.Initialize(project, target);

        // Connect Generate ROM button
        SceneEditorView.OnGenerateRomRequested -= OnGenerateRomRequested;
        SceneEditorView.OnGenerateRomRequested += OnGenerateRomRequested;
    }

    private async void OnGenerateRomRequested(RetruxelProject project)
    {
        ShowOverlay("BUILD CONSOLE", _buildConsoleView);
        await _buildConsoleView.BuildAsync(project);
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