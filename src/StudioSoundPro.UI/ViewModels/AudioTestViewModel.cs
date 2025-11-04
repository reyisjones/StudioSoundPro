using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.AudioIO.PortAudio;

namespace StudioSoundPro.UI.ViewModels;

public partial class AudioTestViewModel : ViewModelBase
{
    private readonly PortAudioDeviceService _deviceService;
    private readonly PortAudioEngine _audioEngine;
    private readonly SineGenerator _sineGenerator;

    [ObservableProperty]
    private ObservableCollection<IAudioDevice> _availableDevices = new();

    [ObservableProperty]
    private IAudioDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isPlaying = false;

    [ObservableProperty]
    private double _frequency = 440.0;

    [ObservableProperty]
    private double _amplitudeDb = -12.0;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _outputLevel = 0.0;

    public AudioTestViewModel()
    {
        _deviceService = new PortAudioDeviceService();
        _audioEngine = new PortAudioEngine();
        _sineGenerator = new SineGenerator();

        // Subscribe to engine events
        _audioEngine.StateChanged += OnAudioEngineStateChanged;
        _audioEngine.ProcessingError += OnAudioEngineError;

        InitializeAudio();
    }

    private void InitializeAudio()
    {
        try
        {
            _deviceService.Initialize();
            var devices = _deviceService.GetAvailableDevices();
            
            AvailableDevices.Clear();
            foreach (var device in devices)
            {
                if (device.MaxOutputChannels > 0)
                {
                    AvailableDevices.Add(device);
                }
            }

            SelectedDevice = _deviceService.GetDefaultOutputDevice();
            _sineGenerator.Initialize(48000);
            _sineGenerator.Frequency = Frequency;
            _sineGenerator.SetAmplitudeDb(AmplitudeDb);

            StatusMessage = $"Initialized with {AvailableDevices.Count} output devices";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Initialization error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartAudio()
    {
        if (SelectedDevice == null)
        {
            StatusMessage = "No output device selected";
            return;
        }

        try
        {
            _audioEngine.Initialize(SelectedDevice, 48000, 512, 2);
            _sineGenerator.IsEnabled = true;

            _audioEngine.Start((input, output, frameCount, channelCount) =>
            {
                // Generate audio
                _sineGenerator.GenerateAudio(output, frameCount, channelCount);

                // Calculate output level (simple peak detection)
                var maxLevel = 0.0f;
                for (int i = 0; i < output.Length; i++)
                {
                    maxLevel = System.Math.Max(maxLevel, System.Math.Abs(output[i]));
                }

                // Update output level on UI thread
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OutputLevel = maxLevel;
                });

                return true; // Continue processing
            });

            StatusMessage = $"Playing {Frequency}Hz sine tone at {AmplitudeDb:F1}dB on {SelectedDevice.Name}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Start error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StopAudio()
    {
        try
        {
            _audioEngine.Stop();
            _sineGenerator.IsEnabled = false;
            OutputLevel = 0.0;
            StatusMessage = "Stopped";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Stop error: {ex.Message}";
        }
    }

    partial void OnFrequencyChanged(double value)
    {
        _sineGenerator.Frequency = value;
    }

    partial void OnAmplitudeDbChanged(double value)
    {
        _sineGenerator.SetAmplitudeDb(value);
    }

    private void OnAudioEngineStateChanged(object? sender, AudioEngineState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsPlaying = state == AudioEngineState.Running;
        });
    }

    private void OnAudioEngineError(object? sender, string error)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = $"Audio error: {error}";
            IsPlaying = false;
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _audioEngine.Dispose();
            _deviceService.Dispose();
        }
        base.Dispose(disposing);
    }
}