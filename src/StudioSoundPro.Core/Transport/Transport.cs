namespace StudioSoundPro.Core.Transport;

/// <summary>
/// Transport implementation for DAW playback control
/// </summary>
public class Transport : ITransport
{
    private TransportState _state = TransportState.Stopped;
    private long _position = 0;
    private long _stopPosition = 0;
    private bool _isLooping = false;
    private long _loopStart = 0;
    private long _loopEnd = 0;
    private readonly object _lockObject = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

    public Transport(IClock clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public TransportState State
    {
        get
        {
            lock (_lockObject)
            {
                return _state;
            }
        }
    }

    public long Position
    {
        get
        {
            lock (_lockObject)
            {
                return _position;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be negative");

            long oldPosition;
            lock (_lockObject)
            {
                oldPosition = _position;
                _position = value;
            }

            if (oldPosition != value)
            {
                FirePositionChanged(value);
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            lock (_lockObject)
            {
                return _state == TransportState.Playing || _state == TransportState.Recording;
            }
        }
    }

    public bool IsLooping
    {
        get
        {
            lock (_lockObject)
            {
                return _isLooping;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _isLooping = value;
            }
        }
    }

    public long LoopStart
    {
        get
        {
            lock (_lockObject)
            {
                return _loopStart;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Loop start cannot be negative");

            lock (_lockObject)
            {
                _loopStart = value;
                if (_loopEnd <= _loopStart)
                {
                    _loopEnd = _loopStart + Clock.GetBarLengthInSamples();
                }
            }
        }
    }

    public long LoopEnd
    {
        get
        {
            lock (_lockObject)
            {
                return _loopEnd;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Loop end cannot be negative");

            lock (_lockObject)
            {
                _loopEnd = value;
                if (_loopStart >= _loopEnd)
                {
                    _loopStart = Math.Max(0, _loopEnd - Clock.GetBarLengthInSamples());
                }
            }
        }
    }

    public IClock Clock { get; }

    public async Task PlayAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            TransportState oldState;
            lock (_lockObject)
            {
                oldState = _state;
                if (_state == TransportState.Stopped)
                {
                    // Starting from stop - use current position
                }
                else if (_state == TransportState.Paused)
                {
                    // Resuming from pause - keep current position
                }
                else if (_state == TransportState.Playing)
                {
                    // Already playing
                    return;
                }
                else if (_state == TransportState.Recording)
                {
                    // Switch from recording to playing
                }

                _state = TransportState.Playing;
            }

            FireStateChanged(oldState, TransportState.Playing);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task PauseAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            TransportState oldState;
            lock (_lockObject)
            {
                oldState = _state;
                if (_state == TransportState.Playing || _state == TransportState.Recording)
                {
                    _state = TransportState.Paused;
                }
                else
                {
                    return; // Already paused or stopped
                }
            }

            FireStateChanged(oldState, TransportState.Paused);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task StopAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            TransportState oldState;
            long newPosition;
            lock (_lockObject)
            {
                oldState = _state;
                if (_state == TransportState.Stopped)
                {
                    return; // Already stopped
                }

                _state = TransportState.Stopped;
                newPosition = _stopPosition;
                _position = newPosition;
            }

            FireStateChanged(oldState, TransportState.Stopped);
            FirePositionChanged(newPosition);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task RecordAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            TransportState oldState;
            lock (_lockObject)
            {
                oldState = _state;
                if (_state == TransportState.Recording)
                {
                    return; // Already recording
                }

                _state = TransportState.Recording;
            }

            FireStateChanged(oldState, TransportState.Recording);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task SeekAsync(long samplePosition)
    {
        if (samplePosition < 0)
            throw new ArgumentOutOfRangeException(nameof(samplePosition), "Sample position cannot be negative");

        await _operationSemaphore.WaitAsync();
        try
        {
            long oldPosition;
            lock (_lockObject)
            {
                oldPosition = _position;
                _position = samplePosition;
                
                // Update stop position if we're currently stopped
                if (_state == TransportState.Stopped)
                {
                    _stopPosition = samplePosition;
                }
            }

            if (oldPosition != samplePosition)
            {
                FirePositionChanged(samplePosition);
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task RewindAsync()
    {
        await SeekAsync(0);
    }

    public void Advance(int sampleCount)
    {
        if (sampleCount <= 0)
            return;

        long newPosition;
        bool shouldFirePositionEvent = false;

        lock (_lockObject)
        {
            if (!IsRunning)
                return;

            long oldPosition = _position;
            newPosition = _position + sampleCount;

            // Handle looping
            if (_isLooping && _loopEnd > _loopStart)
            {
                if (newPosition >= _loopEnd)
                {
                    // Calculate how far past the loop end we went
                    long overflow = newPosition - _loopEnd;
                    long loopLength = _loopEnd - _loopStart;
                    
                    if (loopLength > 0)
                    {
                        // Wrap around to loop start + overflow
                        newPosition = _loopStart + (overflow % loopLength);
                    }
                }
            }

            _position = newPosition;
            shouldFirePositionEvent = oldPosition != newPosition;
        }

        if (shouldFirePositionEvent)
        {
            FirePositionChanged(newPosition);
        }
    }

    private void FireStateChanged(TransportState oldState, TransportState newState)
    {
        var args = new TransportStateChangedEventArgs
        {
            PreviousState = oldState,
            CurrentState = newState,
            SamplePosition = Position,
            Timestamp = DateTime.UtcNow
        };

        StateChanged?.Invoke(this, args);
    }

    private void FirePositionChanged(long position)
    {
        var (bar, beat, tick) = Clock.SamplesToMusicalTime(position);
        var timeSeconds = Clock.SamplesToSeconds(position);

        var args = new TransportPositionChangedEventArgs
        {
            SamplePosition = position,
            TimeSeconds = timeSeconds,
            Bar = bar,
            Beat = beat,
            Tick = tick
        };

        PositionChanged?.Invoke(this, args);
    }

    public event EventHandler<TransportStateChangedEventArgs>? StateChanged;
    public event EventHandler<TransportPositionChangedEventArgs>? PositionChanged;

    public void Dispose()
    {
        _operationSemaphore?.Dispose();
    }
}