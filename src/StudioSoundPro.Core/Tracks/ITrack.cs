namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Represents an audio or MIDI track in the DAW
/// </summary>
public interface ITrack : IDisposable
{
    /// <summary>Gets the unique identifier for this track</summary>
    Guid Id { get; }
    
    /// <summary>Gets or sets the name of the track</summary>
    string Name { get; set; }
    
    /// <summary>Gets or sets the track color for visual representation</summary>
    string Color { get; set; }
    
    /// <summary>Gets or sets whether the track is muted</summary>
    bool IsMuted { get; set; }
    
    /// <summary>Gets or sets whether the track is soloed</summary>
    bool IsSolo { get; set; }
    
    /// <summary>Gets or sets whether the track is armed for recording</summary>
    bool IsArmed { get; set; }
    
    /// <summary>Gets or sets the track volume (0.0 to 1.0, where 1.0 is unity gain)</summary>
    float Volume { get; set; }
    
    /// <summary>Gets or sets the track pan (-1.0 left to +1.0 right, 0.0 is center)</summary>
    float Pan { get; set; }
    
    /// <summary>Gets the collection of clips on this track</summary>
    IReadOnlyList<IClip> Clips { get; }
    
    /// <summary>Adds a clip to the track</summary>
    /// <param name="clip">The clip to add</param>
    void AddClip(IClip clip);
    
    /// <summary>Removes a clip from the track</summary>
    /// <param name="clip">The clip to remove</param>
    /// <returns>True if the clip was removed, false if not found</returns>
    bool RemoveClip(IClip clip);
    
    /// <summary>Removes a clip by its ID</summary>
    /// <param name="clipId">The ID of the clip to remove</param>
    /// <returns>True if the clip was removed, false if not found</returns>
    bool RemoveClip(Guid clipId);
    
    /// <summary>Gets all clips that intersect with the specified time range</summary>
    /// <param name="startPosition">Start position in samples</param>
    /// <param name="endPosition">End position in samples</param>
    /// <returns>Collection of clips in the specified range</returns>
    IEnumerable<IClip> GetClipsInRange(long startPosition, long endPosition);
    
    /// <summary>Processes audio for this track at the specified position</summary>
    /// <param name="buffer">Audio buffer to fill</param>
    /// <param name="offset">Offset into the buffer</param>
    /// <param name="count">Number of samples to process</param>
    /// <param name="position">Current transport position in samples</param>
    void ProcessAudio(float[] buffer, int offset, int count, long position);
    
    /// <summary>Event fired when track properties change</summary>
    event EventHandler<TrackPropertyChangedEventArgs>? PropertyChanged;
    
    /// <summary>Event fired when a clip is added to the track</summary>
    event EventHandler<ClipEventArgs>? ClipAdded;
    
    /// <summary>Event fired when a clip is removed from the track</summary>
    event EventHandler<ClipEventArgs>? ClipRemoved;
}
