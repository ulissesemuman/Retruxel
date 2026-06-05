using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Retruxel.Core.Services;

/// <summary>
/// Manages the lifecycle of a Retruxel project.
/// Responsible for creating, loading, saving and validating .rtrxproject files.
/// </summary>
public class ProjectManager
{
    private const string ProjectFileExtension = ".rtrxproject";
    private static readonly SemaphoreSlim _saveLock = new(1, 1);

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

        var mainSceneId = Guid.NewGuid().ToString();

        var project = new RetruxelProject
        {
            Name = name,
            ProjectPath = projectPath,
            TargetId = target.TargetId,
            TemplateId = template.TemplateId,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now,
            InitialSceneId = mainSceneId,
            InputPorts = BuildDefaultInputPorts(target),
            Scenes = [
                new SceneData
                {
                    SceneId = mainSceneId,
                    SceneName = "Main"
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
    /// Uses SemaphoreSlim to prevent concurrent writes.
    /// </summary>
    public async Task SaveAsync()
    {
        if (CurrentProject is null)
            throw new InvalidOperationException("No project is currently open.");

        await _saveLock.WaitAsync();
        try
        {
            CurrentProject.ModifiedAt = DateTime.Now;

            var json = JsonSerializer.Serialize(CurrentProject, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var filePath = Path.Combine(
                CurrentProject.ProjectPath,
                CurrentProject.Name + ProjectFileExtension);

            // Write to temp file first, then atomic rename
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);

            // Atomic replace
            File.Move(tempPath, filePath, overwrite: true);

            HasUnsavedChanges = false;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Loads a project from a .rtrxproject file.
    /// </summary>
    public async Task<RetruxelProject> LoadAsync(string filePath, ITarget? target = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project file not found.", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<RetruxelProject>(json)
            ?? throw new InvalidDataException("Failed to deserialize project file.");

        // Migrate: populate InputPorts from target defaults if missing (old project files)
        if (project.InputPorts.Count == 0)
        {
            var defaultTarget = TargetRegistry.GetTargetById(project.TargetId);
            if (defaultTarget is not null)
                project.InputPorts = BuildDefaultInputPorts(defaultTarget);
        }

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
    /// Builds the default InputPortBinding list from the target's hardware InputPort definitions.
    /// Called on project creation and as a migration fallback for old project files.
    /// </summary>
    public static List<InputPortBinding> BuildDefaultInputPorts(ITarget target)
        => target.GetInputPorts()
            .Select(port => new InputPortBinding
            {
                Id    = port.Id,
                Label = port.Label,
                Type  = port.Type,
                Buttons = port.Buttons
                    .Select(b => new InputButtonBinding
                    {
                        Id          = b.Id,
                        Label       = b.Label,
                        DevkitConst = b.DevkitConst
                    })
                    .ToList()
            })
            .ToList();

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