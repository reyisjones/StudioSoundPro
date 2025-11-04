namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Generator for sine wave audio signals.
/// </summary>
public interface ISineGenerator
{
    /// <summary>
    /// Gets or sets the frequency of the sine wave in Hz.
    /// </summary>
    double Frequency { get; set; }

    /// <summary>
    /// Gets or sets the amplitude (0.0 to 1.0).
    /// </summary>
    double Amplitude { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the generator is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes the generator with the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    void Initialize(double sampleRate);

    /// <summary>
    /// Generates audio samples into the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to fill with audio samples.</param>
    /// <param name="frameCount">The number of frames to generate.</param>
    /// <param name="channels">The number of channels per frame.</param>
    void GenerateAudio(float[] buffer, int frameCount, int channels);

    /// <summary>
    /// Resets the generator's internal state.
    /// </summary>
    void Reset();
}