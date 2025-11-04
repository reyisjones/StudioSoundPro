namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Represents the current state of the transport system
/// </summary>
public enum TransportState
{
    /// <summary>Transport is stopped</summary>
    Stopped,
    
    /// <summary>Transport is playing back audio</summary>
    Playing,
    
    /// <summary>Transport is paused (maintains current position)</summary>
    Paused,
    
    /// <summary>Transport is recording audio</summary>
    Recording
}

/// <summary>
/// Event arguments for transport state changes
/// </summary>
public class TransportStateChangedEventArgs : EventArgs
{
    public TransportState PreviousState { get; init; }
    public TransportState CurrentState { get; init; }
    public long SamplePosition { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for transport position changes
/// </summary>
public class TransportPositionChangedEventArgs : EventArgs
{
    public long SamplePosition { get; init; }
    public double TimeSeconds { get; init; }
    public int Bar { get; init; }
    public int Beat { get; init; }
    public int Tick { get; init; }
}