using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Retruxel.Core.Services;

/// <summary>
/// Maintains a disk cache of generated source and asset hashes.
/// CodeGenerator prepares the plan; toolchains use it to skip unchanged work.
/// </summary>
public static class IncrementalBuildCache
{
    private const int CurrentVersion = 1;
    private const string CacheDirectoryName = ".retruxel-cache";
    private const string ManifestFileName = "incremental-build.json";

    public static IncrementalBuildInfo Prepare(BuildContext context, IProgress<string>? progress = null)
    {
        var cacheDirectory = Path.Combine(context.OutputDirectory, CacheDirectoryName);
        Directory.CreateDirectory(cacheDirectory);

        var manifestPath = Path.Combine(cacheDirectory, ManifestFileName);
        var previous = LoadManifest(manifestPath);
        var current = CreateManifest(context, manifestPath);

        current.HasPreviousCache = previous is not null;

        if (previous is null ||
            previous.Version != CurrentVersion ||
            !string.Equals(previous.TargetId, current.TargetId, StringComparison.OrdinalIgnoreCase))
        {
            current.ChangedSourceFiles = current.SourceHashes.Keys.OrderBy(k => k).ToList();
            current.ChangedAssetFiles = current.AssetHashes.Keys.OrderBy(k => k).ToList();
        }
        else
        {
            current.ChangedSourceFiles = GetChangedFiles(current.SourceHashes, previous.SourceHashes);
            current.ChangedAssetFiles = GetChangedFiles(current.AssetHashes, previous.AssetHashes);
            current.RemovedSourceFiles = GetRemovedFiles(current.SourceHashes, previous.SourceHashes);
            current.RemovedAssetFiles = GetRemovedFiles(current.AssetHashes, previous.AssetHashes);
        }

        current.HeaderChanged =
            current.ChangedSourceFiles.Concat(current.RemovedSourceFiles)
                .Any(f => f.EndsWith(".h", StringComparison.OrdinalIgnoreCase));

        context.IncrementalBuild = current;

        var changedSources = current.HeaderChanged
            ? current.SourceHashes.Keys.Count(f => f.EndsWith(".c", StringComparison.OrdinalIgnoreCase))
            : current.ChangedSourceFiles.Count(f => f.EndsWith(".c", StringComparison.OrdinalIgnoreCase));

        progress?.Report(current.HasPreviousCache
            ? $"INFO: Incremental cache ready - {changedSources} source file(s), {current.ChangedAssetFiles.Count} asset(s) changed."
            : "INFO: Incremental cache initialized - first build will compile all generated sources.");

        return current;
    }

    public static async Task WriteInputsAsync(
        BuildContext context,
        string sourceDirectory,
        List<BuildLogEntry>? log = null,
        IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(sourceDirectory);
        var incremental = context.IncrementalBuild;

        DeleteRemovedFiles(sourceDirectory, incremental?.RemovedSourceFiles);
        DeleteRemovedFiles(sourceDirectory, incremental?.RemovedAssetFiles);

        foreach (var file in context.SourceFiles)
        {
            var path = Path.Combine(sourceDirectory, file.FileName);
            if (ShouldWriteSource(incremental, file, path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, file.Content);
                log?.Add(new BuildLogEntry { Level = BuildLogLevel.Info, Message = $"WRITTEN: {file.FileName}" });
            }
            else
            {
                progress?.Report($"CACHE: source unchanged - {file.FileName}");
            }
        }

        foreach (var asset in context.Assets)
        {
            var path = Path.Combine(sourceDirectory, asset.FileName);
            if (ShouldWriteAsset(incremental, asset, path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, asset.Data);
                log?.Add(new BuildLogEntry { Level = BuildLogLevel.Info, Message = $"WRITTEN: {asset.FileName}" });
            }
            else
            {
                progress?.Report($"CACHE: asset unchanged - {asset.FileName}");
            }
        }
    }

    public static bool ShouldCompileSource(BuildContext context, GeneratedFile sourceFile, string sourceDirectory, string objectExtension)
    {
        var incremental = context.IncrementalBuild;
        if (incremental is null || !incremental.Enabled || !incremental.HasPreviousCache)
            return true;

        if (incremental.HeaderChanged)
            return true;

        if (incremental.ChangedSourceFiles.Contains(sourceFile.FileName, StringComparer.OrdinalIgnoreCase))
            return true;

        var objectPath = Path.Combine(
            sourceDirectory,
            Path.GetFileNameWithoutExtension(sourceFile.FileName) + objectExtension);

        return !File.Exists(objectPath);
    }

    public static async Task CommitAsync(BuildContext context)
    {
        var incremental = context.IncrementalBuild;
        if (incremental is null || !incremental.Enabled || string.IsNullOrWhiteSpace(incremental.ManifestPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(incremental.ManifestPath)!);

        var json = JsonSerializer.Serialize(incremental, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(incremental.ManifestPath, json);
    }

    private static IncrementalBuildInfo CreateManifest(BuildContext context, string manifestPath)
    {
        return new IncrementalBuildInfo
        {
            Version = CurrentVersion,
            TargetId = context.TargetId,
            ManifestPath = manifestPath,
            SourceHashes = context.SourceFiles
                .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => HashText(g.Last().Content), StringComparer.OrdinalIgnoreCase),
            AssetHashes = context.Assets
                .GroupBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => HashBytes(g.Last().Data), StringComparer.OrdinalIgnoreCase)
        };
    }

    private static IncrementalBuildInfo? LoadManifest(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<IncrementalBuildInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> GetChangedFiles(
        Dictionary<string, string> current,
        Dictionary<string, string> previous)
    {
        return current
            .Where(kv => !previous.TryGetValue(kv.Key, out var oldHash) || oldHash != kv.Value)
            .Select(kv => kv.Key)
            .OrderBy(k => k)
            .ToList();
    }

    private static List<string> GetRemovedFiles(
        Dictionary<string, string> current,
        Dictionary<string, string> previous)
    {
        return previous.Keys
            .Where(k => !current.ContainsKey(k))
            .OrderBy(k => k)
            .ToList();
    }

    private static bool ShouldWriteSource(IncrementalBuildInfo? incremental, GeneratedFile file, string path)
    {
        return incremental is null ||
               !incremental.HasPreviousCache ||
               incremental.ChangedSourceFiles.Contains(file.FileName, StringComparer.OrdinalIgnoreCase) ||
               !File.Exists(path);
    }

    private static bool ShouldWriteAsset(IncrementalBuildInfo? incremental, GeneratedAsset asset, string path)
    {
        return incremental is null ||
               !incremental.HasPreviousCache ||
               incremental.ChangedAssetFiles.Contains(asset.FileName, StringComparer.OrdinalIgnoreCase) ||
               !File.Exists(path);
    }

    private static void DeleteRemovedFiles(string sourceDirectory, IEnumerable<string>? fileNames)
    {
        if (fileNames is null)
            return;

        foreach (var fileName in fileNames)
        {
            var path = Path.GetFullPath(Path.Combine(sourceDirectory, fileName));
            var root = Path.GetFullPath(sourceDirectory);
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static string HashText(string text)
    {
        return HashBytes(Encoding.UTF8.GetBytes(text));
    }

    private static string HashBytes(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
