using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// ViewModel for a multi-track session
/// </summary>
public partial class SessionViewModel : ObservableObject, IDisposable
{
    private bool _disposed;

    public SessionViewModel()
    {
        Tracks = new ObservableCollection<TrackViewModel>();
    }

    /// <summary>Gets the collection of tracks in this session</summary>
    public ObservableCollection<TrackViewModel> Tracks { get; }

    [RelayCommand]
    private void AddTrack()
    {
        var track = new Track($"Track {Tracks.Count + 1}")
        {
            Color = GetNextTrackColor()
        };
        
        var trackVm = new TrackViewModel(track);
        trackVm.TrackDeleteRequested += OnTrackDeleteRequested;
        Tracks.Add(trackVm);
    }

    [RelayCommand]
    private void AddAudioTrack()
    {
        var track = new Track($"Audio {Tracks.Count + 1}")
        {
            Color = "#4A90E2" // Blue for audio tracks
        };
        
        var trackVm = new TrackViewModel(track);
        trackVm.TrackDeleteRequested += OnTrackDeleteRequested;
        Tracks.Add(trackVm);
    }

    [RelayCommand]
    private void ClearAllTracks()
    {
        foreach (var track in Tracks)
        {
            track.TrackDeleteRequested -= OnTrackDeleteRequested;
            track.Dispose();
        }
        Tracks.Clear();
    }

    private void OnTrackDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is TrackViewModel trackVm)
        {
            trackVm.TrackDeleteRequested -= OnTrackDeleteRequested;
            Tracks.Remove(trackVm);
            trackVm.Dispose();
        }
    }

    private string GetNextTrackColor()
    {
        // Cycle through a palette of colors
        string[] colors = 
        {
            "#4A90E2", // Blue
            "#50C878", // Green
            "#FFA500", // Orange
            "#9B59B6", // Purple
            "#E74C3C", // Red
            "#1ABC9C", // Teal
            "#F39C12", // Yellow-Orange
            "#34495E"  // Dark Gray
        };
        
        return colors[Tracks.Count % colors.Length];
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var track in Tracks)
        {
            track.TrackDeleteRequested -= OnTrackDeleteRequested;
            track.Dispose();
        }
        Tracks.Clear();
    }
}
