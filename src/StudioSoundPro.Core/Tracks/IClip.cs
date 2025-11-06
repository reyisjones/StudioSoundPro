namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Represents a clip of audio or MIDI data that can be placed on a track
/// </summary>
public interface IClip
{
    /// <summary>Gets the unique identifier for this clip</summary>
    Guid Id { get; }
    
    /// <summary>Gets or sets the name of the clip</summary>
    string Name { get; set; }
    
    /// <summary>Gets the start position of the clip in samples</summary>
    long StartPosition { get; set; }
    
    /// <summary>Gets the length of the clip in samples</summary>
    long Length { get; set; }
    
    /// <summary>Gets the end position of the clip in samples (StartPosition + Length)</summary>
    long EndPosition { get; }
    
    /// <summary>Gets or sets the offset into the source data in samples</summary>
    long SourceOffset { get; set; }
    
    /// <summary>Gets or sets whether the clip is muted</summary>
    bool IsMuted { get; set; }
    
    /// <summary>Gets or sets the clip volume/gain (0.0 to 1.0, where 1.0 is unity gain)</summary>
    float Gain { get; set; }
    
    /// <summary>Gets or sets the clip fade-in length in samples</summary>
    long FadeInLength { get; set; }
    
    /// <summary>Gets or sets the clip fade-out length in samples</summary>
    long FadeOutLength { get; set; }
    
    /// <summary>Gets or sets the color for visual representation</summary>
    string Color { get; set; }
    
    /// <summary>Event fired when clip properties change</summary>
    event EventHandler<ClipPropertyChangedEventArgs>? PropertyChanged;
}
