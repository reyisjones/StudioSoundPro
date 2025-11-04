using FluentAssertions;
using StudioSoundPro.AudioIO.PortAudio;
using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.AudioIO.Tests.PortAudio;

public class PortAudioDeviceServiceTests
{
    [Fact]
    public void AudioDeviceService_ListDevices_returns_at_least_one_output()
    {
        // Arrange
        var service = new PortAudioDeviceService();

        try
        {
            // Act
            service.Initialize();
            var devices = service.GetAvailableDevices();

            // Assert
            devices.Should().NotBeEmpty();
            devices.Should().Contain(device => device.MaxOutputChannels > 0);
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }

    [Fact]
    public void AudioDeviceService_GetDefaultOutputDevice_returns_valid_device()
    {
        // Arrange
        var service = new PortAudioDeviceService();

        try
        {
            // Act
            service.Initialize();
            var defaultDevice = service.GetDefaultOutputDevice();

            // Assert
            defaultDevice.Should().NotBeNull();
            defaultDevice!.MaxOutputChannels.Should().BeGreaterThan(0);
            defaultDevice.IsDefaultOutput.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            service.Dispose();
        }
    }
}

public class PortAudioEngineTests
{
    [Fact]
    public void AudioEngine_StartStop_changes_state_and_invokes_callback()
    {
        // Arrange
        var deviceService = new PortAudioDeviceService();
        var engine = new PortAudioEngine();
        var callbackInvoked = false;
        var stateChanges = new List<AudioEngineState>();

        try
        {
            deviceService.Initialize();
            var outputDevice = deviceService.GetDefaultOutputDevice();
            outputDevice.Should().NotBeNull();

            engine.StateChanged += (sender, state) => stateChanges.Add(state);
            engine.Initialize(outputDevice!, 48000, 512, 2);

            AudioCallback callback = (input, output, frameCount, channels) =>
            {
                callbackInvoked = true;
                // Fill with silence for testing
                Array.Fill(output, 0.0f);
                return true;
            };

            // Act
            engine.Start(callback);
            
            // Give some time for the callback to be invoked
            Thread.Sleep(100);
            
            engine.Stop();

            // Assert
            engine.State.Should().Be(AudioEngineState.Stopped);
            stateChanges.Should().Contain(AudioEngineState.Starting);
            stateChanges.Should().Contain(AudioEngineState.Running);
            stateChanges.Should().Contain(AudioEngineState.Stopping);
            callbackInvoked.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            engine.Dispose();
            deviceService.Dispose();
        }
    }

    [Fact]
    public void AudioEngine_requires_initialization_before_start()
    {
        // Arrange
        var engine = new PortAudioEngine();

        // Act & Assert
        var action = () => engine.Start((input, output, frameCount, channels) => true);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be initialized*");

        engine.Dispose();
    }
}
