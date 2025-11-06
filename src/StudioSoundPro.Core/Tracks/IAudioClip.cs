namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Represents an audio clip containing audio sample data
/// </summary>
public interface IAudioClip : IClip
{
    /// <summary>Gets the number of audio channels in this clip</summary>
    int Channels { get; }
    
    /// <summary>Gets the sample rate of the audio data</summary>
    int SampleRate { get; }
    
    /// <summary>Gets or sets the audio data buffer</summary>
    float[]? AudioData { get; set; }
    
    /// <summary>Reads audio samples from the clip at the specified position</summary>
    /// <param name="buffer">Buffer to fill with audio samples</param>
    /// <param name="offset">Offset into the buffer to start writing</param>
    /// <param name="count">Number of samples to read</param>
    /// <param name="position">Position in the clip to start reading from (in samples)</param>
    /// <returns>Number of samples actually read</returns>
    int ReadSamples(float[] buffer, int offset, int count, long position);
    
    /// <summary>Gets the peak amplitude value at the specified position for visualization</summary>
    /// <param name="position">Position in samples</param>
    /// <param name="windowSize">Size of the window to calculate peak over</param>
    /// <returns>Peak amplitude value (0.0 to 1.0)</returns>
    float GetPeakAmplitude(long position, int windowSize);
}
