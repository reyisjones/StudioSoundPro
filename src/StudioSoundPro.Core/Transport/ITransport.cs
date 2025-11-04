namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Controls transport functionality for the DAW (play, pause, stop, record)
/// </summary>
public interface ITransport
{
    /// <summary>Gets the current transport state</summary>
    TransportState State { get; }
    
    /// <summary>Gets the current playback position in samples</summary>
    long Position { get; set; }
    
    /// <summary>Gets whether the transport is currently playing or recording</summary>
    bool IsRunning { get; }
    
    /// <summary>Gets whether looping is enabled</summary>
    bool IsLooping { get; set; }
    
    /// <summary>Gets the loop start position in samples</summary>
    long LoopStart { get; set; }
    
    /// <summary>Gets the loop end position in samples</summary>
    long LoopEnd { get; set; }
    
    /// <summary>Gets the clock instance for timing calculations</summary>
    IClock Clock { get; }
    
    /// <summary>Starts playback from current position</summary>
    Task PlayAsync();
    
    /// <summary>Pauses playback, maintaining current position</summary>
    Task PauseAsync();
    
    /// <summary>Stops playback and returns to stop position</summary>
    Task StopAsync();
    
    /// <summary>Starts recording from current position</summary>
    Task RecordAsync();
    
    /// <summary>Moves the playhead to the specified sample position</summary>
    Task SeekAsync(long samplePosition);
    
    /// <summary>Moves the playhead to the beginning</summary>
    Task RewindAsync();
    
    /// <summary>Advances the transport by the specified number of samples (for audio callback)</summary>
    void Advance(int sampleCount);
    
    /// <summary>Event fired when transport state changes</summary>
    event EventHandler<TransportStateChangedEventArgs> StateChanged;
    
    /// <summary>Event fired when transport position changes</summary>
    event EventHandler<TransportPositionChangedEventArgs> PositionChanged;
}