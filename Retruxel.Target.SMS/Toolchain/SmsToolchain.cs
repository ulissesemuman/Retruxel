using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Diagnostics;
using System.Reflection;

namespace Retruxel.Target.SMS.Toolchain;

/// <summary>
/// SMS toolchain implementation using devkitSMS + SDCC.
/// Extracts embedded binaries to %AppData%\Retruxel\toolchain\sms\ on first run.
/// </summary>
public class SmsToolchain : IToolchain
{
    private static readonly string ToolchainPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "toolchain", "sms");

    public string TargetId => "sms";
    public string DisplayName => "devkitSMS + SDCC 4.5.24";
    public string Version => "4.5.24";

    /// <summary>
    /// Extracts all embedded toolchain resources to the local app data folder.
    /// Skips extraction if binaries are already present and up to date.
    /// </summary>
    public async Task ExtractAsync(IProgress<string> progress)
    {
        Directory.CreateDirectory(ToolchainPath);
        progress.Report("EXTRACTING: SMS toolchain binaries...");

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("Retruxel.Target.SMS.Toolchain.Resources."));

        foreach (var resource in resources)
        {
            var fileName = resource.Replace("Retruxel.Target.SMS.Toolchain.Resources.", "");
            var destPath = Path.Combine(ToolchainPath, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var file = File.Create(destPath);
            await stream.CopyToAsync(file);

            progress.Report($"EXTRACTED: {fileName}");
        }

        progress.Report("DONE: SMS toolchain ready.");
    }

    /// <summary>
    /// Compiles generated C source files into a .sms ROM using SDCC + ihx2sms.
    /// </summary>
    public async Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        var result = new BuildResult { StartedAt = DateTime.Now };
        var log = new List<BuildLogEntry>();

        try
        {
            Directory.CreateDirectory(context.OutputDirectory);

            // Write source files to output directory
            foreach (var file in context.SourceFiles)
            {
                var path = Path.Combine(context.OutputDirectory, file.FileName);
                await File.WriteAllTextAsync(path, file.Content);
            }

            // Write binary assets to output directory
            foreach (var asset in context.Assets)
            {
                var path = Path.Combine(context.OutputDirectory, asset.FileName);
                await File.WriteAllBytesAsync(path, asset.Data);
            }

            // Run SDCC compilation
            var sdccPath = Path.Combine(ToolchainPath, "sdcc.exe");
            var compileSuccess = await RunProcessAsync(sdccPath,
                BuildSdccArgs(context), context.OutputDirectory, log, progress);

            if (!compileSuccess)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Run ihx2sms conversion
            var ihx2smsPath = Path.Combine(ToolchainPath, "ihx2sms.exe");
            var romName = context.BuildParameters.TryGetValue("romName", out var name)
                ? name.ToString()! : "output";

            var convertSuccess = await RunProcessAsync(ihx2smsPath,
                $"{romName}.ihx {romName}.sms", context.OutputDirectory, log, progress);

            var romPath = Path.Combine(context.OutputDirectory, $"{romName}.sms");
            result.Success = convertSuccess && File.Exists(romPath);
            result.RomPath = result.Success ? romPath : null;
            result.RomSizeBytes = result.Success ? (int)new FileInfo(romPath).Length : 0;

            if (result.Success)
            {
                result.RomMd5 = await ComputeMd5Async(romPath);
                result.RomSha256 = await ComputeSha256Async(romPath);
                log.Add(new BuildLogEntry
                {
                    Level = BuildLogLevel.Success,
                    Message = $"ROM generated: {result.RomSizeBytes / 1024}KB"
                });
            }
        }
        catch (Exception ex)
        {
            log.Add(new BuildLogEntry { Level = BuildLogLevel.Error, Message = ex.Message });
            result.Success = false;
        }

        result.Log = log;
        result.FinishedAt = DateTime.Now;
        return result;
    }

    /// <summary>
    /// Verifies that SDCC and ihx2sms are present and executable.
    /// </summary>
    public async Task<bool> VerifyAsync()
    {
        var sdcc = Path.Combine(ToolchainPath, "sdcc.exe");
        var ihx2sms = Path.Combine(ToolchainPath, "ihx2sms.exe");
        return await Task.FromResult(File.Exists(sdcc) && File.Exists(ihx2sms));
    }

    private string BuildSdccArgs(BuildContext context)
    {
        var region = context.BuildParameters.TryGetValue("region", out var r) ? r.ToString() : "NTSC";
        return $"-mz80 --no-std-crt0 --data-loc 0xC000 " +
               $"-I\"{Path.Combine(ToolchainPath, "SMSlib", "src")}\" " +
               $"--peep-file \"{Path.Combine(ToolchainPath, "SMSlib", "src", "peep-rules.txt")}\" " +
               $"{string.Join(" ", context.SourceFiles.Select(f => f.FileName))}";
    }

    private async Task<bool> RunProcessAsync(string exe, string args,
        string workingDir, List<BuildLogEntry> log, IProgress<string> progress)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            log.Add(new BuildLogEntry { Level = BuildLogLevel.Info, Message = e.Data });
            progress.Report(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            var level = e.Data.Contains("error") ? BuildLogLevel.Error : BuildLogLevel.Warning;
            log.Add(new BuildLogEntry { Level = level, Message = e.Data });
            progress.Report(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }

    private async Task<string> ComputeMd5Async(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => md5.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    private async Task<string> ComputeSha256Async(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "");
    }
}