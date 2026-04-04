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

    public async Task ExtractAsync(IProgress<string> progress)
    {
        Directory.CreateDirectory(ToolchainPath);
        progress.Report("EXTRACTING: SMS toolchain binaries...");

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        // Debug — log all found resources
        foreach (var r in resources)
            progress.Report($"FOUND_RESOURCE: {r}");

        foreach (var resource in resources
            .Where(r => r.StartsWith("Retruxel.Target.SMS")))
        {
            // Strip namespace prefix to get relative path
            var relativePath = resource
                .Replace("Retruxel.Target.SMS.Toolchain.Resources.", "")
                .Replace('.', Path.DirectorySeparatorChar);

            // Restore original extension — last segment after final dot
            var parts = resource.Split('.');
            var ext = parts[^1];
            var nameWithoutExt = string.Join(".", parts[..^1]);
            var fileName = nameWithoutExt
                .Replace("Retruxel.Target.SMS.Toolchain.Resources.", "")
                .Replace('.', Path.DirectorySeparatorChar) + "." + ext;

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

            // Write source files
            foreach (var file in context.SourceFiles)
            {
                var path = Path.Combine(context.OutputDirectory, file.FileName);
                await File.WriteAllTextAsync(path, file.Content);
            }

            // Write binary assets
            foreach (var asset in context.Assets)
            {
                var path = Path.Combine(context.OutputDirectory, asset.FileName);
                await File.WriteAllBytesAsync(path, asset.Data);
            }

            var sdccPath = Path.Combine(ToolchainPath, "sdcc.exe");
            var smslibPath = Path.Combine(ToolchainPath, "SMSlib");
            var smslibSrcPath = Path.Combine(smslibPath, "src");
            var crt0Path = Path.Combine(smslibPath, "crt0", "crt0_sms.rel");
            var smslibLib = Path.Combine(smslibPath, "SMSlib.lib");
            var z80Lib = Path.Combine(ToolchainPath, "z80.lib");
            var peepRules = Path.Combine(smslibSrcPath, "peep-rules.txt");

            var sourceFiles = context.SourceFiles
                .Where(f => f.FileType == GeneratedFileType.Source)
                .ToList();

            // Step 1 — Compile each .c to .rel
            foreach (var file in sourceFiles)
            {
                progress.Report($"COMPILE: {file.FileName}");
                var compileArgs = $"-mz80 --no-std-crt0 " +
                                  $"--sdcccall 1 " +
                                  $"-I\"{smslibSrcPath}\" " +
                                  $"--peep-file \"{peepRules}\" " +
                                  $"-c {file.FileName}";

                var ok = await RunProcessAsync(sdccPath, compileArgs,
                    context.OutputDirectory, log, progress);

                if (!ok)
                {
                    result.Success = false;
                    result.Log = log;
                    result.FinishedAt = DateTime.Now;
                    return result;
                }
            }

            // Step 2 — Link all .rel files into .ihx
            var relFiles = sourceFiles
                .Select(f => Path.GetFileNameWithoutExtension(f.FileName) + ".rel");

            var romName = "output";
            var linkArgs = $"-mz80 --no-std-crt0 --data-loc 0xC000 " +
                           $"--sdcccall 1 " +
                           $"-o {romName}.ihx " +
                           $"\"{crt0Path}\" " +
                           $"{string.Join(" ", relFiles)} " +
                           $"\"{smslibLib}\" " +
                           $"\"{z80Lib}\"";

            progress.Report("LINK: linking objects...");
            var linkOk = await RunProcessAsync(sdccPath, linkArgs,
                context.OutputDirectory, log, progress);

            if (!linkOk)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Step 3 — Convert .ihx to .sms
            var ihx2smsPath = Path.Combine(ToolchainPath, "ihx2sms.exe");
            progress.Report("CONVERT: generating ROM...");
            var convertOk = await RunProcessAsync(ihx2smsPath,
                $"{romName}.ihx {romName}.sms",
                context.OutputDirectory, log, progress);

            var romPath = Path.Combine(context.OutputDirectory, $"{romName}.sms");
            result.Success = convertOk && File.Exists(romPath);
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
        var sourceFiles = context.SourceFiles
            .Where(f => f.FileType == GeneratedFileType.Source)
            .Select(f => f.FileName);

        var smslibPath = Path.Combine(ToolchainPath, "SMSlib");
        var smslibSrcPath = Path.Combine(smslibPath, "src");
        var crt0Path = Path.Combine(smslibPath, "crt0", "crt0_sms.rel");
        var smslibLib = Path.Combine(smslibPath, "SMSlib.lib");
        var peepRules = Path.Combine(smslibSrcPath, "peep-rules.txt");

        // Compile each source file to .rel first
        return $"-mz80 --no-std-crt0 --data-loc 0xC000 " +
               $"-I\"{smslibSrcPath}\" " +
               $"--peep-file \"{peepRules}\" " +
               $"-c {string.Join(" ", sourceFiles)}";
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