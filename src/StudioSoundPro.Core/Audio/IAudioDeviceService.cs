namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Service for discovering and managing audio devices.
/// </summary>
public interface IAudioDeviceService
{
    /// <summary>
    /// Gets all available audio devices.
    /// </summary>
    /// <returns>A collection of available audio devices.</returns>
    IEnumerable<IAudioDevice> GetAvailableDevices();

    /// <summary>
    /// Gets the default output device.
    /// </summary>
    /// <returns>The default output device, or null if none available.</returns>
    IAudioDevice? GetDefaultOutputDevice();

    /// <summary>
    /// Gets the default input device.
    /// </summary>
    /// <returns>The default input device, or null if none available.</returns>
    IAudioDevice? GetDefaultInputDevice();

    /// <summary>
    /// Gets an audio device by its ID.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>The audio device, or null if not found.</returns>
    IAudioDevice? GetDeviceById(int deviceId);

    /// <summary>
    /// Initializes the audio device service.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Releases resources used by the audio device service.
    /// </summary>
    void Dispose();
}