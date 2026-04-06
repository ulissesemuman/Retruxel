using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Views;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Retruxel;

public partial class MainWindow : Window
{
    private readonly BuildConsoleView _buildConsoleView = new();
    private readonly ProjectManager _projectManager = new();
    private bool _isDraggingOverlay = false;
    private Point _overlayDragStart;

    public MainWindow()
    {
        InitializeComponent();
        TxtVersion.Text = $"v{GetAppVersion()}";
        WelcomeView.OnProjectCreated += OnProjectCreated;
        WelcomeView.OnAboutRequested += () => ShowOverlay("ABOUT", new AboutView());
        
        // Ctrl+S to save
        KeyDown += MainWindow_KeyDown;
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}-alpha" : "0.0.0-alpha";
    }

    private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            await SaveProjectAsync();
            e.Handled = true;
        }
    }

    private async Task SaveProjectAsync()
    {
        if (_projectManager.CurrentProject is not null)
        {
            await _projectManager.SaveAsync();
            // TODO: Show save feedback in status bar
        }
    }

    /// <summary>
    /// Shows a view as an overlay on top of the current screen.
    /// </summary>
    private void ShowOverlay(string title, object content)
    {
        OverlayTitle.Text = title;
        OverlayContentPresenter.Content = content;
        OverlayContent.Margin = new Thickness(40); // Reset position
        OverlayLayer.Visibility = Visibility.Visible;
        
        // Block interaction with background
        TitleBar.IsEnabled = false;
        StatusBar.IsEnabled = false;
        WelcomeView.IsEnabled = false;
        SceneEditorView.IsEnabled = false;
    }

    /// <summary>
    /// Closes the current overlay and returns to the base layer.
    /// </summary>
    private void CloseOverlay_Click(object sender, RoutedEventArgs e)
    {
        OverlayLayer.Visibility = Visibility.Collapsed;
        OverlayContentPresenter.Content = null;
        
        // Re-enable interaction with background
        TitleBar.IsEnabled = true;
        StatusBar.IsEnabled = true;
        WelcomeView.IsEnabled = true;
        SceneEditorView.IsEnabled = true;
    }

    /// <summary>
    /// Called when a project is created — shows Build Console as overlay.
    /// </summary>
    private async void OnProjectCreated(RetruxelProject project)
    {
        var target = Services.TargetRegistry.GetTargetById(project.TargetId);
        if (target is null)
        {
            MessageBox.Show($"Target '{project.TargetId}' not found.", "Retruxel", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        // Initialize ModuleLoader
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var moduleLoader = new ModuleLoader(basePath);
        moduleLoader.RegisterBuiltinModules(target);
        moduleLoader.LoadCompatible(target.TargetId);
        
        _projectManager.CurrentProject = project;
        
        // Show Scene Editor as main view
        WelcomeView.Visibility = Visibility.Collapsed;
        SceneEditorView.Visibility = Visibility.Visible;
        BtnHome.Visibility = Visibility.Visible;
        SceneEditorView.SetProjectManager(_projectManager);
        SceneEditorView.SetModuleLoader(moduleLoader);
        SceneEditorView.Initialize(project, target);
        
        // Save after initialization
        await _projectManager.SaveAsync();
        
        // Add to recent projects
        AddToRecentProjects(project);
        
        // Connect Generate ROM button
        SceneEditorView.OnGenerateRomRequested -= OnGenerateRomRequested;
        SceneEditorView.OnGenerateRomRequested += OnGenerateRomRequested;
    }

    private void AddToRecentProjects(RetruxelProject project)
    {
        var settings = SettingsService.Load();
        var projectPath = Path.Combine(project.ProjectPath, project.Name + ".rtrxproject");
        
        settings.General.RecentProjects.Remove(projectPath);
        settings.General.RecentProjects.Insert(0, projectPath);
        
        if (settings.General.RecentProjects.Count > 5)
            settings.General.RecentProjects = settings.General.RecentProjects.Take(5).ToList();
        
        SettingsService.Save(settings);
    }

    private async void OnGenerateRomRequested(RetruxelProject project)
    {
        await SaveProjectAsync();
        ShowOverlay("BUILD CONSOLE", _buildConsoleView);
        await _buildConsoleView.BuildAsync(project);
    }

    private async void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        // Check for unsaved changes
        if (_projectManager.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before returning to the Welcome screen?",
                "Retruxel",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                await SaveProjectAsync();
        }

        // Return to Welcome view
        SceneEditorView.Visibility = Visibility.Collapsed;
        WelcomeView.Visibility = Visibility.Visible;
        BtnHome.Visibility = Visibility.Collapsed;
        
        // Clear current project
        _projectManager.CurrentProject = null;
        
        // Refresh recent projects list
        WelcomeView.RefreshRecentProjects();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_projectManager.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Retruxel",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                await SaveProjectAsync();
        }

        Close();
    }

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OverlayTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            _isDraggingOverlay = true;
            _overlayDragStart = e.GetPosition(OverlayLayer);
            OverlayTitleBar.CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDraggingOverlay && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(OverlayLayer);
            var offset = currentPos - _overlayDragStart;

            var currentMargin = OverlayContent.Margin;
            OverlayContent.Margin = new Thickness(
                currentMargin.Left + offset.X,
                currentMargin.Top + offset.Y,
                currentMargin.Right - offset.X,
                currentMargin.Bottom - offset.Y);

            _overlayDragStart = currentPos;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isDraggingOverlay)
        {
            _isDraggingOverlay = false;
            OverlayTitleBar.ReleaseMouseCapture();
        }
    }

}