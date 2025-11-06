using System;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.AudioIO;

/// <summary>
/// Integrates the MixerEngine with an audio device for real-time playback.
/// Manages the audio callback and routes mixer output to hardware.
/// </summary>
public class AudioPlaybackEngine : IDisposable
{
    private readonly IAudioDevice _device;
    private readonly IMixerEngine _mixer;
    private readonly IAudioEngine _audioEngine;
    private bool _disposed;
    private bool _isRunning;

    /// <summary>
    /// Creates a new audio playback engine
    /// </summary>
    /// <param name="device">Audio output device</param>
    /// <param name="mixer">Mixer engine for track mixing</param>
    /// <param name="audioEngine">Audio engine for device management</param>
    public AudioPlaybackEngine(IAudioDevice device, IMixerEngine mixer, IAudioEngine audioEngine)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
    }

    /// <summary>
    /// Gets whether the playback engine is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the mixer engine
    /// </summary>
    public IMixerEngine Mixer => _mixer;

    /// <summary>
    /// Gets the transport system
    /// </summary>
    public ITransport Transport => _mixer.Transport;

    /// <summary>
    /// Starts audio playback
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _audioEngine.Start(AudioCallback);
        _isRunning = true;
    }

    /// <summary>
    /// Stops audio playback
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _audioEngine.Stop();
        _isRunning = false;
    }

    /// <summary>
    /// Audio callback invoked by the audio device for each buffer
    /// </summary>
    private bool AudioCallback(float[]? inputBuffer, float[] outputBuffer, int frameCount, int channels)
    {
        try
        {
            // Validate parameters
            if (outputBuffer == null || frameCount <= 0)
                return false;

            // Ensure buffer is large enough
            int requiredSamples = frameCount * channels;
            if (outputBuffer.Length < requiredSamples)
                return false;

            // Ensure mixer channel count matches device
            if (_mixer.ChannelCount != channels)
            {
                // Channel count mismatch - fill with silence and continue
                Array.Clear(outputBuffer, 0, requiredSamples);
                return true;
            }

            // Process audio through mixer
            _mixer.ProcessBuffer(outputBuffer.AsSpan(), frameCount);

            // Advance transport if playing
            if (_mixer.Transport.State == TransportState.Playing)
            {
                _mixer.Transport.Advance(frameCount);
            }

            return true; // Continue playback
        }
        catch
        {
            // In case of any error, output silence and continue
            // We must not throw exceptions in the audio callback
            if (outputBuffer != null)
            {
                Array.Clear(outputBuffer, 0, outputBuffer.Length);
            }
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
