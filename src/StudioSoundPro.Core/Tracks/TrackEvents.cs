namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Event arguments for clip property changes
/// </summary>
public class ClipPropertyChangedEventArgs : EventArgs
{
    /// <summary>Gets the name of the property that changed</summary>
    public string PropertyName { get; init; } = string.Empty;
    
    /// <summary>Gets the clip that changed</summary>
    public IClip Clip { get; init; } = null!;
}

/// <summary>
/// Event arguments for track property changes
/// </summary>
public class TrackPropertyChangedEventArgs : EventArgs
{
    /// <summary>Gets the name of the property that changed</summary>
    public string PropertyName { get; init; } = string.Empty;
    
    /// <summary>Gets the track that changed</summary>
    public ITrack Track { get; init; } = null!;
}

/// <summary>
/// Event arguments for clip events on tracks
/// </summary>
public class ClipEventArgs : EventArgs
{
    /// <summary>Gets the clip associated with the event</summary>
    public IClip Clip { get; init; } = null!;
    
    /// <summary>Gets the track associated with the event</summary>
    public ITrack Track { get; init; } = null!;
}
