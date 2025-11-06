using System;
using StudioSoundPro.Core.Tracks;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Represents the mixer engine that combines multiple tracks into a stereo output.
/// Designed to be called from the real-time audio callback thread.
/// </summary>
public interface IMixerEngine : IDisposable
{
    /// <summary>
    /// Gets the sample rate in Hz (e.g., 44100, 48000).
    /// </summary>
    int SampleRate { get; }
    
    /// <summary>
    /// Gets the number of output channels (typically 2 for stereo).
    /// </summary>
    int ChannelCount { get; }
    
    /// <summary>
    /// Gets or sets the master output volume (0.0 = silence, 1.0 = unity gain, >1.0 = amplification).
    /// Thread-safe: can be modified from UI thread.
    /// </summary>
    float MasterVolume { get; set; }
    
    /// <summary>
    /// Gets or sets whether the master output is muted.
    /// Thread-safe: can be modified from UI thread.
    /// </summary>
    bool IsMasterMuted { get; set; }
    
    /// <summary>
    /// Gets the transport system controlling playback position and state.
    /// </summary>
    ITransport Transport { get; }
    
    /// <summary>
    /// Adds a track to the mixer.
    /// Thread-safe: can be called from UI thread.
    /// </summary>
    /// <param name="track">The track to add.</param>
    void AddTrack(ITrack track);
    
    /// <summary>
    /// Removes a track from the mixer.
    /// Thread-safe: can be called from UI thread.
    /// </summary>
    /// <param name="track">The track to remove.</param>
    /// <returns>True if the track was found and removed, false otherwise.</returns>
    bool RemoveTrack(ITrack track);
    
    /// <summary>
    /// Removes all tracks from the mixer.
    /// Thread-safe: can be called from UI thread.
    /// </summary>
    void ClearTracks();
    
    /// <summary>
    /// Gets a read-only snapshot of current tracks.
    /// Returns a new array to avoid threading issues.
    /// </summary>
    /// <returns>Array of current tracks.</returns>
    ITrack[] GetTracks();
    
    /// <summary>
    /// Processes one buffer of audio output by mixing all active tracks.
    /// This method is called from the real-time audio thread and must be lock-free and non-blocking.
    /// </summary>
    /// <param name="outputBuffer">
    /// The output buffer to fill with interleaved samples.
    /// Length must be frameCount * ChannelCount.
    /// Format: [L0, R0, L1, R1, L2, R2, ...]
    /// </param>
    /// <param name="frameCount">Number of frames (samples per channel) to generate.</param>
    /// <remarks>
    /// CRITICAL: This method runs on the audio callback thread. It must:
    /// - Not allocate memory
    /// - Not acquire locks (use lock-free data structures or careful synchronization)
    /// - Complete within the buffer time budget
    /// - Handle all edge cases gracefully without exceptions
    /// 
    /// The mixer will:
    /// 1. Query Transport for current playback position and state
    /// 2. If transport is playing:
    ///    a. Mix audio from all non-muted tracks (respecting solo state)
    ///    b. Apply per-track volume and pan
    ///    c. Sum into output buffer
    /// 3. Apply master volume and mute state
    /// 4. Advance Transport position by frameCount samples if playing
    /// 5. Clear buffer to silence if transport is stopped or master is muted
    /// </remarks>
    void ProcessBuffer(Span<float> outputBuffer, int frameCount);
    
    /// <summary>
    /// Resets the mixer state. Does not affect transport or track positions.
    /// Thread-safe: can be called from UI thread.
    /// </summary>
    void Reset();
}
