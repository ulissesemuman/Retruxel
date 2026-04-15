using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Interface for code generation plugins.
/// Each plugin generates target-specific C code for a module.
/// </summary>
public interface ICodeGenPlugin
{
    /// <summary>Target platform this codegen supports (e.g., "sms", "nes")</summary>
    string TargetId { get; }
    
    /// <summary>
    /// Module ID this codegen is associated with (e.g., "entity", "enemy").
    /// If null, creates a TargetModule automatically with metadata from this plugin.
    /// </summary>
    string? ModuleId { get; }
    
    /// <summary>Display name (used when ModuleId is null)</summary>
    string DisplayName { get; }
    
    /// <summary>Category for grouping (used when ModuleId is null)</summary>
    string Category { get; }
    
    /// <summary>Whether this is a singleton module (used when ModuleId is null)</summary>
    bool IsSingleton { get; }
    
    /// <summary>
    /// Generates C source files from module JSON state.
    /// </summary>
    IEnumerable<GeneratedFile> Generate(string moduleJson);
    
    /// <summary>
    /// Validates module JSON and returns error messages.
    /// Empty list = valid.
    /// </summary>
    IEnumerable<string> Validate(string moduleJson);
}
