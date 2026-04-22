using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace Retruxel.Toolchain.Builders;

/// <summary>
/// NES toolchain builder using cc65 + neslib.
/// Extracts embedded binaries to %AppData%\Retruxel\toolchain\ on first run.
/// 
/// Build pipeline:
///   1. cc65  — compiles each .c file to .s (6502 assembly)
///   2. ca65  — assembles each .s file + crt0.s to .o
///   3. ld65  — links all .o files with nes.lib using nes.cfg → .nes ROM
/// </summary>
public class NesToolchainBuilder : IToolchainBuilder
{
    private static readonly string ToolchainPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "toolchain");

    public string TargetId => "nes";
    public string DisplayName => "cc65 + neslib";
    public string Version => "2.19";

    public async Task ExtractAsync(IProgress<string> progress)
    {
        Directory.CreateDirectory(ToolchainPath);
        progress.Report("EXTRACTING: NES toolchain binaries...");

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("Retruxel.Toolchain."));

        foreach (var resource in resources)
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

        progress.Report("DONE: NES toolchain ready.");
    }

    public async Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        var result = new BuildResult { StartedAt = DateTime.Now };
        var log = new List<BuildLogEntry>();

        try
        {
            var srcDirectory = Path.Combine(context.OutputDirectory, "src");
            Directory.CreateDirectory(srcDirectory);

            foreach (var file in context.SourceFiles)
            {
                var path = Path.Combine(srcDirectory, file.FileName);
                await File.WriteAllTextAsync(path, file.Content);
                log.Add(new BuildLogEntry { Level = BuildLogLevel.Info, Message = $"WRITTEN: {file.FileName}" });
            }

            var cc65 = Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "cc65.exe");
            var ca65 = Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "ca65.exe");
            var ld65 = Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "ld65.exe");
            var crt0 = Path.Combine(ToolchainPath, "sdks", "nes", "include", "crt0.s");
            var nesLib = Path.Combine(ToolchainPath, "sdks", "nes", "lib", "nes.lib");
            var nesCfg = Path.Combine(ToolchainPath, "sdks", "nes", "lib", "nes.cfg");
            var includeDir = Path.Combine(ToolchainPath, "sdks", "nes", "include");

            // Create NES configuration file
            var nesConfigPath = Path.Combine(srcDirectory, "nes_config.s");
            await File.WriteAllTextAsync(nesConfigPath,
                "; NES cartridge configuration\n" +
                ".segment \"HEADER\"\n" +
                "\n" +
                ".export NES_MAPPER: absolute\n" +
                ".export NES_PRG_BANKS: absolute\n" +
                ".export NES_CHR_BANKS: absolute\n" +
                ".export NES_MIRRORING: absolute\n" +
                "\n" +
                "NES_MAPPER     = 0\n" +
                "NES_PRG_BANKS  = 2\n" +
                "NES_CHR_BANKS  = 1\n" +
                "NES_MIRRORING  = 1\n");

            // Create CHR data file
            var chrPath = Path.Combine(ToolchainPath, "sdks", "nes", "chr", "font.chr");
            var chrAsmPath = Path.Combine(srcDirectory, "chr_data.s");
            var chrData = await File.ReadAllBytesAsync(chrPath);
            var chrAsm = new System.Text.StringBuilder();
            chrAsm.AppendLine("; CHR-ROM data");
            chrAsm.AppendLine(".segment \"CHARS\"");
            chrAsm.AppendLine(".byte " + string.Join(", ", chrData.Select(b => $"${b:X2}")));
            await File.WriteAllTextAsync(chrAsmPath, chrAsm.ToString());

            var objectFiles = new List<string>();

            // Step 1: cc65 — compile each .c to .s
            foreach (var srcFile in context.SourceFiles.Where(f => f.FileName.EndsWith(".c")))
            {
                var cPath = Path.Combine(srcDirectory, srcFile.FileName);
                var sPath = Path.ChangeExtension(cPath, ".s");

                progress.Report($"COMPILING: {srcFile.FileName}");

                var cc65Args = $"-t nes -O --include-dir \"{includeDir}\" \"{cPath}\" -o \"{sPath}\"";
                var cc65Ok = await RunProcessAsync(cc65, cc65Args, srcDirectory, log, progress);

                if (!cc65Ok)
                {
                    result.Success = false;
                    result.Log = log;
                    result.FinishedAt = DateTime.Now;
                    return result;
                }

                // Step 2a: ca65 — assemble each .s
                var oPath = Path.ChangeExtension(cPath, ".o");
                progress.Report($"ASSEMBLING: {Path.GetFileName(sPath)}");

                var ca65Args = $"--include-dir \"{includeDir}\" \"{sPath}\" -o \"{oPath}\"";
                var ca65Ok = await RunProcessAsync(ca65, ca65Args, srcDirectory, log, progress);

                if (!ca65Ok)
                {
                    result.Success = false;
                    result.Log = log;
                    result.FinishedAt = DateTime.Now;
                    return result;
                }

                objectFiles.Add(oPath);
            }

            // Step 2b: ca65 — assemble crt0.s
            var crt0ObjPath = Path.Combine(srcDirectory, "crt0.o");
            progress.Report("ASSEMBLING: crt0.s");

            var crt0Args = $"--include-dir \"{includeDir}\" \"{crt0}\" -o \"{crt0ObjPath}\"";
            var crt0Ok = await RunProcessAsync(ca65, crt0Args, srcDirectory, log, progress);

            if (!crt0Ok)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Step 2c: ca65 — assemble nes_config.s
            var nesConfigObjPath = Path.Combine(srcDirectory, "nes_config.o");
            progress.Report("ASSEMBLING: nes_config.s");

            var nesConfigArgs = $"\"{nesConfigPath}\" -o \"{nesConfigObjPath}\"";
            var nesConfigOk = await RunProcessAsync(ca65, nesConfigArgs, srcDirectory, log, progress);

            if (!nesConfigOk)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Step 2d: ca65 — assemble chr_data.s
            var chrDataObjPath = Path.Combine(srcDirectory, "chr_data.o");
            progress.Report("ASSEMBLING: chr_data.s");

            var chrDataArgs = $"\"{chrAsmPath}\" -o \"{chrDataObjPath}\"";
            var chrDataOk = await RunProcessAsync(ca65, chrDataArgs, srcDirectory, log, progress);

            if (!chrDataOk)
            {
                result.Success = false;
                result.Log = log;
                result.FinishedAt = DateTime.Now;
                return result;
            }

            // Step 3: ld65 — link everything
            var romName = context.BuildParameters.TryGetValue("romName", out var n)
                ? n.ToString()! : "output";

            var romPath = Path.Combine(context.OutputDirectory, $"{romName}.nes");
            var allObjects = string.Join(" ", new[] { $"\"{crt0ObjPath}\"", $"\"{nesConfigObjPath}\"", $"\"{chrDataObjPath}\"" }
                .Concat(objectFiles.Select(o => $"\"{o}\"")));

            progress.Report("LINKING...");

            var ld65Args = $"-C \"{nesCfg}\" -o \"{romPath}\" {allObjects} \"{nesLib}\"";
            var linkOk = await RunProcessAsync(ld65, ld65Args, srcDirectory, log, progress);

            // Não anexar CHR - ele já está incluído via segmento CHARS
            result.Success = linkOk && File.Exists(romPath);
            result.RomPath = result.Success ? romPath : null;

            if (result.Success)
            {
                var romSize = (int)new FileInfo(romPath).Length;

                // NES ROM structure: 16-byte header + PRG-ROM + CHR-ROM
                // Header bytes 4-5 indicate PRG/CHR bank counts
                var romBytes = await File.ReadAllBytesAsync(romPath);
                var prgBanks = romBytes[4]; // Each bank = 16KB
                var chrBanks = romBytes[5]; // Each bank = 8KB

                var prgSize = prgBanks * 16384;
                var chrSize = chrBanks * 8192;

                result.BankUsage["prg"] = prgSize;
                result.BankUsage["chr"] = chrSize;

                result.RomMd5 = await ComputeHashAsync(romPath, MD5.Create());
                result.RomSha256 = await ComputeHashAsync(romPath, SHA256.Create());
                log.Add(new BuildLogEntry
                {
                    Level = BuildLogLevel.Success,
                    Message = $"ROM generated: {romSize / 1024}KB (PRG: {prgSize / 1024}KB, CHR: {chrSize / 1024}KB) — {romPath}"
                });
                progress.Report($"SUCCESS: {romName}.nes ({romSize / 1024}KB)");
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
        var required = new[]
        {
            Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "cc65.exe"),
            Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "ca65.exe"),
            Path.Combine(ToolchainPath, "compilers", "cc65", "bin", "ld65.exe"),
            Path.Combine(ToolchainPath, "sdks", "nes", "include", "crt0.s"),
            Path.Combine(ToolchainPath, "sdks", "nes", "include", "neslib.h"),
            Path.Combine(ToolchainPath, "sdks", "nes", "lib", "nes.lib"),
            Path.Combine(ToolchainPath, "sdks", "nes", "lib", "nes.cfg"),
        };

        return await Task.FromResult(required.All(File.Exists));
    }

    private string? ExtractRelativePath(string resourceName)
    {
        if (resourceName.StartsWith("Retruxel.Toolchain.Compilers.Cc65.Resources.bin."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.Compilers.Cc65.Resources.bin.", "");
            return Path.Combine("compilers", "cc65", "bin", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.SDKs.Nes.Resources.include."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.SDKs.Nes.Resources.include.", "");
            return Path.Combine("sdks", "nes", "include", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.SDKs.Nes.Resources.lib."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.SDKs.Nes.Resources.lib.", "");
            return Path.Combine("sdks", "nes", "lib", fileName);
        }
        if (resourceName.StartsWith("Retruxel.Toolchain.SDKs.Nes.Resources.chr."))
        {
            var fileName = resourceName.Replace("Retruxel.Toolchain.SDKs.Nes.Resources.chr.", "");
            return Path.Combine("sdks", "nes", "chr", fileName);
        }
        return null;
    }

    private async Task<bool> RunProcessAsync(string exe, string args, string workingDir,
        List<BuildLogEntry> log, IProgress<string> progress)
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

    private static async Task<string> ComputeHashAsync(string filePath, HashAlgorithm hasher)
    {
        using (hasher)
        using (var stream = File.OpenRead(filePath))
        {
            var hash = await Task.Run(() => hasher.ComputeHash(stream));
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
