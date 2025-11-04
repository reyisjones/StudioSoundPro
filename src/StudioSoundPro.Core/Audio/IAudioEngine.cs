namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Callback delegate for audio processing.
/// </summary>
/// <param name="inputBuffer">Input audio buffer (may be null for output-only scenarios).</param>
/// <param name="outputBuffer">Output audio buffer to fill with audio data.</param>
/// <param name="frameCount">Number of frames to process.</param>
/// <param name="channels">Number of channels per frame.</param>
/// <returns>True to continue processing, false to stop.</returns>
public delegate bool AudioCallback(float[]? inputBuffer, float[] outputBuffer, int frameCount, int channels);

/// <summary>
/// Engine state enumeration.
/// </summary>
public enum AudioEngineState
{
    Stopped,
    Starting,
    Running,
    Stopping
}

/// <summary>
/// Core audio engine for real-time audio processing.
/// </summary>
public interface IAudioEngine : IDisposable
{
    /// <summary>
    /// Gets the current state of the audio engine.
    /// </summary>
    AudioEngineState State { get; }

    /// <summary>
    /// Gets the current sample rate.
    /// </summary>
    double SampleRate { get; }

    /// <summary>
    /// Gets the current buffer size in frames.
    /// </summary>
    int BufferSize { get; }

    /// <summary>
    /// Gets the number of output channels.
    /// </summary>
    int OutputChannels { get; }

    /// <summary>
    /// Event raised when the engine state changes.
    /// </summary>
    event EventHandler<AudioEngineState>? StateChanged;

    /// <summary>
    /// Event raised when an audio processing error occurs.
    /// </summary>
    event EventHandler<string>? ProcessingError;

    /// <summary>
    /// Initializes the audio engine with the specified device and parameters.
    /// </summary>
    /// <param name="outputDevice">The output device to use.</param>
    /// <param name="sampleRate">The desired sample rate (default: 48000).</param>
    /// <param name="bufferSize">The desired buffer size in frames (default: 512).</param>
    /// <param name="channels">The number of output channels (default: 2).</param>
    void Initialize(IAudioDevice outputDevice, double sampleRate = 48000, int bufferSize = 512, int channels = 2);

    /// <summary>
    /// Starts the audio engine with the specified callback.
    /// </summary>
    /// <param name="callback">The audio processing callback.</param>
    void Start(AudioCallback callback);

    /// <summary>
    /// Stops the audio engine.
    /// </summary>
    void Stop();
}