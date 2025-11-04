namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Represents an audio device (input or output).
/// </summary>
public interface IAudioDevice
{
    /// <summary>
    /// Gets the unique identifier for this device.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the display name of the device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the maximum number of input channels supported by this device.
    /// </summary>
    int MaxInputChannels { get; }

    /// <summary>
    /// Gets the maximum number of output channels supported by this device.
    /// </summary>
    int MaxOutputChannels { get; }

    /// <summary>
    /// Gets the default sample rate for this device.
    /// </summary>
    double DefaultSampleRate { get; }

    /// <summary>
    /// Gets a value indicating whether this is the default input device.
    /// </summary>
    bool IsDefaultInput { get; }

    /// <summary>
    /// Gets a value indicating whether this is the default output device.
    /// </summary>
    bool IsDefaultOutput { get; }
}