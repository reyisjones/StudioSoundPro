using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to StudioSoundPro!";
    
    public AudioTestViewModel AudioTest { get; }
    public TransportViewModel Transport { get; }
    public SessionViewModel Session { get; }
    public MixerViewModel Mixer { get; }
    
    private readonly IMixerEngine _mixerEngine;

    public MainWindowViewModel()
    {
        AudioTest = new AudioTestViewModel();
        
        // Create transport system
        var clock = new Clock(48000); // 48kHz sample rate
        var transport = new StudioSoundPro.Core.Transport.Transport(clock);
        Transport = new TransportViewModel(transport);
        
        // Create mixer engine (integrates transport with track mixing)
        _mixerEngine = new MixerEngine(transport, 48000, 2); // Stereo, 48kHz
        Mixer = new MixerViewModel(_mixerEngine);
        
        // Create session (tracks will be added to the mixer)
        Session = new SessionViewModel();
        
        // Wire up session tracks to mixer
        Session.Tracks.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (TrackViewModel trackVm in e.NewItems)
                {
                    _mixerEngine.AddTrack(trackVm.Track);
                }
            }
            if (e.OldItems != null)
            {
                foreach (TrackViewModel trackVm in e.OldItems)
                {
                    _mixerEngine.RemoveTrack(trackVm.Track);
                }
            }
        };
    }
}

