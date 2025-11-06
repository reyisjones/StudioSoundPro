using System;
using System.IO;
using System.Threading.Tasks;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Audio file write options
/// </summary>
public class AudioFileWriteOptions
{
    public int SampleRate { get; set; } = 48000;
    public int Channels { get; set; } = 2;
    public int BitsPerSample { get; set; } = 16; // 16, 24, or 32
    public bool UseFloatFormat { get; set; } = false; // Use IEEE float for 32-bit
}

/// <summary>
/// Interface for writing audio files
/// </summary>
public interface IAudioFileWriter : IDisposable
{
    /// <summary>
    /// Writes audio data to a file
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="samples">Interleaved audio samples</param>
    /// <param name="options">Write options (sample rate, channels, bit depth)</param>
    void Write(string filePath, float[] samples, AudioFileWriteOptions options);
    
    /// <summary>
    /// Writes audio data to a stream
    /// </summary>
    /// <param name="stream">Output stream</param>
    /// <param name="samples">Interleaved audio samples</param>
    /// <param name="options">Write options</param>
    void Write(Stream stream, float[] samples, AudioFileWriteOptions options);
    
    /// <summary>
    /// Writes audio data asynchronously
    /// </summary>
    Task WriteAsync(string filePath, float[] samples, AudioFileWriteOptions options);
}
