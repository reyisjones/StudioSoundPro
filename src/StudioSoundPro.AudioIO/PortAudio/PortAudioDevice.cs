using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.AudioIO.PortAudio;

/// <summary>
/// PortAudio implementation of IAudioDevice.
/// </summary>
public class PortAudioDevice : IAudioDevice
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MaxInputChannels { get; init; }
    public int MaxOutputChannels { get; init; }
    public double DefaultSampleRate { get; init; }
    public bool IsDefaultInput { get; init; }
    public bool IsDefaultOutput { get; init; }

    public override string ToString()
    {
        return $"{Name} (ID: {Id}, In: {MaxInputChannels}, Out: {MaxOutputChannels}, SR: {DefaultSampleRate}Hz)";
    }
}