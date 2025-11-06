using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// ViewModel for the mixer engine controls
/// </summary>
public partial class MixerViewModel : ObservableObject, IDisposable
{
    private readonly IMixerEngine _mixer;
    private bool _disposed;

    public MixerViewModel(IMixerEngine mixer)
    {
        _mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
    }

    /// <summary>
    /// Gets the mixer engine
    /// </summary>
    public IMixerEngine Mixer => _mixer;

    /// <summary>
    /// Gets the transport system
    /// </summary>
    public ITransport Transport => _mixer.Transport;

    /// <summary>
    /// Gets or sets the master volume (0.0 to 2.0)
    /// </summary>
    public float MasterVolume
    {
        get => _mixer.MasterVolume;
        set
        {
            if (Math.Abs(_mixer.MasterVolume - value) > 0.001f)
            {
                _mixer.MasterVolume = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MasterVolumeDb));
            }
        }
    }

    /// <summary>
    /// Gets the master volume in dB
    /// </summary>
    public string MasterVolumeDb
    {
        get
        {
            if (_mixer.MasterVolume <= 0.0f)
                return "-∞ dB";
            
            double db = 20.0 * Math.Log10(_mixer.MasterVolume);
            return $"{db:F1} dB";
        }
    }

    /// <summary>
    /// Gets or sets whether the master output is muted
    /// </summary>
    public bool IsMasterMuted
    {
        get => _mixer.IsMasterMuted;
        set
        {
            if (_mixer.IsMasterMuted != value)
            {
                _mixer.IsMasterMuted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Resets master volume to unity gain (0 dB)
    /// </summary>
    [RelayCommand]
    private void ResetMasterVolume()
    {
        MasterVolume = 1.0f;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        // Note: Don't dispose the mixer here - it's owned by the parent
    }
}
