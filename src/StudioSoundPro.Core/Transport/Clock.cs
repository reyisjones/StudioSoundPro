namespace StudioSoundPro.Core.Transport;

/// <summary>
/// High-precision clock implementation for DAW timing and synchronization
/// </summary>
public class Clock : IClock
{
    private double _tempo = 120.0;
    private int _timeSignatureNumerator = 4;
    private int _timeSignatureDenominator = 4;
    private readonly object _lockObject = new();

    public Clock(int sampleRate, int ticksPerQuarterNote = 480)
    {
        SampleRate = sampleRate;
        TicksPerQuarterNote = ticksPerQuarterNote;
    }

    public int SampleRate { get; }
    
    public int TicksPerQuarterNote { get; }

    public double Tempo
    {
        get
        {
            lock (_lockObject)
            {
                return _tempo;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Tempo must be positive");

            double oldTempo;
            lock (_lockObject)
            {
                oldTempo = _tempo;
                _tempo = value;
            }

            if (Math.Abs(oldTempo - value) > 0.001)
            {
                TempoChanged?.Invoke(this, value);
            }
        }
    }

    public int TimeSignatureNumerator
    {
        get
        {
            lock (_lockObject)
            {
                return _timeSignatureNumerator;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Time signature numerator must be positive");

            int oldNumerator, oldDenominator;
            lock (_lockObject)
            {
                oldNumerator = _timeSignatureNumerator;
                oldDenominator = _timeSignatureDenominator;
                _timeSignatureNumerator = value;
            }

            if (oldNumerator != value)
            {
                TimeSignatureChanged?.Invoke(this, (value, oldDenominator));
            }
        }
    }

    public int TimeSignatureDenominator
    {
        get
        {
            lock (_lockObject)
            {
                return _timeSignatureDenominator;
            }
        }
        set
        {
            if (value <= 0 || (value & (value - 1)) != 0) // Must be power of 2
                throw new ArgumentOutOfRangeException(nameof(value), "Time signature denominator must be a positive power of 2");

            int oldNumerator, oldDenominator;
            lock (_lockObject)
            {
                oldNumerator = _timeSignatureNumerator;
                oldDenominator = _timeSignatureDenominator;
                _timeSignatureDenominator = value;
            }

            if (oldDenominator != value)
            {
                TimeSignatureChanged?.Invoke(this, (oldNumerator, value));
            }
        }
    }

    public double SamplesToSeconds(long samples)
    {
        return (double)samples / SampleRate;
    }

    public long SecondsToSamples(double seconds)
    {
        return (long)(seconds * SampleRate);
    }

    public (int bar, int beat, int tick) SamplesToMusicalTime(long samples)
    {
        lock (_lockObject)
        {
            // Calculate total ticks from samples
            double secondsPerBeat = 60.0 / _tempo;
            double secondsPerQuarterNote = 60.0 / _tempo;
            double beatsPerSecond = _tempo / 60.0;
            
            double seconds = SamplesToSeconds(samples);
            double totalBeats = seconds * beatsPerSecond;
            
            // Adjust for time signature (if denominator is not 4, adjust beat calculation)
            double quarterNotesPerBeat = 4.0 / _timeSignatureDenominator;
            double adjustedBeats = totalBeats / quarterNotesPerBeat;
            
            long totalTicks = (long)(adjustedBeats * TicksPerQuarterNote);
            
            // Calculate bars, beats, and ticks
            long ticksPerBeat = TicksPerQuarterNote;
            long ticksPerBar = ticksPerBeat * _timeSignatureNumerator;
            
            int bar = (int)(totalTicks / ticksPerBar) + 1; // 1-based
            long remainingTicks = totalTicks % ticksPerBar;
            
            int beat = (int)(remainingTicks / ticksPerBeat) + 1; // 1-based
            int tick = (int)(remainingTicks % ticksPerBeat);
            
            return (bar, beat, tick);
        }
    }

    public long MusicalTimeToSamples(int bar, int beat, int tick)
    {
        if (bar < 1) throw new ArgumentOutOfRangeException(nameof(bar), "Bar must be 1 or greater");
        if (beat < 1) throw new ArgumentOutOfRangeException(nameof(beat), "Beat must be 1 or greater");
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), "Tick must be 0 or greater");

        lock (_lockObject)
        {
            // Convert to 0-based
            int barIndex = bar - 1;
            int beatIndex = beat - 1;
            
            // Calculate total ticks
            long ticksPerBeat = TicksPerQuarterNote;
            long ticksPerBar = ticksPerBeat * _timeSignatureNumerator;
            
            long totalTicks = (barIndex * ticksPerBar) + (beatIndex * ticksPerBeat) + tick;
            
            // Convert ticks to beats, then to seconds, then to samples
            double quarterNotesPerBeat = 4.0 / _timeSignatureDenominator;
            double totalBeats = (double)totalTicks / TicksPerQuarterNote * quarterNotesPerBeat;
            
            double beatsPerSecond = _tempo / 60.0;
            double seconds = totalBeats / beatsPerSecond;
            
            return SecondsToSamples(seconds);
        }
    }

    public long GetBeatLengthInSamples()
    {
        lock (_lockObject)
        {
            // Calculate samples per beat based on tempo and time signature
            double secondsPerBeat = 60.0 / _tempo;
            double quarterNotesPerBeat = 4.0 / _timeSignatureDenominator;
            double secondsPerQuarterNote = secondsPerBeat / quarterNotesPerBeat;
            
            return SecondsToSamples(secondsPerQuarterNote);
        }
    }

    public long GetBarLengthInSamples()
    {
        lock (_lockObject)
        {
            return GetBeatLengthInSamples() * _timeSignatureNumerator;
        }
    }

    public event EventHandler<double>? TempoChanged;
    public event EventHandler<(int numerator, int denominator)>? TimeSignatureChanged;
}