namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Provides metronome functionality with configurable sounds and timing
/// </summary>
public interface IMetronome
{
    /// <summary>Gets whether the metronome is enabled</summary>
    bool IsEnabled { get; set; }
    
    /// <summary>Gets whether to play metronome during playback</summary>
    bool PlayDuringPlayback { get; set; }
    
    /// <summary>Gets whether to play metronome during recording</summary>
    bool PlayDuringRecording { get; set; }
    
    /// <summary>Gets the metronome volume (0.0 to 1.0)</summary>
    float Volume { get; set; }
    
    /// <summary>Gets whether to accent the first beat of each bar</summary>
    bool AccentFirstBeat { get; set; }
    
    /// <summary>Gets the frequency for the downbeat click in Hz</summary>
    float DownbeatFrequency { get; set; }
    
    /// <summary>Gets the frequency for regular beat clicks in Hz</summary>
    float BeatFrequency { get; set; }
    
    /// <summary>Gets the click duration in milliseconds</summary>
    int ClickDurationMs { get; set; }
    
    /// <summary>Processes the metronome for the given audio buffer</summary>
    /// <param name="buffer">Audio buffer to write metronome clicks to</param>
    /// <param name="sampleCount">Number of samples to process</param>
    /// <param name="startPosition">Starting sample position in the timeline</param>
    /// <param name="clock">Clock for timing calculations</param>
    void ProcessBuffer(Span<float> buffer, int sampleCount, long startPosition, IClock clock);
    
    /// <summary>Event fired when metronome settings change</summary>
    event EventHandler<EventArgs> SettingsChanged;
}