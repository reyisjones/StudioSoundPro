using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to StudioSoundPro!";
    
    public AudioTestViewModel AudioTest { get; }
    public TransportViewModel Transport { get; }

    public MainWindowViewModel()
    {
        AudioTest = new AudioTestViewModel();
        
        // Create transport system
        var clock = new Clock(48000); // 48kHz sample rate
        var transport = new StudioSoundPro.Core.Transport.Transport(clock);
        Transport = new TransportViewModel(transport);
    }
}
