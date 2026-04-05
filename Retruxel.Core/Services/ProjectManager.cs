using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.IO;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Manages the lifecycle of a Retruxel project.
/// Responsible for creating, loading, saving and validating .rtrxproject files.
/// </summary>
public class ProjectManager
{
    private const string ProjectFileExtension = ".rtrxproject";

    /// <summary>The currently loaded project. Null if no project is open.</summary>
    public RetruxelProject? CurrentProject { get; set; }

    /// <summary>Whether there are unsaved changes in the current project.</summary>
    public bool HasUnsavedChanges { get; private set; }

    /// <summary>
    /// Raised when the current project changes — opened, created or closed.
    /// </summary>
    public event EventHandler<RetruxelProject?>? ProjectChanged;

    /// <summary>
    /// Creates a new project from a target and template.
    /// Creates the project directory if it doesn't exist.
    /// </summary>
    public RetruxelProject CreateProject(
        string name,
        string projectPath,
        ITarget target,
        ProjectTemplate template)
    {
        // Create project directory
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
        Directory.CreateDirectory(Path.Combine(projectPath, "build"));

        var project = new RetruxelProject
        {
            Name = name,
            ProjectPath = projectPath,
            TargetId = target.TargetId,
            TemplateId = template.TemplateId,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now,
            DefaultModules = template.DefaultModules.ToList(),
            Parameters = new Dictionary<string, object>(template.DefaultParameters),
            Scenes = [
                new SceneData
                {
                    SceneId = Guid.NewGuid().ToString(),
                    SceneName = "Main",
                    Elements = []
                }
            ]
        };

        CurrentProject = project;
        HasUnsavedChanges = true;
        ProjectChanged?.Invoke(this, project);

        return project;
    }

    /// <summary>
    /// Saves the current project to its .rtrxproject file.
    /// </summary>
    public async Task SaveAsync()
    {
        if (CurrentProject is null)
            throw new InvalidOperationException("No project is currently open.");

        CurrentProject.ModifiedAt = DateTime.Now;

        var json = JsonSerializer.Serialize(CurrentProject, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var filePath = Path.Combine(
            CurrentProject.ProjectPath,
            CurrentProject.Name + ProjectFileExtension);

        await File.WriteAllTextAsync(filePath, json);
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Loads a project from a .rtrxproject file.
    /// </summary>
    public async Task<RetruxelProject> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project file not found.", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<RetruxelProject>(json)
            ?? throw new InvalidDataException("Failed to deserialize project file.");

        CurrentProject = project;
        HasUnsavedChanges = false;
        ProjectChanged?.Invoke(this, project);

        return project;
    }

    /// <summary>
    /// Closes the current project.
    /// </summary>
    public void Close()
    {
        CurrentProject = null;
        HasUnsavedChanges = false;
        ProjectChanged?.Invoke(this, null);
    }

    /// <summary>
    /// Marks the current project as having unsaved changes.
    /// Called by modules whenever their state is modified.
    /// </summary>
    public void MarkDirty()
    {
        if (!HasUnsavedChanges)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectManager] MarkDirty called from: {Environment.StackTrace}");
        }
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Clears the dirty flag without saving.
    /// Used after explicit save operations.
    /// </summary>
    public void ClearDirtyFlag()
    {
        HasUnsavedChanges = false;
    }
}