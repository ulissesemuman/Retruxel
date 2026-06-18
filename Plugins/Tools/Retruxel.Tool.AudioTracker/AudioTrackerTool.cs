using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Tool.AudioTracker.Domain;
using Retruxel.Tool.AudioTracker.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;

namespace Retruxel.Tool.AudioTracker;

/// <summary>
/// Audio Tracker tool entry point.
/// Implements ITool, resolves hardware constraints from the active TargetId,
/// and opens the AudioTrackerWindow.
/// </summary>
public class AudioTrackerTool : ITool
{
    public string  ToolId        => "retruxel.tool.audiotracker";
    public string  DisplayName   => "Audio Tracker";
    public string  Description   => "Compose music and sound effects using a hardware-accurate tracker.";
    public object? Icon          => null;
    public string  Category      => "Audio";
    public string? Shortcut      => null;
    public bool    IsStandalone  => true;
    public bool    RequiresProject => true;
    public string? TargetId      => null;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = true
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Extract project context
        var project  = input.TryGetValue("projectRef",  out var p) ? p as RetruxelProject  : null;
        var target   = input.TryGetValue("targetRef",   out var t) ? t as ITarget           : null;
        var targetId = project?.TargetId
                    ?? (target?.TargetId)
                    ?? GetString(input, "targetId", "sms");

        // Load or create audio project state
        AudioProjectState state;
        if (input.TryGetValue("audioState", out var stateObj) && stateObj is string stateJson)
            state = JsonSerializer.Deserialize<AudioProjectState>(stateJson, _jsonOptions) ?? CreateDefault(targetId);
        else
            state = CreateDefault(targetId);

        // Resolve hardware constraints
        var constraints = AudioTargetConstraints.ForTarget(targetId);

        // Open window
        var window = new AudioTrackerWindow(state, constraints, targetId)
        {
            Owner = Application.Current?.MainWindow
        };

        bool ok = window.ShowDialog() == true;

        var result = new Dictionary<string, object> { ["ok"] = ok };
        if (ok)
            result["audioState"] = JsonSerializer.Serialize(window.State, _jsonOptions);

        return result;
    }

    private static AudioProjectState CreateDefault(string targetId)
    {
        var constraints = AudioTargetConstraints.ForTarget(targetId);
        var state       = new AudioProjectState();

        // Default instruments
        for (int i = 0; i < constraints.ChannelCount; i++)
        {
            var ch = constraints.Channels[i];
            state.Instruments.Add(new InstrumentState
            {
                Index    = (byte)i,
                Name     = ch.Name,
                Waveform = ch.Type.ToString().ToLowerInvariant()
            });
        }

        // One empty pattern
        var pattern = new AudioPattern { PatternId = 0, Name = "Pattern 00" };
        for (int c = 0; c < constraints.ChannelCount; c++)
            pattern.Channels.Add(new TrackPatternChannel { ChannelIndex = c });
        state.Patterns.Add(pattern);

        // One arrangement row
        state.Arrangement.Add(new ArrangementRow { Label = "Intro" });

        return state;
    }

    private static string GetString(Dictionary<string, object> input, string key, string fallback)
        => input.TryGetValue(key, out var v) ? v?.ToString() ?? fallback : fallback;
}
