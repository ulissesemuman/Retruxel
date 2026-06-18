using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Retruxel.Tool.AudioTracker.Models;

/// <summary>A single cell in the tracker grid.</summary>
public class NoteEvent
{
    [JsonPropertyName("note")]
    public string Note { get; set; } = "---";

    [JsonPropertyName("instrumentIndex")]
    public byte InstrumentIndex { get; set; } = 0xFF;

    [JsonPropertyName("volume")]
    public byte Volume { get; set; } = 0xFF;

    [JsonPropertyName("effectCommand")]
    public ushort EffectCommand { get; set; } = 0;

    [JsonIgnore]
    public bool IsEmpty => Note == "---" && InstrumentIndex == 0xFF && Volume == 0xFF && EffectCommand == 0;

    public NoteEvent Clone() => new() { Note = Note, InstrumentIndex = InstrumentIndex, Volume = Volume, EffectCommand = EffectCommand };
}

/// <summary>One channel column inside a pattern. Fixed 64 rows.</summary>
public class TrackPatternChannel
{
    public const int RowCount = 64;

    [JsonPropertyName("channelIndex")]
    public int ChannelIndex { get; set; }

    [JsonPropertyName("rows")]
    public NoteEvent[] Rows { get; set; } = InitRows();

    private static NoteEvent[] InitRows()
    {
        var rows = new NoteEvent[RowCount];
        for (int i = 0; i < RowCount; i++) rows[i] = new NoteEvent();
        return rows;
    }
}

/// <summary>One pattern block (64 rows x N channels).</summary>
public class AudioPattern
{
    [JsonPropertyName("patternId")]
    public int PatternId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("channels")]
    public List<TrackPatternChannel> Channels { get; set; } = [];
}

/// <summary>Abstract instrument — hardware-agnostic ADSR envelope.</summary>
public class InstrumentState
{
    [JsonPropertyName("index")]
    public byte Index { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Unnamed";

    /// <summary>"square", "pulse50", "triangle", "sawtooth", "noise"</summary>
    [JsonPropertyName("waveform")]
    public string Waveform { get; set; } = "square";

    [JsonPropertyName("attack")]
    public byte Attack { get; set; } = 0;

    [JsonPropertyName("decay")]
    public byte Decay { get; set; } = 8;

    [JsonPropertyName("sustain")]
    public byte Sustain { get; set; } = 10;

    [JsonPropertyName("release")]
    public byte Release { get; set; } = 4;
}

/// <summary>One row in the song arrangement.</summary>
public class ArrangementRow
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>channelIndex -> patternId</summary>
    [JsonPropertyName("channelPatterns")]
    public Dictionary<int, int> ChannelPatterns { get; set; } = [];
}

/// <summary>Root state of an audio project. Serialized to .rtrxaudio JSON.</summary>
public class AudioProjectState
{
    [JsonPropertyName("bpm")]
    public byte Bpm { get; set; } = 120;

    [JsonPropertyName("speed")]
    public byte Speed { get; set; } = 6;

    [JsonIgnore]
    public int PatternCount => Patterns.Count;

    [JsonPropertyName("patterns")]
    public List<AudioPattern> Patterns { get; set; } = [];

    [JsonPropertyName("instruments")]
    public List<InstrumentState> Instruments { get; set; } = [];

    [JsonPropertyName("arrangement")]
    public List<ArrangementRow> Arrangement { get; set; } = [];

    [JsonIgnore] public int CurrentPatternIndex { get; set; } = 0;
    [JsonIgnore] public int PlayheadRow         { get; set; } = 0;
}
