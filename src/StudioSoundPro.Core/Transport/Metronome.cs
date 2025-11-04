namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Metronome implementation with configurable click sounds and timing
/// </summary>
public class Metronome : IMetronome
{
    private bool _isEnabled = false;
    private bool _playDuringPlayback = true;
    private bool _playDuringRecording = true;
    private float _volume = 0.5f;
    private bool _accentFirstBeat = true;
    private float _downbeatFrequency = 1000.0f; // Hz
    private float _beatFrequency = 800.0f; // Hz
    private int _clickDurationMs = 50;
    private readonly object _lockObject = new();

    // Click generation state
    private double _clickPhase = 0.0;
    private int _clickSamplesRemaining = 0;
    private bool _isDownbeat = false;

    public bool IsEnabled
    {
        get
        {
            lock (_lockObject)
            {
                return _isEnabled;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _isEnabled = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool PlayDuringPlayback
    {
        get
        {
            lock (_lockObject)
            {
                return _playDuringPlayback;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _playDuringPlayback = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool PlayDuringRecording
    {
        get
        {
            lock (_lockObject)
            {
                return _playDuringRecording;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _playDuringRecording = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float Volume
    {
        get
        {
            lock (_lockObject)
            {
                return _volume;
            }
        }
        set
        {
            if (value < 0.0f || value > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");

            lock (_lockObject)
            {
                _volume = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool AccentFirstBeat
    {
        get
        {
            lock (_lockObject)
            {
                return _accentFirstBeat;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _accentFirstBeat = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float DownbeatFrequency
    {
        get
        {
            lock (_lockObject)
            {
                return _downbeatFrequency;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Frequency must be positive");

            lock (_lockObject)
            {
                _downbeatFrequency = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float BeatFrequency
    {
        get
        {
            lock (_lockObject)
            {
                return _beatFrequency;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Frequency must be positive");

            lock (_lockObject)
            {
                _beatFrequency = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int ClickDurationMs
    {
        get
        {
            lock (_lockObject)
            {
                return _clickDurationMs;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Click duration must be positive");

            lock (_lockObject)
            {
                _clickDurationMs = value;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ProcessBuffer(Span<float> buffer, int sampleCount, long startPosition, IClock clock)
    {
        if (!IsEnabled || sampleCount <= 0)
            return;

        // Get current settings in a thread-safe way
        bool enabled, playDuringPlayback, playDuringRecording, accentFirstBeat;
        float volume, downbeatFreq, beatFreq;
        int clickDuration;

        lock (_lockObject)
        {
            enabled = _isEnabled;
            playDuringPlayback = _playDuringPlayback;
            playDuringRecording = _playDuringRecording;
            volume = _volume;
            accentFirstBeat = _accentFirstBeat;
            downbeatFreq = _downbeatFrequency;
            beatFreq = _beatFrequency;
            clickDuration = _clickDurationMs;
        }

        if (!enabled || volume <= 0.0f)
            return;

        int clickSamples = (int)(clickDuration * clock.SampleRate / 1000.0);
        long beatLengthSamples = clock.GetBeatLengthInSamples();

        for (int i = 0; i < sampleCount; i++)
        {
            long currentPosition = startPosition + i;
            
            // Check if we're on a beat boundary
            if (beatLengthSamples > 0 && currentPosition % beatLengthSamples == 0)
            {
                // Determine if this is a downbeat (first beat of a bar)
                var (bar, beat, tick) = clock.SamplesToMusicalTime(currentPosition);
                _isDownbeat = accentFirstBeat && beat == 1;
                
                // Start a new click
                _clickSamplesRemaining = clickSamples;
                _clickPhase = 0.0;
            }

            // Generate click sound if we're in the middle of a click
            if (_clickSamplesRemaining > 0)
            {
                float frequency = _isDownbeat ? downbeatFreq : beatFreq;
                float clickVolume = _isDownbeat ? volume * 1.2f : volume; // Accent downbeat
                clickVolume = Math.Min(clickVolume, 1.0f);

                // Generate sine wave click with envelope
                double phaseIncrement = 2.0 * Math.PI * frequency / clock.SampleRate;
                float envelope = GetClickEnvelope(_clickSamplesRemaining, clickSamples);
                float sample = (float)(Math.Sin(_clickPhase) * clickVolume * envelope);

                // Mix with existing buffer content (add to mono)
                if (buffer.Length > i)
                {
                    buffer[i] += sample;
                }

                _clickPhase += phaseIncrement;
                if (_clickPhase >= 2.0 * Math.PI)
                    _clickPhase -= 2.0 * Math.PI;

                _clickSamplesRemaining--;
            }
        }
    }

    private static float GetClickEnvelope(int samplesRemaining, int totalSamples)
    {
        if (totalSamples <= 0)
            return 0.0f;

        float progress = 1.0f - (float)samplesRemaining / totalSamples;
        
        // Simple attack-decay envelope
        if (progress < 0.1f)
        {
            // Quick attack
            return progress / 0.1f;
        }
        else
        {
            // Exponential decay
            float decayProgress = (progress - 0.1f) / 0.9f;
            return (float)Math.Exp(-3.0 * decayProgress); // e^(-3x) gives nice decay
        }
    }

    public event EventHandler<EventArgs>? SettingsChanged;
}