using System;
using System.IO;
using System.Threading.Tasks;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Metadata about an audio file
/// </summary>
public class AudioFileInfo
{
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    public long TotalSamples { get; set; } // Total samples per channel
    public TimeSpan Duration { get; set; }
    public string Format { get; set; } = "Unknown";
}

/// <summary>
/// Interface for reading audio files
/// </summary>
public interface IAudioFileReader : IDisposable
{
    /// <summary>
    /// Gets information about the audio file
    /// </summary>
    AudioFileInfo Info { get; }
    
    /// <summary>
    /// Reads all audio data as interleaved float samples
    /// </summary>
    /// <returns>Array of interleaved samples (e.g., [L, R, L, R, ...])</returns>
    float[] ReadAllSamples();
    
    /// <summary>
    /// Reads audio data asynchronously
    /// </summary>
    Task<float[]> ReadAllSamplesAsync();
    
    /// <summary>
    /// Opens an audio file for reading
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    void Open(string filePath);
    
    /// <summary>
    /// Opens an audio file from a stream
    /// </summary>
    /// <param name="stream">Stream containing audio data</param>
    void Open(Stream stream);
}
