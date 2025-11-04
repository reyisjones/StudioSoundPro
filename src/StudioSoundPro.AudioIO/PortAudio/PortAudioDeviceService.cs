using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.AudioIO.PortAudio;

/// <summary>
/// Mock implementation of IAudioDeviceService for initial development.
/// This provides basic functionality to get the project building and tests passing.
/// A real PortAudio implementation will be added in a future iteration.
/// </summary>
public class PortAudioDeviceService : IAudioDeviceService, IDisposable
{
    private readonly List<PortAudioDevice> _devices = new();
    private bool _initialized = false;
    private bool _disposed = false;

    /// <summary>
    /// Initializes the mock device service.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            // Create mock devices for testing
            _devices.Add(new PortAudioDevice
            {
                Id = 0,
                Name = "Default Output Device",
                MaxInputChannels = 0,
                MaxOutputChannels = 2,
                DefaultSampleRate = 48000,
                IsDefaultInput = false,
                IsDefaultOutput = true
            });

            _devices.Add(new PortAudioDevice
            {
                Id = 1,
                Name = "Default Input Device",
                MaxInputChannels = 2,
                MaxOutputChannels = 0,
                DefaultSampleRate = 48000,
                IsDefaultInput = true,
                IsDefaultOutput = false
            });

            _devices.Add(new PortAudioDevice
            {
                Id = 2,
                Name = "Built-in Output",
                MaxInputChannels = 0,
                MaxOutputChannels = 2,
                DefaultSampleRate = 48000,
                IsDefaultInput = false,
                IsDefaultOutput = false
            });

            _initialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize audio device service", ex);
        }
    }

    /// <summary>
    /// Gets all available audio devices.
    /// </summary>
    public IEnumerable<IAudioDevice> GetAvailableDevices()
    {
        EnsureInitialized();
        return _devices.AsReadOnly();
    }

    /// <summary>
    /// Gets the default output device.
    /// </summary>
    public IAudioDevice? GetDefaultOutputDevice()
    {
        EnsureInitialized();
        return _devices.FirstOrDefault(d => d.IsDefaultOutput);
    }

    /// <summary>
    /// Gets the default input device.
    /// </summary>
    public IAudioDevice? GetDefaultInputDevice()
    {
        EnsureInitialized();
        return _devices.FirstOrDefault(d => d.IsDefaultInput);
    }

    /// <summary>
    /// Gets an audio device by its ID.
    /// </summary>
    public IAudioDevice? GetDeviceById(int deviceId)
    {
        EnsureInitialized();
        return _devices.FirstOrDefault(d => d.Id == deviceId);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("AudioDeviceService must be initialized before use.");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _initialized = false;
            _devices.Clear();
            _disposed = true;
        }
    }
}