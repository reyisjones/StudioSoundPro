using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Services;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// ViewModel for a multi-track session
/// </summary>
public partial class SessionViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private readonly AudioFileService _audioFileService;
    private IStorageProvider? _storageProvider;

    public SessionViewModel()
    {
        Tracks = new ObservableCollection<TrackViewModel>();
        _audioFileService = new AudioFileService(
            new WavFileReader(),
            new WavFileWriter()
        );
    }

    /// <summary>Sets the storage provider for file dialogs</summary>
    public void SetStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
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

    [RelayCommand]
    private async Task ImportAudioFile()
    {
        if (_storageProvider == null)
            return;

        // Show file picker
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Audio File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("WAV Files")
                {
                    Patterns = new[] { "*.wav" }
                }
            }
        });

        if (files.Count == 0)
            return;

        var file = files[0];
        var filePath = file.Path.LocalPath;

        try
        {
            // Import the audio file
            var clip = _audioFileService.ImportAudioFile(filePath);

            // Create or use selected track
            TrackViewModel? targetTrack = null;
            if (Tracks.Count > 0)
            {
                // TODO: Get selected track from UI
                // For now, use the first track or create new one
                targetTrack = Tracks[0];
            }
            else
            {
                var track = new Track("Audio Track 1") { Color = "#4A90E2" };
                targetTrack = new TrackViewModel(track);
                targetTrack.TrackDeleteRequested += OnTrackDeleteRequested;
                Tracks.Add(targetTrack);
            }

            // Add clip to track
            targetTrack.Track.AddClip(clip);
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error importing audio: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportTrack))]
    private async Task ExportTrack()
    {
        if (_storageProvider == null || Tracks.Count == 0)
            return;

        // TODO: Get selected track from UI
        // For now, export the first track
        var trackVm = Tracks[0];

        // Show save file dialog
        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Track",
            DefaultExtension = "wav",
            SuggestedFileName = $"{trackVm.Name}.wav",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("WAV Files")
                {
                    Patterns = new[] { "*.wav" }
                }
            }
        });

        if (file == null)
            return;

        var filePath = file.Path.LocalPath;

        try
        {
            // Export the track
            await _audioFileService.ExportTrackAsync(trackVm.Track, filePath);
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error exporting track: {ex.Message}");
        }
    }

    private bool CanExportTrack() => Tracks.Count > 0;

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
