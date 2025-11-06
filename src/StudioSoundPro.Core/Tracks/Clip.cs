namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Base implementation of an audio or data clip
/// </summary>
public class Clip : IClip
{
    private string _name = "New Clip";
    private long _startPosition = 0;
    private long _length = 0;
    private long _sourceOffset = 0;
    private bool _isMuted = false;
    private float _gain = 1.0f;
    private long _fadeInLength = 0;
    private long _fadeOutLength = 0;
    private string _color = "#4A90E2";
    private readonly object _lockObject = new();

    public Clip(string name = "New Clip")
    {
        Id = Guid.NewGuid();
        _name = name;
    }

    public Guid Id { get; }

    public string Name
    {
        get
        {
            lock (_lockObject)
            {
                return _name;
            }
        }
        set
        {
            lock (_lockObject)
            {
                if (_name != value)
                {
                    _name = value ?? string.Empty;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
    }

    public long StartPosition
    {
        get
        {
            lock (_lockObject)
            {
                return _startPosition;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Start position cannot be negative");

            lock (_lockObject)
            {
                if (_startPosition != value)
                {
                    _startPosition = value;
                    OnPropertyChanged(nameof(StartPosition));
                    OnPropertyChanged(nameof(EndPosition));
                }
            }
        }
    }

    public long Length
    {
        get
        {
            lock (_lockObject)
            {
                return _length;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Length cannot be negative");

            lock (_lockObject)
            {
                if (_length != value)
                {
                    _length = value;
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(EndPosition));
                }
            }
        }
    }

    public long EndPosition
    {
        get
        {
            lock (_lockObject)
            {
                return _startPosition + _length;
            }
        }
    }

    public long SourceOffset
    {
        get
        {
            lock (_lockObject)
            {
                return _sourceOffset;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Source offset cannot be negative");

            lock (_lockObject)
            {
                if (_sourceOffset != value)
                {
                    _sourceOffset = value;
                    OnPropertyChanged(nameof(SourceOffset));
                }
            }
        }
    }

    public bool IsMuted
    {
        get
        {
            lock (_lockObject)
            {
                return _isMuted;
            }
        }
        set
        {
            lock (_lockObject)
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged(nameof(IsMuted));
                }
            }
        }
    }

    public float Gain
    {
        get
        {
            lock (_lockObject)
            {
                return _gain;
            }
        }
        set
        {
            if (value < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Gain cannot be negative");

            lock (_lockObject)
            {
                if (Math.Abs(_gain - value) > 0.0001f)
                {
                    _gain = value;
                    OnPropertyChanged(nameof(Gain));
                }
            }
        }
    }

    public long FadeInLength
    {
        get
        {
            lock (_lockObject)
            {
                return _fadeInLength;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Fade-in length cannot be negative");

            lock (_lockObject)
            {
                if (_fadeInLength != value)
                {
                    _fadeInLength = value;
                    OnPropertyChanged(nameof(FadeInLength));
                }
            }
        }
    }

    public long FadeOutLength
    {
        get
        {
            lock (_lockObject)
            {
                return _fadeOutLength;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Fade-out length cannot be negative");

            lock (_lockObject)
            {
                if (_fadeOutLength != value)
                {
                    _fadeOutLength = value;
                    OnPropertyChanged(nameof(FadeOutLength));
                }
            }
        }
    }

    public string Color
    {
        get
        {
            lock (_lockObject)
            {
                return _color;
            }
        }
        set
        {
            lock (_lockObject)
            {
                if (_color != value)
                {
                    _color = value ?? "#4A90E2";
                    OnPropertyChanged(nameof(Color));
                }
            }
        }
    }

    public event EventHandler<ClipPropertyChangedEventArgs>? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new ClipPropertyChangedEventArgs
        {
            PropertyName = propertyName,
            Clip = this
        });
    }

    /// <summary>
    /// Calculates the fade envelope at the specified position within the clip
    /// </summary>
    /// <param name="relativePosition">Position relative to clip start (0 = start of clip)</param>
    /// <returns>Fade multiplier (0.0 to 1.0)</returns>
    protected float CalculateFadeEnvelope(long relativePosition)
    {
        if (relativePosition < 0 || relativePosition >= _length)
            return 0.0f;

        float envelope = 1.0f;

        // Fade in
        if (_fadeInLength > 0 && relativePosition < _fadeInLength)
        {
            envelope *= (float)relativePosition / _fadeInLength;
        }

        // Fade out
        if (_fadeOutLength > 0 && relativePosition >= (_length - _fadeOutLength))
        {
            long fadeOutStart = _length - _fadeOutLength;
            long fadeOutProgress = relativePosition - fadeOutStart;
            envelope *= 1.0f - ((float)fadeOutProgress / _fadeOutLength);
        }

        return envelope;
    }
}
