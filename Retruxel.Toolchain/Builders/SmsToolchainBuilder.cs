using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Diagnostics;
using System.Reflection;

namespace Retruxel.Toolchain.Builders;

/// <summary>
/// SMS toolchain builder using SDCC + devkitSMS + SMSlib.
/// Extracts embedded binaries to %AppData%\Retruxel\toolchain\ on first run.
/// </summary>
public class SmsToolchainBuilder : IToolchainBuilder
{
    private static readonly string ToolchainPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "toolchain");

    public string TargetId => "sms";
    public string DisplayName => "SDCC + devkitSMS + SMSlib";
    public string Version => "4.5.24";

    public async Task ExtractAsync(IProgress<string> progress)
    {
        Directory.CreateDirectory(ToolchainPath);
        progress.Report("EXTRACTING: SMS toolchain binaries...");

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        foreach (var resource in resources.Where(r => r.StartsWith("Retruxel.Toolchain.")))
        {
            var relativePath = ExtractRelativePath(resource);
            if (relativePath == null) continue;

            var destPath = Path.Combine(ToolchainPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var file = File.Create(destPath);
            await stream.CopyToAsync(file);

            progress.Report($"EXTRACTED: {relativePath}");
        }

        progress.Report("DONE: SMS toolchain ready.");
    }

    public async Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        var result = new BuildResult { StartedAt = DateTime.Now };
        var log = new List<BuildLogEntry>();

        var settings = await SettingsService.LoadAsync();
        var suppressWarnings = !settings.Targets.Sms.ShowToolchainWarnings;

        try
        {
            var srcDirectory = Path.Combine(context.OutputDirectory, "src");
            Directory.CreateDirectory(srcDirectory);

            foreach (var file in context.SourceFiles)
            {
                var path = Path.Combine(srcDirectory, file.FileName);
                await File.WriteAllTextAsync(path, file.Content);
            }

            foreach (var asset in context.Assets)
            {
                var path = Path.Combine(srcDirectory, asset.FileName);
                await File.WriteAllBytesAsync(path, asset.Data);
            }

            var sdccPath = Path.Combine(ToolchainPath, "compilers", "sdcc", "bin", "sdcc.exe");
            var includePath = Path.Combine(ToolchainPath, "sdks", "sega8bit", "include");
            var libPath = Path.Combine(ToolchainPath, "sdks", "sega8bit", "lib");
            var crt0Path = Path.Combine(libPath, "crt0_sms.rel");
            var smslibLib = Path.Combine(libPath, "SMSlib.lib");
            var z80Lib = Path.Combine(ToolchainPath, "compilers", "sdcc", "lib", "z80", "z80.lib");

            var sourceFiles = context.SourceFiles
                .Where(f => f.FileType == GeneratedFileType.Source)
                .ToList();

            // Step 1 — Compile each .c to .rel
            foreach (var file in sourceFiles)
            {
                progress.Report($"COMPILE: {file.FileName}");
                var compileArgs = $"-mz80 --no-std-crt0 --sdcccall 1 " +
                                  (suppressWarnings ? "--disable-warning 336 " : "") +
                                  $"-I\"{includePath}\" " +
                                  $"-c {file.FileName}";

                var ok = await RunProcessAsync(sdccPath, compileArgs, srcDirectory, log, progress, suppressWarnings);
                if (!ok)
                {
                    result.Success = false;
                    result.Log = log;
                    result.FinishedAt = DateTime.Now;
                    return result;
                }
            }

            // Step 2 — Link all .rel files into .ihx
            var relFiles = sourceFiles.Select(f => Path.GetFileNameWithoutExtension(f.FileName) + ".rel");
            var romName = "output";
            var linkArgs = $"-mz80 --no-std-crt0 --data-loc 0xC000 --sdcccall 1 " +
                           $"-o {romName}.ihx " +
                           $"\"{crt0Path}\" " +
                           $"{string.Join(" ", relFiles)} " +
                           $"\"{smslibLib}\" " +
                           $"\"{z80Lib}\"";

            progress.Report("LINK: linking objects...");
            var linkOk = await RunProcessAsync(sdccPath, linkArgs, srcDirectory, log, progress, suppressWarnings);
            if (!linkOk)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Step 3 — Convert .ihx to .sms
            var ihx2smsPath = Path.Combine(ToolchainPath, "utils", "sega", "bin", "ihx2sms.exe");
            var ihxPath = Path.Combine(srcDirectory, $"{romName}.ihx");
            var romPath = Path.Combine(context.OutputDirectory, $"{romName}.sms");

            progress.Report("CONVERT: generating ROM...");
            var convertOk = await RunProcessAsync(ihx2smsPath, $"\"{ihxPath}\" \"{romPath}\"",
                context.OutputDirectory, log, progress, suppressWarnings);

            result.Success = convertOk && File.Exists(romPath);
            result.RomPath = result.Success ? romPath : null;
            
            if (result.Success)
            {
                var romSize = (int)new FileInfo(romPath).Length;
                result.BankUsage["rom"] = romSize;
                
                result.RomMd5 = await ComputeMd5Async(romPath);
                result.RomSha256 = await ComputeSha256Async(romPath);
                log.Add(new BuildLogEntry
                {
                    Level = BuildLogLevel.Success,
                    Message = $"ROM generated: {romSize / 1024}KB"
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

    public async Task<bool> VerifyAsync()
    {
        var sdcc = Path.Combine(ToolchainPath, "compilers", "sdcc", "bin", "sdcc.exe");
        var ihx2sms = Path.Combine(ToolchainPath, "utils", "sega", "bin", "ihx2sms.exe");
        return await Task.FromResult(File.Exists(sdcc) && File.Exists(ihx2sms));
    }

    private string? ExtractRelativePath(string resourceName)
    {
        if (resourceName.StartsWith("Retruxel.Toolchain.Compilers.Sdcc.Resources.bin."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.Compilers.Sdcc.Resources.bin.", "");
            return Path.Combine("compilers", "sdcc", "bin", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.Compilers.Sdcc.Resources.lib.z80."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.Compilers.Sdcc.Resources.lib.z80.", "");
            return Path.Combine("compilers", "sdcc", "lib", "z80", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.SDKs.Sega8Bit.Resources.include."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.SDKs.Sega8Bit.Resources.include.", "");
            return Path.Combine("sdks", "sega8bit", "include", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.SDKs.Sega8Bit.Resources.lib."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.SDKs.Sega8Bit.Resources.lib.", "");
            return Path.Combine("sdks", "sega8bit", "lib", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.Utils.Sega.Resources.bin."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.Utils.Sega.Resources.bin.", "");
            return Path.Combine("utils", "sega", "bin", fileName);
        }
        return null;
    }

    private async Task<bool> RunProcessAsync(string exe, string args, string workingDir,
        List<BuildLogEntry> log, IProgress<string> progress, bool suppressWarnings = false)
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
            var isWarning = e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase);
            if (suppressWarnings && isWarning) return;
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
