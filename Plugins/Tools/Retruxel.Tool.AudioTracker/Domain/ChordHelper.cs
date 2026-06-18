using Retruxel.Tool.AudioTracker.Models;
using System;
using System.Collections.Generic;

namespace Retruxel.Tool.AudioTracker.Domain;

/// <summary>
/// Basic mode helper: resolves a chord symbol into component notes
/// and distributes them across the available pitch channels.
/// Works entirely with note name strings — no audio rendering.
/// </summary>
public static class ChordHelper
{
    // Chromatic note names (sharps)
    private static readonly string[] NoteNames =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    // Semitone intervals for common chord types (root = 0)
    private static readonly Dictionary<string, int[]> ChordIntervals =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [""]    = [0, 4, 7],          // Major
            ["m"]   = [0, 3, 7],          // Minor
            ["7"]   = [0, 4, 7, 10],      // Dominant 7th
            ["maj7"] = [0, 4, 7, 11],     // Major 7th
            ["m7"]  = [0, 3, 7, 10],      // Minor 7th
            ["dim"] = [0, 3, 6],           // Diminished
            ["aug"] = [0, 4, 8],           // Augmented
            ["sus4"] = [0, 5, 7],          // Suspended 4th
            ["sus2"] = [0, 2, 7],          // Suspended 2nd
        };

    /// <summary>
    /// Given a chord symbol (e.g. "C", "Am", "G7") and a target octave,
    /// returns a list of NoteEvents that can be placed in pitch channels.
    /// </summary>
    /// <param name="chordSymbol">Root + quality, e.g. "C", "Cm", "G7".</param>
    /// <param name="octave">Octave number (3 or 4 recommended for PSG).</param>
    /// <param name="constraints">Hardware constraints to limit channel count.</param>
    /// <param name="instrumentIndex">Instrument to assign to all generated notes.</param>
    /// <param name="volume">Volume 0-15 for all notes.</param>
    public static IReadOnlyList<NoteEvent> ResolveChord(
        string chordSymbol,
        int octave,
        AudioTargetConstraints constraints,
        byte instrumentIndex = 0,
        byte volume = 0xF)
    {
        var (root, quality) = ParseChord(chordSymbol);
        if (root < 0) return [];

        if (!ChordIntervals.TryGetValue(quality, out var intervals))
            intervals = ChordIntervals[""];   // fallback to major

        // Count how many pitch channels are available
        int pitchChannels = 0;
        foreach (var ch in constraints.Channels)
            if (ch.CanPitch) pitchChannels++;

        var events = new List<NoteEvent>();
        int limit  = Math.Min(intervals.Length, pitchChannels);

        for (int i = 0; i < limit; i++)
        {
            int semitone   = (root + intervals[i]) % 12;
            int noteOctave = octave + (root + intervals[i]) / 12;
            string noteName = $"{NoteNames[semitone]}-{noteOctave}";

            // Pad to standard tracker format: "C-4", "C#4", "A-3"
            if (!noteName.Contains('#'))
                noteName = noteName.Replace("-", "-");

            events.Add(new NoteEvent
            {
                Note            = FormatNote(NoteNames[semitone], noteOctave),
                InstrumentIndex = instrumentIndex,
                Volume          = volume,
                EffectCommand   = 0
            });
        }

        return events;
    }

    /// <summary>
    /// Formats a note name into the standard 3-char tracker format.
    /// E.g. "C", 4 -> "C-4" | "C#", 4 -> "C#4"
    /// </summary>
    public static string FormatNote(string noteName, int octave)
    {
        if (noteName.Length == 1) return $"{noteName}-{octave}";
        return $"{noteName}{octave}";   // sharps: "C#4"
    }

    /// <summary>Returns all available chord symbols for display in the UI.</summary>
    public static IReadOnlyList<string> GetChordSymbols(string root)
    {
        var result = new List<string>();
        foreach (var quality in ChordIntervals.Keys)
            result.Add(root + quality);
        return result;
    }

    /// <summary>Common root notes for the quick chord matrix.</summary>
    public static readonly string[] CommonChords =
        ["C", "Cm", "G", "G7", "F", "Am", "D", "Dm", "E", "Em", "A", "Bm"];

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static (int rootIndex, string quality) ParseChord(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return (-1, "");

        // Extract root (1 or 2 chars: "C", "C#", "Bb" treated as "A#")
        string root;
        string quality;

        if (symbol.Length > 1 && (symbol[1] == '#' || symbol[1] == 'b'))
        {
            root    = symbol[..2].Replace("Bb", "A#").Replace("Eb", "D#")
                                  .Replace("Ab", "G#").Replace("Db", "C#")
                                  .Replace("Gb", "F#");
            quality = symbol[2..];
        }
        else
        {
            root    = symbol[..1];
            quality = symbol[1..];
        }

        int idx = Array.IndexOf(NoteNames, root);
        return (idx, quality);
    }
}
