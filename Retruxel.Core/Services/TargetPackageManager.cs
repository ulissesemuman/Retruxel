using Retruxel.Core.Models;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Manages downloadable target packages.
/// 
/// Responsibilities:
/// - Fetch manifest from GitHub (or CDN)
/// - Check which targets are installed
/// - Download and extract target packages
/// - Verify checksums
/// - Uninstall targets
/// 
/// Package structure:
///   sms-0.7.1.zip
///   ├─ Retruxel.Target.SMS.dll
///   ├─ toolchain/
///   │  └─ (SDCC binaries)
///   └─ codegens/
///      └─ (declarative CodeGens)
/// </summary>
public class TargetPackageManager
{
    private const string ManifestUrl = "https://raw.githubusercontent.com/ulissesemuman/Retruxel/main/targets-manifest.json";
    
    private readonly string _targetsPath;
    private readonly string _toolchainPath;
    private readonly string _codegensPath;
    private readonly HttpClient _httpClient;

    public TargetPackageManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var retruxelData = Path.Combine(appData, "Retruxel");
        
        _targetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "targets");
        _toolchainPath = Path.Combine(retruxelData, "toolchain");
        _codegensPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Plugins", "CodeGens");
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Retruxel/0.7.1");
    }

    /// <summary>
    /// Fetches the manifest and returns all available targets with installation status.
    /// </summary>
    public async Task<List<TargetPackageInfo>> GetAllTargetsAsync()
    {
        try
        {
            var manifest = await DownloadManifestAsync();
            
            foreach (var target in manifest)
                target.IsInstalled = await IsInstalledAsync(target.TargetId);
            
            return manifest;
        }
        catch (Exception ex)
        {
            // Fallback: return empty list if manifest fetch fails
            // (offline mode or GitHub down)
            Console.WriteLine($"Failed to fetch manifest: {ex.Message}");
            return new List<TargetPackageInfo>();
        }
    }

    /// <summary>
    /// Checks if a target is currently installed.
    /// </summary>
    public async Task<bool> IsInstalledAsync(string targetId)
    {
        // Use convention: Retruxel.Target.{PascalCase}.dll
        // Convert targetId to PascalCase (e.g., "sms" -> "Sms", "sg1000" -> "Sg1000")
        var pascalCase = string.Concat(targetId.Split('-', '_').Select(s => 
            char.ToUpper(s[0]) + s.Substring(1).ToLower()));
        
        var dllPath = Path.Combine(_targetsPath, $"Retruxel.Target.{pascalCase}.dll");
        return await Task.FromResult(File.Exists(dllPath));
    }

    /// <summary>
    /// Downloads and installs a target package.
    /// </summary>
    public async Task DownloadAndInstallAsync(
        TargetPackageInfo target,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(0);

        // 1. Download ZIP to temp location
        var tempZip = Path.Combine(Path.GetTempPath(), $"{target.TargetId}-{target.Version}.zip");
        
        try
        {
            await DownloadFileAsync(target.DownloadUrl, tempZip, progress, cancellationToken);
            progress?.Report(0.7);

            // 2. Verify checksum
            if (!string.IsNullOrEmpty(target.Checksum))
            {
                var actualChecksum = await ComputeSha256Async(tempZip);
                if (!actualChecksum.Equals(target.Checksum, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Checksum verification failed. Package may be corrupted.");
            }
            progress?.Report(0.8);

            // 3. Extract to appropriate locations
            await ExtractPackageAsync(tempZip, target.TargetId);
            progress?.Report(0.95);

            // 4. Register target in TargetRegistry (dynamic loading)
            // This will be handled by TargetRegistry.Refresh() on next startup
            
            progress?.Report(1.0);
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }
    }

    /// <summary>
    /// Uninstalls a target package.
    /// </summary>
    public async Task UninstallAsync(string targetId)
    {
        await Task.Run(() =>
        {
            // Remove DLL using PascalCase convention
            var pascalCase = string.Concat(targetId.Split('-', '_').Select(s => 
                char.ToUpper(s[0]) + s.Substring(1).ToLower()));
            
            var dllPath = Path.Combine(_targetsPath, $"Retruxel.Target.{pascalCase}.dll");
            if (File.Exists(dllPath))
                File.Delete(dllPath);

            // Remove toolchain - scan for any toolchain directories that might be used by this target
            // This is safe because toolchains are shared (e.g., SDCC used by SMS, GG, SG1000, Coleco)
            // We only remove if no other targets are using it
            // For now, we skip toolchain removal to avoid breaking other targets
            // TODO: Implement reference counting for shared toolchains

            // Remove CodeGens
            var codegensDir = Path.Combine(_codegensPath, targetId);
            if (Directory.Exists(codegensDir))
                Directory.Delete(codegensDir, recursive: true);
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<TargetPackageInfo>> DownloadManifestAsync()
    {
        var json = await _httpClient.GetStringAsync(ManifestUrl);
        var manifest = JsonSerializer.Deserialize<TargetManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return manifest?.Targets ?? new List<TargetPackageInfo>();
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes > 0 && progress != null;

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (canReportProgress)
            {
                var progressPercentage = (double)totalRead / totalBytes * 0.7; // 0-70% for download
                progress!.Report(progressPercentage);
            }
        }
    }

    private async Task ExtractPackageAsync(string zipPath, string targetId)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue; // Skip directories

                string destinationPath;

                // Route files to appropriate locations
                if (entry.FullName.StartsWith("targets/"))
                {
                    // DLL goes to targets/
                    var relativePath = entry.FullName.Substring("targets/".Length);
                    destinationPath = Path.Combine(_targetsPath, relativePath);
                }
                else if (entry.FullName.StartsWith("toolchain/"))
                {
                    // Toolchain goes to %AppData%/Retruxel/toolchain/
                    var relativePath = entry.FullName.Substring("toolchain/".Length);
                    destinationPath = Path.Combine(_toolchainPath, relativePath);
                }
                else if (entry.FullName.StartsWith("codegens/"))
                {
                    // CodeGens go to plugins/CodeGens/
                    var relativePath = entry.FullName.Substring("codegens/".Length);
                    destinationPath = Path.Combine(_codegensPath, relativePath);
                }
                else
                {
                    continue; // Skip unknown files
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        });
    }

    private async Task<string> ComputeSha256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    // ── Internal models ───────────────────────────────────────────────────────

    private class TargetManifest
    {
        public string Version { get; set; } = "";
        public List<TargetPackageInfo> Targets { get; set; } = new();
    }
}
