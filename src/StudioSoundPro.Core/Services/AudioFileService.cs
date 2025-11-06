using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.Core.Services;

/// <summary>
/// Service for importing and exporting audio files
/// </summary>
public class AudioFileService : IDisposable
{
    private readonly IAudioFileReader _reader;
    private readonly IAudioFileWriter _writer;
    private bool _disposed;

    public AudioFileService()
        : this(new WavFileReader(), new WavFileWriter())
    {
    }

    public AudioFileService(IAudioFileReader reader, IAudioFileWriter writer)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Import an audio file and create an AudioClip
    /// </summary>
    public AudioClip ImportAudioFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Audio file not found: {filePath}", filePath);

        _reader.Open(filePath);
        var info = _reader.Info;
        var samples = _reader.ReadAllSamples();

        // Convert to stereo if needed
        float[] stereoSamples;
        if (info.Channels == 1)
        {
            stereoSamples = new float[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                stereoSamples[i * 2] = samples[i];
                stereoSamples[i * 2 + 1] = samples[i];
            }
        }
        else if (info.Channels == 2)
        {
            stereoSamples = samples;
        }
        else
        {
            // For multi-channel, downmix to stereo by averaging channels
            stereoSamples = DownmixToStereo(samples, info.Channels);
        }

        // Create clip with file name (without extension)
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var clip = new AudioClip(fileName, stereoSamples, 2, info.SampleRate);

        return clip;
    }

    /// <summary>
    /// Import an audio file asynchronously
    /// </summary>
    public async Task<AudioClip> ImportAudioFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Audio file not found: {filePath}", filePath);

        _reader.Open(filePath);
        var info = _reader.Info;
        var samples = await _reader.ReadAllSamplesAsync();

        var fileName = Path.GetFileNameWithoutExtension(filePath);

        float[] stereoSamples;
        if (info.Channels == 1)
        {
            stereoSamples = new float[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                stereoSamples[i * 2] = samples[i];
                stereoSamples[i * 2 + 1] = samples[i];
            }
        }
        else if (info.Channels == 2)
        {
            stereoSamples = samples;
        }
        else
        {
            stereoSamples = DownmixToStereo(samples, info.Channels);
        }

        var clip = new AudioClip(fileName, stereoSamples, 2, info.SampleRate);

        return clip;
    }

    /// <summary>
    /// Export a track to an audio file
    /// </summary>
    public void ExportTrack(ITrack track, string filePath, AudioFileWriteOptions? options = null)
    {
        if (track == null)
            throw new ArgumentNullException(nameof(track));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        options ??= new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 16
        };

        // Calculate total length needed
        var totalSamples = CalculateTrackLength(track);
        if (totalSamples == 0)
            throw new InvalidOperationException("Track is empty");

        // Render the track
        var buffer = new float[totalSamples * 2]; // Stereo
        RenderTrack(track, buffer);

        _writer.Write(filePath, buffer, options);
    }

    /// <summary>
    /// Export a track asynchronously
    /// </summary>
    public async Task ExportTrackAsync(ITrack track, string filePath, AudioFileWriteOptions? options = null)
    {
        if (track == null)
            throw new ArgumentNullException(nameof(track));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        options ??= new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 16
        };

        var totalSamples = CalculateTrackLength(track);
        if (totalSamples == 0)
            throw new InvalidOperationException("Track is empty");

        var buffer = new float[totalSamples * 2];
        RenderTrack(track, buffer);

        await _writer.WriteAsync(filePath, buffer, options);
    }

    private static float[] DownmixToStereo(float[] samples, int channels)
    {
        var frameCount = samples.Length / channels;
        var stereo = new float[frameCount * 2];

        for (int frame = 0; frame < frameCount; frame++)
        {
            float left = 0;
            float right = 0;

            // Average all channels into stereo
            for (int ch = 0; ch < channels; ch++)
            {
                var sample = samples[frame * channels + ch];
                if (ch % 2 == 0)
                    left += sample;
                else
                    right += sample;
            }

            var leftChannels = (channels + 1) / 2;
            var rightChannels = channels / 2;

            stereo[frame * 2] = left / leftChannels;
            stereo[frame * 2 + 1] = rightChannels > 0 ? right / rightChannels : left / leftChannels;
        }

        return stereo;
    }

    private static int CalculateTrackLength(ITrack track)
    {
        if (!track.Clips.Any())
            return 0;

        // Find the rightmost point of any clip
        var maxEndSample = track.Clips.Max(c => c.StartPosition + c.Length);
        return (int)maxEndSample;
    }

    private static void RenderTrack(ITrack track, Span<float> buffer)
    {
        buffer.Clear();

        foreach (var clip in track.Clips.OfType<IAudioClip>())
        {
            var clipBuffer = new float[clip.Length * 2];
            
            // Read samples from the clip starting from its timeline position
            clip.ReadSamples(clipBuffer, 0, (int)clip.Length * 2, clip.StartPosition);

            // Apply fade in/out and gain
            ApplyClipProcessing(clip, clipBuffer);

            // Mix clip into output buffer at correct position
            var startSample = clip.StartPosition;
            var endSample = Math.Min(startSample + clip.Length, buffer.Length / 2);

            for (long i = startSample; i < endSample; i++)
            {
                var clipIndex = (int)(i - startSample) * 2;
                var bufferIndex = (int)i * 2;

                buffer[bufferIndex] += clipBuffer[clipIndex];
                buffer[bufferIndex + 1] += clipBuffer[clipIndex + 1];
            }
        }
    }

    private static void ApplyClipProcessing(IAudioClip clip, Span<float> buffer)
    {
        var length = clip.Length;
        
        // Apply gain
        if (clip.Gain != 1.0f)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= clip.Gain;
            }
        }

        // Apply fade in
        if (clip.FadeInLength > 0)
        {
            var fadeLen = Math.Min(clip.FadeInLength, length);
            for (long i = 0; i < fadeLen; i++)
            {
                var factor = (float)i / fadeLen;
                buffer[(int)i * 2] *= factor;
                buffer[(int)i * 2 + 1] *= factor;
            }
        }

        // Apply fade out
        if (clip.FadeOutLength > 0)
        {
            var fadeLen = Math.Min(clip.FadeOutLength, length);
            var fadeStart = length - fadeLen;
            for (long i = 0; i < fadeLen; i++)
            {
                var factor = 1.0f - ((float)i / fadeLen);
                var sampleIdx = (int)(fadeStart + i);
                buffer[sampleIdx * 2] *= factor;
                buffer[sampleIdx * 2 + 1] *= factor;
            }
        }

        // Apply mute
        if (clip.IsMuted)
        {
            buffer.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reader?.Dispose();
        _writer?.Dispose();

        _disposed = true;
    }
}
