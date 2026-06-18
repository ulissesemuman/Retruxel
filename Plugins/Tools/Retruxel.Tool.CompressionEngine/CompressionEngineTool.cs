using Retruxel.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Retruxel.Tool.CompressionEngine;

/// <summary>
/// Compression engine for ROM assets.
///
/// Input keys:
///   data              byte[], string, int[] or JSON array (required)
///   algorithm         "auto", "none", "rle", "lz77" or "huffman" (default: auto)
///   assetType         "tiles", "maps", "text" or custom label (optional)
///   includeContainer  bool, wraps payload with an RTXC header (default: true)
///   allowExpansion    bool, allows auto mode to select a larger payload (default: false)
///
/// Output keys:
///   compressedData, containerData, algorithm, originalSize, compressedSize,
///   containerSize, savedBytes, compressionRatio, candidates, preview
/// </summary>
public class CompressionEngineTool : ITool
{
    private const string AlgorithmAuto = "auto";
    private const string AlgorithmNone = "none";
    private const string AlgorithmRle = "rle";
    private const string AlgorithmLz77 = "lz77";
    private const string AlgorithmHuffman = "huffman";

    private static readonly IReadOnlyDictionary<string, byte> AlgorithmIds =
        new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            [AlgorithmNone] = 0,
            [AlgorithmRle] = 1,
            [AlgorithmLz77] = 2,
            [AlgorithmHuffman] = 3
        };

    public string ToolId => "retruxel.tool.compressionengine";
    public string DisplayName => "Compression Engine";
    public string Description => "Compress assets using retro-appropriate algorithms (RLE, LZ77, Huffman)";
    public object? Icon => null;
    public string Category => "Optimization";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => true;
    public string? TargetExtensionId => "compression_engine";

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["algorithm"] = AlgorithmAuto,
        ["assetType"] = "raw",
        ["includeContainer"] = true,
        ["allowExpansion"] = false
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        if (!input.TryGetValue("data", out var dataObject))
            throw new ArgumentException("data is required. Supported values: byte[], string, int[] or JSON numeric array.");

        var original = GetBytes(dataObject);
        var requestedAlgorithm = GetString(input, "algorithm", AlgorithmAuto).ToLowerInvariant();
        var assetType = GetString(input, "assetType", "raw");
        var includeContainer = GetBool(input, "includeContainer", true);
        var allowExpansion = GetBool(input, "allowExpansion", false);

        if (!AlgorithmIds.ContainsKey(requestedAlgorithm) && requestedAlgorithm != AlgorithmAuto)
            throw new ArgumentException($"Unsupported compression algorithm '{requestedAlgorithm}'. Use auto, none, rle, lz77 or huffman.");

        var candidates = BuildCandidates(original, requestedAlgorithm, allowExpansion);
        var selected = SelectCandidate(candidates, original.Length, allowExpansion);
        var containerData = includeContainer
            ? BuildContainer(selected.Algorithm, original.Length, selected.Data)
            : selected.Data;

        var compressedSize = selected.Data.Length;
        var containerSize = containerData.Length;
        var savedBytes = original.Length - compressedSize;

        return new Dictionary<string, object>
        {
            ["algorithm"] = selected.Algorithm,
            ["requestedAlgorithm"] = requestedAlgorithm,
            ["assetType"] = assetType,
            ["originalSize"] = original.Length,
            ["compressedSize"] = compressedSize,
            ["containerSize"] = containerSize,
            ["savedBytes"] = savedBytes,
            ["containerSavedBytes"] = original.Length - containerSize,
            ["compressionRatio"] = original.Length == 0 ? 1.0 : (double)compressedSize / original.Length,
            ["containerCompressionRatio"] = original.Length == 0 ? 1.0 : (double)containerSize / original.Length,
            ["compressedData"] = selected.Data,
            ["containerData"] = containerData,
            ["isCompressed"] = selected.Algorithm != AlgorithmNone,
            ["candidates"] = candidates.Select(c => c.ToDictionary(original.Length)).ToArray(),
            ["preview"] = BuildPreview(original.Length, compressedSize, containerSize, selected.Algorithm),
            ["format"] = includeContainer ? "RTXC v1" : selected.Algorithm
        };
    }

    private static List<CompressionCandidate> BuildCandidates(byte[] original, string requestedAlgorithm, bool allowExpansion)
    {
        var candidates = new List<CompressionCandidate>
        {
            new(AlgorithmNone, original.ToArray())
        };

        var algorithms = requestedAlgorithm == AlgorithmAuto
            ? GetAutoAlgorithms(original)
            : new[] { requestedAlgorithm };

        foreach (var algorithm in algorithms)
        {
            if (algorithm == AlgorithmNone)
                continue;

            var compressed = algorithm switch
            {
                AlgorithmRle => CompressRle(original),
                AlgorithmLz77 => CompressLz77(original),
                AlgorithmHuffman => CompressHuffman(original),
                _ => throw new ArgumentException($"Unsupported compression algorithm '{algorithm}'.")
            };

            if (allowExpansion || compressed.Length <= original.Length || requestedAlgorithm != AlgorithmAuto)
                candidates.Add(new CompressionCandidate(algorithm, compressed));
            else
                candidates.Add(new CompressionCandidate(algorithm, compressed, Rejected: true));
        }

        return candidates;
    }

    private static string[] GetAutoAlgorithms(byte[] data)
    {
        if (data.Length == 0)
            return [AlgorithmRle, AlgorithmLz77, AlgorithmHuffman];

        var distinct = data.Distinct().Count();
        var hasLongRuns = HasRunOfAtLeast(data, 4);

        if (hasLongRuns && distinct <= 32)
            return [AlgorithmRle, AlgorithmLz77, AlgorithmHuffman];

        if (distinct <= 64)
            return [AlgorithmLz77, AlgorithmHuffman, AlgorithmRle];

        return [AlgorithmLz77, AlgorithmRle, AlgorithmHuffman];
    }

    private static CompressionCandidate SelectCandidate(List<CompressionCandidate> candidates, int originalSize, bool allowExpansion)
    {
        var selectable = candidates.Where(c => !c.Rejected).ToList();
        if (!allowExpansion)
            selectable = selectable.Where(c => c.Algorithm == AlgorithmNone || c.Data.Length <= originalSize).ToList();

        return selectable
            .OrderBy(c => c.Data.Length)
            .ThenBy(c => c.Algorithm == AlgorithmNone ? 1 : 0)
            .First();
    }

    // PackBits-style RLE: control 0..127 = literal length - 1, control 128..255 = run length - 3.
    private static byte[] CompressRle(byte[] data)
    {
        using var output = new MemoryStream();
        var literal = new List<byte>(128);
        var i = 0;

        void FlushLiteral()
        {
            if (literal.Count == 0)
                return;

            output.WriteByte((byte)(literal.Count - 1));
            output.Write(literal.ToArray(), 0, literal.Count);
            literal.Clear();
        }

        while (i < data.Length)
        {
            var runLength = CountRun(data, i, 130);
            if (runLength >= 3)
            {
                FlushLiteral();
                output.WriteByte((byte)(0x80 | (runLength - 3)));
                output.WriteByte(data[i]);
                i += runLength;
                continue;
            }

            literal.Add(data[i]);
            i++;

            if (literal.Count == 128)
                FlushLiteral();
        }

        FlushLiteral();
        return output.ToArray();
    }

    // LZSS variant: one flag byte per 8 tokens, 1 = match, 0 = literal.
    // Match token: 12-bit distance (1..4096) + 4-bit length code (3..18).
    private static byte[] CompressLz77(byte[] data)
    {
        using var output = new MemoryStream();
        var i = 0;

        while (i < data.Length)
        {
            var flagPosition = output.Position;
            output.WriteByte(0);
            byte flags = 0;

            for (var bit = 0; bit < 8 && i < data.Length; bit++)
            {
                var match = FindBestMatch(data, i);
                if (match.Length >= 3)
                {
                    flags |= (byte)(1 << bit);
                    var encoded = ((match.Length - 3) << 12) | (match.Distance - 1);
                    output.WriteByte((byte)(encoded >> 8));
                    output.WriteByte((byte)(encoded & 0xFF));
                    i += match.Length;
                }
                else
                {
                    output.WriteByte(data[i]);
                    i++;
                }
            }

            var current = output.Position;
            output.Position = flagPosition;
            output.WriteByte(flags);
            output.Position = current;
        }

        return output.ToArray();
    }

    private static byte[] CompressHuffman(byte[] data)
    {
        if (data.Length == 0)
            return [];

        var frequencies = new int[256];
        foreach (var b in data)
            frequencies[b]++;

        var root = BuildHuffmanTree(frequencies);
        var codes = new Dictionary<byte, HuffmanCode>();
        BuildHuffmanCodes(root, string.Empty, codes);

        using var output = new MemoryStream();
        var usedSymbols = frequencies
            .Select((frequency, symbol) => new { Symbol = symbol, Frequency = frequency })
            .Where(x => x.Frequency > 0)
            .ToArray();

        output.WriteByte((byte)(usedSymbols.Length - 1));
        foreach (var item in usedSymbols)
        {
            output.WriteByte((byte)item.Symbol);
            WriteUInt32(output, (uint)item.Frequency);
        }

        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var b in data)
        {
            var code = codes[b];
            foreach (var bit in code.Bits)
            {
                bitBuffer = (bitBuffer << 1) | (bit == '1' ? 1 : 0);
                bitCount++;
                if (bitCount == 8)
                {
                    output.WriteByte((byte)bitBuffer);
                    bitBuffer = 0;
                    bitCount = 0;
                }
            }
        }

        if (bitCount > 0)
            output.WriteByte((byte)(bitBuffer << (8 - bitCount)));

        return output.ToArray();
    }

    private static byte[] BuildContainer(string algorithm, int originalSize, byte[] payload)
    {
        using var output = new MemoryStream();
        output.WriteByte((byte)'R');
        output.WriteByte((byte)'T');
        output.WriteByte((byte)'X');
        output.WriteByte((byte)'C');
        output.WriteByte(1);
        output.WriteByte(AlgorithmIds[algorithm]);
        WriteUInt32(output, (uint)originalSize);
        WriteUInt32(output, (uint)payload.Length);
        output.Write(payload, 0, payload.Length);
        return output.ToArray();
    }

    private static string BuildPreview(int originalSize, int compressedSize, int containerSize, string algorithm)
    {
        var saved = originalSize - compressedSize;
        var percent = originalSize == 0 ? 0 : saved * 100.0 / originalSize;
        return $"{algorithm.ToUpperInvariant()}: {originalSize} -> {compressedSize} bytes ({saved:+#;-#;0} bytes, {percent:0.##}% saved). ROM container: {containerSize} bytes.";
    }

    private static byte[] GetBytes(object value)
    {
        switch (value)
        {
            case byte[] bytes:
                return bytes;
            case string text:
                return Encoding.UTF8.GetBytes(text);
            case JsonElement json when json.ValueKind == JsonValueKind.String:
                return Encoding.UTF8.GetBytes(json.GetString() ?? string.Empty);
            case JsonElement json when json.ValueKind == JsonValueKind.Array:
                return json.EnumerateArray().Select(ReadByte).ToArray();
            case IEnumerable<byte> bytes:
                return bytes.ToArray();
            case IEnumerable<int> ints:
                return ints.Select(i => ToByte(i)).ToArray();
            case IEnumerable enumerable:
                return enumerable.Cast<object>().Select(ToByte).ToArray();
            default:
                throw new ArgumentException($"Unsupported data type '{value.GetType().Name}'.");
        }
    }

    private static byte ReadByte(JsonElement element)
    {
        if (!element.TryGetInt32(out var value))
            throw new ArgumentException("JSON data arrays must contain numeric byte values.");

        return ToByte(value);
    }

    private static byte ToByte(object value)
    {
        var number = Convert.ToInt32(value);
        if (number < byte.MinValue || number > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"Byte value {number} is outside 0..255.");

        return (byte)number;
    }

    private static string GetString(Dictionary<string, object> input, string key, string defaultValue)
    {
        if (!input.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return value is JsonElement json ? json.GetString() ?? defaultValue : Convert.ToString(value) ?? defaultValue;
    }

    private static bool GetBool(Dictionary<string, object> input, string key, bool defaultValue)
    {
        if (!input.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return value is JsonElement json
            ? json.ValueKind == JsonValueKind.True || (json.ValueKind == JsonValueKind.String && bool.Parse(json.GetString()!))
            : Convert.ToBoolean(value);
    }

    private static int CountRun(byte[] data, int start, int maxLength)
    {
        var value = data[start];
        var length = 1;
        while (start + length < data.Length && length < maxLength && data[start + length] == value)
            length++;

        return length;
    }

    private static bool HasRunOfAtLeast(byte[] data, int minimum)
    {
        if (data.Length == 0)
            return false;

        var run = 1;
        for (var i = 1; i < data.Length; i++)
        {
            run = data[i] == data[i - 1] ? run + 1 : 1;
            if (run >= minimum)
                return true;
        }

        return false;
    }

    private static (int Distance, int Length) FindBestMatch(byte[] data, int position)
    {
        const int windowSize = 4096;
        const int maxLength = 18;

        var bestDistance = 0;
        var bestLength = 0;
        var searchStart = Math.Max(0, position - windowSize);

        for (var candidate = searchStart; candidate < position; candidate++)
        {
            var length = 0;
            while (length < maxLength &&
                   position + length < data.Length &&
                   data[candidate + length] == data[position + length])
            {
                length++;
            }

            if (length > bestLength)
            {
                bestLength = length;
                bestDistance = position - candidate;
                if (bestLength == maxLength)
                    break;
            }
        }

        return (bestDistance, bestLength);
    }

    private static HuffmanNode BuildHuffmanTree(int[] frequencies)
    {
        var nodes = frequencies
            .Select((frequency, symbol) => new HuffmanNode((byte)symbol, frequency))
            .Where(n => n.Frequency > 0)
            .ToList();

        if (nodes.Count == 1)
            return new HuffmanNode(null, nodes[0].Frequency, nodes[0], null);

        while (nodes.Count > 1)
        {
            nodes.Sort((a, b) =>
            {
                var frequencyCompare = a.Frequency.CompareTo(b.Frequency);
                return frequencyCompare != 0 ? frequencyCompare : a.MinSymbol.CompareTo(b.MinSymbol);
            });

            var left = nodes[0];
            var right = nodes[1];
            nodes.RemoveRange(0, 2);
            nodes.Add(new HuffmanNode(null, left.Frequency + right.Frequency, left, right));
        }

        return nodes[0];
    }

    private static void BuildHuffmanCodes(HuffmanNode node, string bits, Dictionary<byte, HuffmanCode> codes)
    {
        if (node.Symbol is byte symbol)
        {
            codes[symbol] = new HuffmanCode(bits.Length == 0 ? "0" : bits);
            return;
        }

        BuildHuffmanCodes(node.Left!, bits + "0", codes);
        if (node.Right is not null)
            BuildHuffmanCodes(node.Right, bits + "1", codes);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private sealed record CompressionCandidate(string Algorithm, byte[] Data, bool Rejected = false)
    {
        public Dictionary<string, object> ToDictionary(int originalSize)
        {
            var savedBytes = originalSize - Data.Length;
            return new Dictionary<string, object>
            {
                ["algorithm"] = Algorithm,
                ["size"] = Data.Length,
                ["savedBytes"] = savedBytes,
                ["compressionRatio"] = originalSize == 0 ? 1.0 : (double)Data.Length / originalSize,
                ["rejected"] = Rejected
            };
        }
    }

    private sealed record HuffmanCode(string Bits);

    private sealed class HuffmanNode
    {
        public HuffmanNode(byte? symbol, int frequency, HuffmanNode? left = null, HuffmanNode? right = null)
        {
            Symbol = symbol;
            Frequency = frequency;
            Left = left;
            Right = right;
            MinSymbol = symbol ?? Math.Min(left?.MinSymbol ?? byte.MaxValue, right?.MinSymbol ?? byte.MaxValue);
        }

        public byte? Symbol { get; }
        public int Frequency { get; }
        public HuffmanNode? Left { get; }
        public HuffmanNode? Right { get; }
        public byte MinSymbol { get; }
    }
}
