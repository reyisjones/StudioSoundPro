using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Pure C# WAV file writer supporting PCM and IEEE float formats
/// </summary>
public class WavFileWriter : IAudioFileWriter
{
    private bool _disposed;

    public void Write(string filePath, float[] samples, AudioFileWriteOptions options)
    {
        using var stream = File.Create(filePath);
        Write(stream, samples, options);
    }

    public void Write(Stream stream, float[] samples, AudioFileWriteOptions options)
    {
        ValidateOptions(options);
        
        if (samples == null || samples.Length == 0)
            throw new ArgumentException("Sample data cannot be null or empty", nameof(samples));

        if (samples.Length % options.Channels != 0)
            throw new ArgumentException($"Sample count ({samples.Length}) must be divisible by channel count ({options.Channels})", nameof(samples));

        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        int bytesPerSample = options.BitsPerSample / 8;
        int dataSize = samples.Length * bytesPerSample;
        int fileSize = 36 + dataSize; // RIFF header (12) + fmt chunk (24) + data chunk header (8) + data

        // Write RIFF header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(fileSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        // Write fmt chunk
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // fmt chunk size (standard PCM)
        
        short audioFormat = (short)(options.UseFloatFormat && options.BitsPerSample == 32 ? 3 : 1); // 1 = PCM, 3 = IEEE float
        writer.Write(audioFormat);
        writer.Write((short)options.Channels);
        writer.Write(options.SampleRate);
        
        int byteRate = options.SampleRate * options.Channels * bytesPerSample;
        writer.Write(byteRate);
        
        short blockAlign = (short)(options.Channels * bytesPerSample);
        writer.Write(blockAlign);
        writer.Write((short)options.BitsPerSample);

        // Write data chunk
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        WriteSamples(writer, samples, options);
    }

    public async Task WriteAsync(string filePath, float[] samples, AudioFileWriteOptions options)
    {
        await Task.Run(() => Write(filePath, samples, options));
    }

    private void ValidateOptions(AudioFileWriteOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.SampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive", nameof(options));

        if (options.Channels <= 0 || options.Channels > 8)
            throw new ArgumentException("Channels must be between 1 and 8", nameof(options));

        if (options.BitsPerSample != 16 && options.BitsPerSample != 24 && options.BitsPerSample != 32)
            throw new ArgumentException("Bit depth must be 16, 24, or 32", nameof(options));
    }

    private void WriteSamples(BinaryWriter writer, float[] samples, AudioFileWriteOptions options)
    {
        switch (options.BitsPerSample)
        {
            case 16:
                // 16-bit PCM
                foreach (var sample in samples)
                {
                    float clamped = Math.Clamp(sample, -1.0f, 1.0f);
                    short intSample = (short)(clamped * 32767.0f);
                    writer.Write(intSample);
                }
                break;

            case 24:
                // 24-bit PCM
                foreach (var sample in samples)
                {
                    float clamped = Math.Clamp(sample, -1.0f, 1.0f);
                    int intSample = (int)(clamped * 8388607.0f);
                    
                    // Write as 3 bytes (little-endian)
                    writer.Write((byte)(intSample & 0xFF));
                    writer.Write((byte)((intSample >> 8) & 0xFF));
                    writer.Write((byte)((intSample >> 16) & 0xFF));
                }
                break;

            case 32:
                if (options.UseFloatFormat)
                {
                    // 32-bit IEEE float
                    foreach (var sample in samples)
                    {
                        writer.Write(sample);
                    }
                }
                else
                {
                    // 32-bit PCM
                    foreach (var sample in samples)
                    {
                        float clamped = Math.Clamp(sample, -1.0f, 1.0f);
                        int intSample = (int)(clamped * 2147483647.0f);
                        writer.Write(intSample);
                    }
                }
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
