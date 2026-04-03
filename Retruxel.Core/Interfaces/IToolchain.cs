using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Contract for a target-specific build toolchain.
/// Responsible for compiling generated C code into a ROM file.
/// Each target provides its own toolchain implementation.
/// The toolchain binaries are embedded in the application and
/// extracted to %AppData%\Retruxel\toolchain\ on first run.
/// </summary>
public interface IToolchain
{
    /// <summary>Target identifier this toolchain compiles for. Ex: "sms", "nes"</summary>
    string TargetId { get; }

    /// <summary>Toolchain display name. Ex: "devkitSMS + SDCC 4.5.24"</summary>
    string DisplayName { get; }

    /// <summary>Current version of the embedded toolchain binaries.</summary>
    string Version { get; }

    /// <summary>
    /// Extracts embedded toolchain binaries to the local app data folder.
    /// Called once on first run or when a toolchain update is detected.
    /// </summary>
    Task ExtractAsync(IProgress<string> progress);

    /// <summary>
    /// Compiles a set of generated files into a ROM.
    /// Returns the build result including the ROM path, log output and any errors.
    /// </summary>
    Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress);

    /// <summary>
    /// Verifies that the extracted toolchain binaries are intact and functional.
    /// Called by the ToolchainValidator in Debug mode.
    /// </summary>
    Task<bool> VerifyAsync();
}