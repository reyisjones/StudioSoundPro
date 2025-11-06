using System;
using System.Collections.Generic;
using System.Threading;
using StudioSoundPro.Core.Tracks;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Implementation of the mixer engine that combines multiple tracks into stereo output.
/// Uses lock-free techniques where possible for real-time audio thread safety.
/// </summary>
public class MixerEngine : IMixerEngine
{
    private readonly ITransport _transport;
    private readonly int _sampleRate;
    private readonly int _channelCount;
    
    // Track management with simple locking (brief, non-blocking for audio thread)
    private readonly object _trackLock = new();
    private List<ITrack> _tracks = new();
    private ITrack[] _trackSnapshot = Array.Empty<ITrack>();
    
    // Master controls - use volatile for simple atomic reads/writes
    private volatile float _masterVolume = 1.0f;
    private volatile bool _isMasterMuted = false;
    
    // Temporary buffers for mixing (allocated once, reused)
    private float[] _trackBuffer;
    private float[] _mixBuffer;
    
    private bool _disposed;

    public MixerEngine(ITransport transport, int sampleRate, int channelCount = 2)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (channelCount <= 0 || channelCount > 8)
            throw new ArgumentOutOfRangeException(nameof(channelCount), "Channel count must be between 1 and 8");
        
        _sampleRate = sampleRate;
        _channelCount = channelCount;
        
        // Pre-allocate buffers for common buffer sizes (up to 2048 frames)
        const int maxFrames = 2048;
        _trackBuffer = new float[maxFrames * channelCount];
        _mixBuffer = new float[maxFrames * channelCount];
    }

    public int SampleRate => _sampleRate;
    public int ChannelCount => _channelCount;
    public ITransport Transport => _transport;

    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Math.Clamp(value, 0.0f, 10.0f); // Limit to reasonable range
    }

    public bool IsMasterMuted
    {
        get => _isMasterMuted;
        set => _isMasterMuted = value;
    }

    public void AddTrack(ITrack track)
    {
        if (track == null)
            throw new ArgumentNullException(nameof(track));
        
        lock (_trackLock)
        {
            if (!_tracks.Contains(track))
            {
                _tracks.Add(track);
                UpdateSnapshot();
            }
        }
    }

    public bool RemoveTrack(ITrack track)
    {
        if (track == null)
            return false;
        
        lock (_trackLock)
        {
            bool removed = _tracks.Remove(track);
            if (removed)
            {
                UpdateSnapshot();
            }
            return removed;
        }
    }

    public void ClearTracks()
    {
        lock (_trackLock)
        {
            _tracks.Clear();
            UpdateSnapshot();
        }
    }

    public ITrack[] GetTracks()
    {
        // Return the snapshot (no locking needed for read)
        var snapshot = _trackSnapshot;
        var result = new ITrack[snapshot.Length];
        Array.Copy(snapshot, result, snapshot.Length);
        return result;
    }

    private void UpdateSnapshot()
    {
        // Must be called while holding _trackLock
        _trackSnapshot = _tracks.ToArray();
    }

    public void ProcessBuffer(Span<float> outputBuffer, int frameCount)
    {
        if (frameCount <= 0)
            return;
        
        int sampleCount = frameCount * _channelCount;
        
        if (outputBuffer.Length < sampleCount)
            throw new ArgumentException($"Output buffer too small. Need {sampleCount}, got {outputBuffer.Length}");
        
        // Ensure our temporary buffers are large enough
        if (_mixBuffer.Length < sampleCount)
        {
            // Rare case: need to reallocate (only happens if buffer size increased)
            _mixBuffer = new float[sampleCount * 2]; // Allocate extra to reduce future reallocations
            _trackBuffer = new float[sampleCount * 2];
        }
        
        // Get the output span we'll work with
        var output = outputBuffer.Slice(0, sampleCount);
        var mix = _mixBuffer.AsSpan(0, sampleCount);
        var trackBuf = _trackBuffer.AsSpan(0, sampleCount);
        
        // Clear mix buffer
        mix.Clear();
        
        // Check if we should produce audio
        if (_isMasterMuted || _transport.State != TransportState.Playing)
        {
            // Muted or not playing: output silence
            output.Clear();
            return;
        }
        
        // Get current playback position
        long currentPosition = _transport.Position;
        
        // Get snapshot of tracks (no lock needed - atomic reference read)
        var tracks = _trackSnapshot;
        
        if (tracks.Length == 0)
        {
            // No tracks: output silence
            output.Clear();
            return;
        }
        
        // Determine if any tracks are soloed
        bool hasSolo = false;
        foreach (var track in tracks)
        {
            if (track.IsSolo)
            {
                hasSolo = true;
                break;
            }
        }
        
        // Mix all active tracks
        foreach (var track in tracks)
        {
            // Skip muted tracks
            if (track.IsMuted)
                continue;
            
            // If solo mode is active, skip non-solo tracks
            if (hasSolo && !track.IsSolo)
                continue;
            
            // Clear track buffer
            trackBuf.Clear();
            
            // Render track audio using ProcessAudio method
            // Note: ProcessAudio expects float[] not Span<float>, so we use the array directly
            track.ProcessAudio(_trackBuffer, 0, sampleCount, currentPosition);
            
            // Apply track volume and pan, then mix into output
            ApplyVolumeAndPan(trackBuf, mix, frameCount, track.Volume, track.Pan);
        }
        
        // Apply master volume and copy to output
        float masterVol = _masterVolume;
        for (int i = 0; i < sampleCount; i++)
        {
            output[i] = mix[i] * masterVol;
        }
    }

    /// <summary>
    /// Applies volume and pan to track buffer and adds it to the mix buffer.
    /// For stereo: pan -1.0 = full left, 0.0 = center, 1.0 = full right
    /// </summary>
    private void ApplyVolumeAndPan(Span<float> trackBuffer, Span<float> mixBuffer, int frameCount, float volume, float pan)
    {
        if (_channelCount == 1)
        {
            // Mono: just apply volume
            for (int i = 0; i < frameCount; i++)
            {
                mixBuffer[i] += trackBuffer[i] * volume;
            }
        }
        else if (_channelCount == 2)
        {
            // Stereo: apply volume and pan
            // Pan law: constant power (equal loudness)
            float panAngle = (pan + 1.0f) * 0.25f * MathF.PI; // Map -1..1 to 0..PI/2
            float leftGain = MathF.Cos(panAngle) * volume;
            float rightGain = MathF.Sin(panAngle) * volume;
            
            for (int i = 0; i < frameCount; i++)
            {
                int idx = i * 2;
                float leftSample = trackBuffer[idx];
                float rightSample = trackBuffer[idx + 1];
                
                mixBuffer[idx] += leftSample * leftGain;
                mixBuffer[idx + 1] += rightSample * rightGain;
            }
        }
        else
        {
            // Multi-channel: just apply volume (no panning)
            int sampleCount = frameCount * _channelCount;
            for (int i = 0; i < sampleCount; i++)
            {
                mixBuffer[i] += trackBuffer[i] * volume;
            }
        }
    }

    public void Reset()
    {
        // Reset any internal state (currently none beyond tracks and transport)
        // Transport position is managed by Transport itself
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        lock (_trackLock)
        {
            _tracks.Clear();
            UpdateSnapshot();
        }
        
        GC.SuppressFinalize(this);
    }
}
