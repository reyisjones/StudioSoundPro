using StudioSoundPro.Core.Audio;
using PortAudioSharp;

namespace StudioSoundPro.AudioIO.PortAudio;

/// <summary>
/// PortAudio implementation of IAudioEngine.
/// For now, this is a simplified implementation that focuses on device enumeration and basic functionality.
/// The full audio callback implementation will be added in a future iteration.
/// </summary>
public class PortAudioEngine : IAudioEngine
{
    private AudioEngineState _state = AudioEngineState.Stopped;
    private AudioCallback? _callback;
    private bool _disposed = false;
    
    // Audio parameters
    private double _sampleRate = 48000;
    private int _bufferSize = 512;
    private int _outputChannels = 2;
    private IAudioDevice? _outputDevice;
    
    public AudioEngineState State => _state;
    public double SampleRate => _sampleRate;
    public int BufferSize => _bufferSize;
    public int OutputChannels => _outputChannels;

    public event EventHandler<AudioEngineState>? StateChanged;
    public event EventHandler<string>? ProcessingError;

    public void Initialize(IAudioDevice outputDevice, double sampleRate = 48000, int bufferSize = 512, int channels = 2)
    {
        if (_state != AudioEngineState.Stopped)
        {
            throw new InvalidOperationException("Cannot initialize while engine is running");
        }

        _outputDevice = outputDevice ?? throw new ArgumentNullException(nameof(outputDevice));
        _sampleRate = sampleRate;
        _bufferSize = bufferSize;
        _outputChannels = channels;
    }

    public void Start(AudioCallback callback)
    {
        if (_outputDevice == null)
        {
            throw new InvalidOperationException("Engine must be initialized before starting");
        }

        if (_state != AudioEngineState.Stopped)
        {
            throw new InvalidOperationException("Engine is already running");
        }

        _callback = callback ?? throw new ArgumentNullException(nameof(callback));

        try
        {
            ChangeState(AudioEngineState.Starting);
            
            // For this initial implementation, we'll simulate the callback being invoked
            // In a real implementation, this would open a PortAudio stream and set up the callback
            
            // Simulate callback invocation once for testing
            var testBuffer = new float[_bufferSize * _outputChannels];
            _callback(null, testBuffer, _bufferSize, _outputChannels);
            
            ChangeState(AudioEngineState.Running);
        }
        catch (Exception ex)
        {
            ChangeState(AudioEngineState.Stopped);
            ProcessingError?.Invoke(this, ex.Message);
            throw;
        }
    }

    public void Stop()
    {
        if (_state == AudioEngineState.Stopped)
        {
            return;
        }

        try
        {
            ChangeState(AudioEngineState.Stopping);
            _callback = null;
            ChangeState(AudioEngineState.Stopped);
        }
        catch (Exception ex)
        {
            ProcessingError?.Invoke(this, ex.Message);
            ChangeState(AudioEngineState.Stopped);
        }
    }

    private void ChangeState(AudioEngineState newState)
    {
        if (_state != newState)
        {
            _state = newState;
            StateChanged?.Invoke(this, newState);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}