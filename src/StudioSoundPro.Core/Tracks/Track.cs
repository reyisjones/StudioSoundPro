namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Represents an audio track with clip management and audio processing
/// </summary>
public class Track : ITrack
{
    private readonly List<IClip> _clips = new();
    private readonly object _lock = new();
    private string _name = "New Track";
    private float _volume = 1.0f;
    private float _pan = 0.0f;
    private bool _isMuted = false;
    private bool _isSolo = false;
    private bool _isArmed = false;
    private string _color = "#4A90E2";

    public Track(string name = "New Track")
    {
        Id = Guid.NewGuid();
        _name = name;
    }

    public Guid Id { get; }

    public string Name
    {
        get
        {
            lock (_lock)
            {
                return _name;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_name != value)
                {
                    _name = value ?? string.Empty;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
    }

    public float Volume
    {
        get
        {
            lock (_lock)
            {
                return _volume;
            }
        }
        set
        {
            if (value < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Volume cannot be negative");

            lock (_lock)
            {
                if (Math.Abs(_volume - value) > 0.0001f)
                {
                    _volume = value;
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }
    }

    public float Pan
    {
        get
        {
            lock (_lock)
            {
                return _pan;
            }
        }
        set
        {
            if (value < -1.0f || value > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Pan must be between -1.0 and 1.0");

            lock (_lock)
            {
                if (Math.Abs(_pan - value) > 0.0001f)
                {
                    _pan = value;
                    OnPropertyChanged(nameof(Pan));
                }
            }
        }
    }

    public bool IsMuted
    {
        get
        {
            lock (_lock)
            {
                return _isMuted;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged(nameof(IsMuted));
                }
            }
        }
    }

    public bool IsSolo
    {
        get
        {
            lock (_lock)
            {
                return _isSolo;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_isSolo != value)
                {
                    _isSolo = value;
                    OnPropertyChanged(nameof(IsSolo));
                }
            }
        }
    }

    public bool IsArmed
    {
        get
        {
            lock (_lock)
            {
                return _isArmed;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_isArmed != value)
                {
                    _isArmed = value;
                    OnPropertyChanged(nameof(IsArmed));
                }
            }
        }
    }

    public string Color
    {
        get
        {
            lock (_lock)
            {
                return _color;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_color != value)
                {
                    _color = value ?? "#4A90E2";
                    OnPropertyChanged(nameof(Color));
                }
            }
        }
    }

    public IReadOnlyList<IClip> Clips
    {
        get
        {
            lock (_lock)
            {
                return _clips.ToList().AsReadOnly();
            }
        }
    }

    public event EventHandler<TrackPropertyChangedEventArgs>? PropertyChanged;
    public event EventHandler<ClipEventArgs>? ClipAdded;
    public event EventHandler<ClipEventArgs>? ClipRemoved;

    public void AddClip(IClip clip)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        lock (_lock)
        {
            if (_clips.Contains(clip))
                return;

            _clips.Add(clip);
            
            // Subscribe to clip property changes
            clip.PropertyChanged += OnClipPropertyChanged;
        }

        ClipAdded?.Invoke(this, new ClipEventArgs { Clip = clip, Track = this });
    }

    public bool RemoveClip(IClip clip)
    {
        if (clip == null)
            return false;

        bool removed;
        lock (_lock)
        {
            removed = _clips.Remove(clip);
            
            if (removed)
            {
                // Unsubscribe from clip property changes
                clip.PropertyChanged -= OnClipPropertyChanged;
            }
        }

        if (removed)
        {
            ClipRemoved?.Invoke(this, new ClipEventArgs { Clip = clip, Track = this });
        }

        return removed;
    }

    public bool RemoveClip(Guid clipId)
    {
        IClip? clipToRemove = null;
        
        lock (_lock)
        {
            clipToRemove = _clips.FirstOrDefault(c => c.Id == clipId);
            if (clipToRemove != null)
            {
                _clips.Remove(clipToRemove);
                clipToRemove.PropertyChanged -= OnClipPropertyChanged;
            }
        }

        if (clipToRemove != null)
        {
            ClipRemoved?.Invoke(this, new ClipEventArgs { Clip = clipToRemove, Track = this });
            return true;
        }

        return false;
    }

    public void ClearClips()
    {
        List<IClip> clipsToRemove;
        
        lock (_lock)
        {
            clipsToRemove = _clips.ToList();
            _clips.Clear();
        }

        // Unsubscribe and notify outside the lock
        foreach (var clip in clipsToRemove)
        {
            clip.PropertyChanged -= OnClipPropertyChanged;
            ClipRemoved?.Invoke(this, new ClipEventArgs { Clip = clip, Track = this });
        }
    }

    public IEnumerable<IClip> GetClipsInRange(long startPosition, long endPosition)
    {
        if (endPosition < startPosition)
            throw new ArgumentException("End position must be greater than or equal to start position");

        lock (_lock)
        {
            return _clips
                .Where(c => c.EndPosition > startPosition && c.StartPosition < endPosition)
                .OrderBy(c => c.StartPosition)
                .ToList();
        }
    }

    public void ProcessAudio(float[] buffer, int offset, int count, long position)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
        if (offset + count > buffer.Length)
            throw new ArgumentException("Buffer is too small for the requested offset and count");

        // Clear buffer section if muted
        if (_isMuted)
        {
            Array.Clear(buffer, offset, count);
            return;
        }

        // Temporary buffer for mixing
        float[] tempBuffer = new float[count];
        bool hasAudio = false;

        lock (_lock)
        {
            // Get clips that overlap the playback position
            var activeClips = _clips
                .Where(c => c.EndPosition > position && c.StartPosition < position + count)
                .ToList();

            foreach (var clip in activeClips)
            {
                if (clip is IAudioClip audioClip)
                {
                    Array.Clear(tempBuffer, 0, count);
                    int samplesRead = audioClip.ReadSamples(tempBuffer, 0, count, position);

                    if (samplesRead > 0)
                    {
                        // Mix clip audio into main buffer
                        for (int i = 0; i < count; i++)
                        {
                            buffer[offset + i] += tempBuffer[i];
                        }
                        hasAudio = true;
                    }
                }
            }
        }

        // Apply track volume if we have audio
        if (hasAudio)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= _volume;
            }
        }
    }

    private void ApplyVolumeAndPan(float[] buffer, int channels, float volume, float pan)
    {
        if (channels == 1)
        {
            // Mono: apply volume only
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= volume;
            }
        }
        else if (channels == 2)
        {
            // Stereo: apply volume and pan using constant power panning
            float panAngle = (pan + 1.0f) * 0.25f * MathF.PI; // Map -1..1 to 0..PI/2
            float leftGain = MathF.Cos(panAngle) * volume;
            float rightGain = MathF.Sin(panAngle) * volume;

            for (int i = 0; i < buffer.Length; i += 2)
            {
                buffer[i] *= leftGain;      // Left channel
                buffer[i + 1] *= rightGain; // Right channel
            }
        }
        else
        {
            // Multi-channel: apply volume to all channels
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= volume;
            }
        }
    }

    private void OnClipPropertyChanged(object? sender, ClipPropertyChangedEventArgs e)
    {
        // Forward clip property changes to track listeners
        PropertyChanged?.Invoke(this, new TrackPropertyChangedEventArgs
        {
            PropertyName = $"Clip.{e.PropertyName}",
            Track = this
        });
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new TrackPropertyChangedEventArgs
        {
            PropertyName = propertyName,
            Track = this
        });
    }

    /// <summary>
    /// Gets the peak amplitude for this track at the specified position
    /// </summary>
    public float GetPeakAmplitude(long position, int windowSize = 1024)
    {
        if (_isMuted)
            return 0.0f;

        float peak = 0.0f;

        lock (_lock)
        {
            var activeClips = _clips
                .Where(c => c.EndPosition > position && c.StartPosition < position + windowSize)
                .OfType<IAudioClip>()
                .ToList();

            foreach (var clip in activeClips)
            {
                float clipPeak = clip.GetPeakAmplitude(position, windowSize);
                if (clipPeak > peak)
                    peak = clipPeak;
            }
        }

        return peak * _volume;
    }

    /// <summary>
    /// Moves a clip to a new position on the timeline
    /// </summary>
    public void MoveClip(IClip clip, long newStartPosition)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));
        if (newStartPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(newStartPosition), "Position cannot be negative");

        lock (_lock)
        {
            if (!_clips.Contains(clip))
                throw new InvalidOperationException("Clip is not part of this track");

            clip.StartPosition = newStartPosition;
        }
    }

    /// <summary>
    /// Trims a clip to fit within specified bounds
    /// </summary>
    public void TrimClip(IClip clip, long? newStart, long? newLength)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        lock (_lock)
        {
            if (!_clips.Contains(clip))
                throw new InvalidOperationException("Clip is not part of this track");

            if (newStart.HasValue)
            {
                if (newStart.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(newStart), "Start position cannot be negative");
                
                long offset = newStart.Value - clip.StartPosition;
                clip.StartPosition = newStart.Value;
                clip.SourceOffset += offset;
                
                if (newLength.HasValue)
                {
                    clip.Length = newLength.Value;
                }
                else
                {
                    clip.Length = Math.Max(0, clip.Length - offset);
                }
            }
            else if (newLength.HasValue)
            {
                if (newLength.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(newLength), "Length cannot be negative");
                
                clip.Length = newLength.Value;
            }
        }
    }

    /// <summary>
    /// Splits a clip at the specified position
    /// </summary>
    public IClip? SplitClip(IClip clip, long splitPosition)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));
        if (splitPosition <= clip.StartPosition || splitPosition >= clip.EndPosition)
            throw new ArgumentOutOfRangeException(nameof(splitPosition), "Split position must be within clip bounds");

        lock (_lock)
        {
            if (!_clips.Contains(clip))
                throw new InvalidOperationException("Clip is not part of this track");

            if (clip is IAudioClip audioClip)
            {
                // Create new clip for the right side
                var audioData = audioClip.AudioData ?? Array.Empty<float>();
                var rightClip = new AudioClip(
                    $"{clip.Name} (2)",
                    audioData,
                    audioClip.Channels,
                    audioClip.SampleRate)
                {
                    StartPosition = splitPosition,
                    Length = clip.EndPosition - splitPosition,
                    SourceOffset = clip.SourceOffset + (splitPosition - clip.StartPosition),
                    Gain = clip.Gain,
                    IsMuted = clip.IsMuted,
                    FadeInLength = 0,
                    FadeOutLength = clip.FadeOutLength,
                    Color = clip.Color
                };

                // Adjust original clip
                clip.Length = splitPosition - clip.StartPosition;
                clip.FadeOutLength = 0;

                AddClip(rightClip);
                return rightClip;
            }
        }

        return null;
    }

    public void Dispose()
    {
        // No unmanaged resources to dispose
        // Just clear references
        ClearClips();
    }
}