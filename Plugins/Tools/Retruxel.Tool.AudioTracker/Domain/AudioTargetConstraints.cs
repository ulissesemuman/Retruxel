using System.Collections.Generic;

namespace Retruxel.Tool.AudioTracker.Domain;

/// <summary>
/// Describes the audio hardware capabilities of a target console.
/// Resolved at runtime from the project's TargetId.
/// </summary>
public class AudioTargetConstraints
{
    /// <summary>Total number of independent audio channels.</summary>
    public int ChannelCount { get; init; }

    /// <summary>Per-channel descriptor.</summary>
    public IReadOnlyList<ChannelDescriptor> Channels { get; init; } = [];

    /// <summary>Volume attenuation bits (4 = 0-15, 3 = 0-7).</summary>
    public int VolumeBits { get; init; } = 4;

    /// <summary>Chip display name shown in the status bar.</summary>
    public string ChipName { get; init; } = string.Empty;

    /// <summary>Resolves constraints from a Retruxel TargetId string.</summary>
    public static AudioTargetConstraints ForTarget(string targetId) => targetId.ToLowerInvariant() switch
    {
        "nes" => new AudioTargetConstraints
        {
            ChipName     = "RP2A03",
            VolumeBits   = 4,
            ChannelCount = 5,
            Channels     =
            [
                new ChannelDescriptor(0, "Pulse 1",   ChannelType.Pulse,    canPitch: true),
                new ChannelDescriptor(1, "Pulse 2",   ChannelType.Pulse,    canPitch: true),
                new ChannelDescriptor(2, "Triangle",  ChannelType.Triangle, canPitch: true,  fixedVolume: true),
                new ChannelDescriptor(3, "Noise",     ChannelType.Noise,    canPitch: false),
                new ChannelDescriptor(4, "DPCM",      ChannelType.Dpcm,     canPitch: false)
            ]
        },
        "sms" or "gg" => new AudioTargetConstraints
        {
            ChipName     = "SN76489",
            VolumeBits   = 4,
            ChannelCount = 4,
            Channels     =
            [
                new ChannelDescriptor(0, "Square 1", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(1, "Square 2", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(2, "Square 3", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(3, "Noise",    ChannelType.Noise,  canPitch: false)
            ]
        },
        "sg1000" or "coleco" => new AudioTargetConstraints
        {
            ChipName     = "SN76489",
            VolumeBits   = 4,
            ChannelCount = 4,
            Channels     =
            [
                new ChannelDescriptor(0, "Square 1", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(1, "Square 2", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(2, "Square 3", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(3, "Noise",    ChannelType.Noise,  canPitch: false)
            ]
        },
        _ => new AudioTargetConstraints   // generic fallback
        {
            ChipName     = "Generic PSG",
            VolumeBits   = 4,
            ChannelCount = 4,
            Channels     =
            [
                new ChannelDescriptor(0, "Ch 1", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(1, "Ch 2", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(2, "Ch 3", ChannelType.Square, canPitch: true),
                new ChannelDescriptor(3, "Noise", ChannelType.Noise, canPitch: false)
            ]
        }
    };
}

public record ChannelDescriptor(
    int    Index,
    string Name,
    ChannelType Type,
    bool   CanPitch,
    bool   FixedVolume = false);

public enum ChannelType { Square, Pulse, Triangle, Sawtooth, Noise, Dpcm }
