namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Provides high-precision timing and synchronization for the DAW
/// </summary>
public interface IClock
{
    /// <summary>Gets the current sample rate in Hz</summary>
    int SampleRate { get; }
    
    /// <summary>Gets the current tempo in beats per minute</summary>
    double Tempo { get; set; }
    
    /// <summary>Gets the time signature numerator (beats per bar)</summary>
    int TimeSignatureNumerator { get; set; }
    
    /// <summary>Gets the time signature denominator (note value for one beat)</summary>
    int TimeSignatureDenominator { get; set; }
    
    /// <summary>Gets the number of ticks per quarter note for MIDI timing</summary>
    int TicksPerQuarterNote { get; }
    
    /// <summary>Converts sample position to time in seconds</summary>
    double SamplesToSeconds(long samples);
    
    /// <summary>Converts time in seconds to sample position</summary>
    long SecondsToSamples(double seconds);
    
    /// <summary>Converts sample position to musical time (bar, beat, tick)</summary>
    (int bar, int beat, int tick) SamplesToMusicalTime(long samples);
    
    /// <summary>Converts musical time to sample position</summary>
    long MusicalTimeToSamples(int bar, int beat, int tick);
    
    /// <summary>Gets the sample length of one beat at current tempo</summary>
    long GetBeatLengthInSamples();
    
    /// <summary>Gets the sample length of one bar at current tempo and time signature</summary>
    long GetBarLengthInSamples();
    
    /// <summary>Event fired when tempo changes</summary>
    event EventHandler<double> TempoChanged;
    
    /// <summary>Event fired when time signature changes</summary>
    event EventHandler<(int numerator, int denominator)> TimeSignatureChanged;
}