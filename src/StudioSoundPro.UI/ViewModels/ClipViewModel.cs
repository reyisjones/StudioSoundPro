using System;
using CommunityToolkit.Mvvm.ComponentModel;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// ViewModel for a single audio or MIDI clip
/// </summary>
public partial class ClipViewModel : ObservableObject, IDisposable
{
    private readonly IClip _clip;
    private bool _disposed;

    public ClipViewModel(IClip clip)
    {
        _clip = clip ?? throw new ArgumentNullException(nameof(clip));
        _clip.PropertyChanged += OnClipPropertyChanged;
    }

    /// <summary>Gets the unique clip identifier</summary>
    public Guid Id => _clip.Id;

    /// <summary>Gets or sets the clip name</summary>
    public string Name
    {
        get => _clip.Name;
        set
        {
            if (_clip.Name != value)
            {
                _clip.Name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the start position in samples</summary>
    public long StartPosition
    {
        get => _clip.StartPosition;
        set
        {
            if (_clip.StartPosition != value)
            {
                _clip.StartPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndPosition));
            }
        }
    }

    /// <summary>Gets or sets the length in samples</summary>
    public long Length
    {
        get => _clip.Length;
        set
        {
            if (_clip.Length != value)
            {
                _clip.Length = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndPosition));
            }
        }
    }

    /// <summary>Gets the end position in samples</summary>
    public long EndPosition => _clip.EndPosition;

    /// <summary>Gets or sets the source offset in samples</summary>
    public long SourceOffset
    {
        get => _clip.SourceOffset;
        set
        {
            if (_clip.SourceOffset != value)
            {
                _clip.SourceOffset = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the clip is muted</summary>
    public bool IsMuted
    {
        get => _clip.IsMuted;
        set
        {
            if (_clip.IsMuted != value)
            {
                _clip.IsMuted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the clip gain (0.0 to 2.0)</summary>
    public float Gain
    {
        get => _clip.Gain;
        set
        {
            if (Math.Abs(_clip.Gain - value) > 0.0001f)
            {
                _clip.Gain = Math.Clamp(value, 0.0f, 2.0f);
                OnPropertyChanged();
                OnPropertyChanged(nameof(GainDb));
            }
        }
    }

    /// <summary>Gets the gain in decibels for display</summary>
    public double GainDb
    {
        get
        {
            if (Gain < 0.0001f)
                return -80.0;
            return 20.0 * Math.Log10(Gain);
        }
    }

    /// <summary>Gets or sets the fade-in length in samples</summary>
    public long FadeInLength
    {
        get => _clip.FadeInLength;
        set
        {
            if (_clip.FadeInLength != value)
            {
                _clip.FadeInLength = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the fade-out length in samples</summary>
    public long FadeOutLength
    {
        get => _clip.FadeOutLength;
        set
        {
            if (_clip.FadeOutLength != value)
            {
                _clip.FadeOutLength = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the clip color</summary>
    public string Color
    {
        get => _clip.Color;
        set
        {
            if (_clip.Color != value)
            {
                _clip.Color = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets whether this is an audio clip</summary>
    public bool IsAudioClip => _clip is IAudioClip;

    /// <summary>Gets the audio clip if this is an audio clip</summary>
    public IAudioClip? AudioClip => _clip as IAudioClip;

    /// <summary>Gets the underlying clip model</summary>
    public IClip Model => _clip;

    private void OnClipPropertyChanged(object? sender, ClipPropertyChangedEventArgs e)
    {
        // Forward property changes to UI
        switch (e.PropertyName)
        {
            case nameof(IClip.Name):
                OnPropertyChanged(nameof(Name));
                break;
            case nameof(IClip.StartPosition):
                OnPropertyChanged(nameof(StartPosition));
                OnPropertyChanged(nameof(EndPosition));
                break;
            case nameof(IClip.Length):
                OnPropertyChanged(nameof(Length));
                OnPropertyChanged(nameof(EndPosition));
                break;
            case nameof(IClip.SourceOffset):
                OnPropertyChanged(nameof(SourceOffset));
                break;
            case nameof(IClip.IsMuted):
                OnPropertyChanged(nameof(IsMuted));
                break;
            case nameof(IClip.Gain):
                OnPropertyChanged(nameof(Gain));
                OnPropertyChanged(nameof(GainDb));
                break;
            case nameof(IClip.FadeInLength):
                OnPropertyChanged(nameof(FadeInLength));
                break;
            case nameof(IClip.FadeOutLength):
                OnPropertyChanged(nameof(FadeOutLength));
                break;
            case nameof(IClip.Color):
                OnPropertyChanged(nameof(Color));
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _clip.PropertyChanged -= OnClipPropertyChanged;
    }
}
