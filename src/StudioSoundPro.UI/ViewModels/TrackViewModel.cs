using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// ViewModel for a single audio track with clips
/// </summary>
public partial class TrackViewModel : ObservableObject, IDisposable
{
    private readonly ITrack _track;
    private bool _disposed;

    public TrackViewModel(ITrack track)
    {
        _track = track ?? throw new ArgumentNullException(nameof(track));
        
        // Subscribe to track events
        _track.PropertyChanged += OnTrackPropertyChanged;
        _track.ClipAdded += OnClipAdded;
        _track.ClipRemoved += OnClipRemoved;
        
        // Initialize clip collection
        Clips = new ObservableCollection<ClipViewModel>();
        foreach (var clip in _track.Clips)
        {
            Clips.Add(new ClipViewModel(clip));
        }
    }

    /// <summary>Gets the underlying track model</summary>
    public ITrack Track => _track;

    /// <summary>Gets the unique track identifier</summary>
    public Guid Id => _track.Id;

    /// <summary>Gets or sets the track name</summary>
    public string Name
    {
        get => _track.Name;
        set
        {
            if (_track.Name != value)
            {
                _track.Name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the track color for visual representation</summary>
    public string Color
    {
        get => _track.Color;
        set
        {
            if (_track.Color != value)
            {
                _track.Color = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the track is muted</summary>
    public bool IsMuted
    {
        get => _track.IsMuted;
        set
        {
            if (_track.IsMuted != value)
            {
                _track.IsMuted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the track is soloed</summary>
    public bool IsSolo
    {
        get => _track.IsSolo;
        set
        {
            if (_track.IsSolo != value)
            {
                _track.IsSolo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the track is armed for recording</summary>
    public bool IsArmed
    {
        get => _track.IsArmed;
        set
        {
            if (_track.IsArmed != value)
            {
                _track.IsArmed = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets the track volume (0.0 to 1.0)</summary>
    public float Volume
    {
        get => _track.Volume;
        set
        {
            if (Math.Abs(_track.Volume - value) > 0.0001f)
            {
                _track.Volume = Math.Clamp(value, 0.0f, 2.0f);
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumeDb));
            }
        }
    }

    /// <summary>Gets the volume in decibels for display</summary>
    public double VolumeDb
    {
        get
        {
            if (Volume < 0.0001f)
                return -80.0;
            return 20.0 * Math.Log10(Volume);
        }
    }

    /// <summary>Gets or sets the track pan (-1.0 to 1.0)</summary>
    public float Pan
    {
        get => _track.Pan;
        set
        {
            if (Math.Abs(_track.Pan - value) > 0.0001f)
            {
                _track.Pan = Math.Clamp(value, -1.0f, 1.0f);
                OnPropertyChanged();
                OnPropertyChanged(nameof(PanPercent));
            }
        }
    }

    /// <summary>Gets the pan as percentage for display (0-100)</summary>
    public double PanPercent
    {
        get
        {
            if (Pan < -0.01f)
                return (Pan + 1.0) * 50.0; // 0-50 for left
            if (Pan > 0.01f)
                return 50.0 + Pan * 50.0; // 50-100 for right
            return 50.0; // Center
        }
    }

    /// <summary>Gets the pan direction for display</summary>
    public string PanLabel
    {
        get
        {
            if (Pan < -0.01f)
                return $"L{Math.Abs(Pan * 100):F0}";
            if (Pan > 0.01f)
                return $"R{Pan * 100:F0}";
            return "C";
        }
    }

    /// <summary>Gets the collection of clips on this track</summary>
    public ObservableCollection<ClipViewModel> Clips { get; }

    /// <summary>Gets the underlying track model</summary>
    public ITrack Model => _track;

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    [RelayCommand]
    private void ToggleSolo()
    {
        IsSolo = !IsSolo;
    }

    [RelayCommand]
    private void ToggleArm()
    {
        IsArmed = !IsArmed;
    }

    [RelayCommand]
    private void ResetVolume()
    {
        Volume = 1.0f;
    }

    [RelayCommand]
    private void ResetPan()
    {
        Pan = 0.0f;
    }

    [RelayCommand]
    private void DeleteTrack()
    {
        // To be handled by parent SessionViewModel
        TrackDeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Event raised when track deletion is requested</summary>
    public event EventHandler? TrackDeleteRequested;

    private void OnTrackPropertyChanged(object? sender, TrackPropertyChangedEventArgs e)
    {
        // Forward property changes to UI
        switch (e.PropertyName)
        {
            case nameof(ITrack.Name):
                OnPropertyChanged(nameof(Name));
                break;
            case nameof(ITrack.Color):
                OnPropertyChanged(nameof(Color));
                break;
            case nameof(ITrack.IsMuted):
                OnPropertyChanged(nameof(IsMuted));
                break;
            case nameof(ITrack.IsSolo):
                OnPropertyChanged(nameof(IsSolo));
                break;
            case nameof(ITrack.IsArmed):
                OnPropertyChanged(nameof(IsArmed));
                break;
            case nameof(ITrack.Volume):
                OnPropertyChanged(nameof(Volume));
                OnPropertyChanged(nameof(VolumeDb));
                break;
            case nameof(ITrack.Pan):
                OnPropertyChanged(nameof(Pan));
                OnPropertyChanged(nameof(PanPercent));
                OnPropertyChanged(nameof(PanLabel));
                break;
        }
    }

    private void OnClipAdded(object? sender, ClipEventArgs e)
    {
        if (e.Clip != null)
        {
            Clips.Add(new ClipViewModel(e.Clip));
        }
    }

    private void OnClipRemoved(object? sender, ClipEventArgs e)
    {
        if (e.Clip != null)
        {
            var clipVm = Clips.FirstOrDefault(c => c.Id == e.Clip.Id);
            if (clipVm != null)
            {
                Clips.Remove(clipVm);
                clipVm.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe from track events
        _track.PropertyChanged -= OnTrackPropertyChanged;
        _track.ClipAdded -= OnClipAdded;
        _track.ClipRemoved -= OnClipRemoved;

        // Dispose clip view models
        foreach (var clip in Clips)
        {
            clip.Dispose();
        }
        Clips.Clear();

        // Dispose track
        _track.Dispose();
    }
}
