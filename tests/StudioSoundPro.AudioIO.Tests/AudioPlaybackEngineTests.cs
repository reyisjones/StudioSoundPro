using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using StudioSoundPro.AudioIO;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.AudioIO.Tests;

public class AudioPlaybackEngineTests : IDisposable
{
    private readonly TestAudioDevice _device;
    private readonly Clock _clock;
    private readonly ITransport _transport;
    private readonly MixerEngine _mixer;
    private readonly TestAudioEngine _audioEngine;
    private readonly AudioPlaybackEngine _playbackEngine;

    public AudioPlaybackEngineTests()
    {
        _device = new TestAudioDevice();
        _clock = new Clock(48000);
        _transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        _mixer = new MixerEngine(_transport, 48000, 2);
        _audioEngine = new TestAudioEngine();
        _playbackEngine = new AudioPlaybackEngine(_device, _mixer, _audioEngine);
    }

    public void Dispose()
    {
        _playbackEngine?.Dispose();
        _mixer?.Dispose();
        _transport?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_Initializes()
    {
        _playbackEngine.IsRunning.Should().BeFalse();
        _playbackEngine.Mixer.Should().BeSameAs(_mixer);
        _playbackEngine.Transport.Should().BeSameAs(_transport);
    }

    [Fact]
    public void Constructor_WithNullDevice_ThrowsArgumentNullException()
    {
        Action act = () => new AudioPlaybackEngine(null!, _mixer, _audioEngine);
        act.Should().Throw<ArgumentNullException>().WithParameterName("device");
    }

    [Fact]
    public void Constructor_WithNullMixer_ThrowsArgumentNullException()
    {
        Action act = () => new AudioPlaybackEngine(_device, null!, _audioEngine);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mixer");
    }

    [Fact]
    public void Constructor_WithNullAudioEngine_ThrowsArgumentNullException()
    {
        Action act = () => new AudioPlaybackEngine(_device, _mixer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("audioEngine");
    }

    [Fact]
    public void Start_StartsAudioEngine()
    {
        _playbackEngine.Start();

        _playbackEngine.IsRunning.Should().BeTrue();
        _audioEngine.IsStarted.Should().BeTrue();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_DoesNothing()
    {
        _playbackEngine.Start();
        int startCount = _audioEngine.StartCallCount;

        _playbackEngine.Start(); // Call again

        _audioEngine.StartCallCount.Should().Be(startCount); // Should not increase
    }

    [Fact]
    public void Stop_StopsAudioEngine()
    {
        _playbackEngine.Start();
        _playbackEngine.Stop();

        _playbackEngine.IsRunning.Should().BeFalse();
        _audioEngine.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void Stop_WhenNotRunning_DoesNothing()
    {
        int stopCount = _audioEngine.StopCallCount;
        
        _playbackEngine.Stop();

        _audioEngine.StopCallCount.Should().Be(stopCount); // Should not increase
    }

    [Fact]
    public void Dispose_StopsPlayback()
    {
        _playbackEngine.Start();
        _playbackEngine.Dispose();

        _playbackEngine.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _playbackEngine.Dispose();
        _playbackEngine.Dispose(); // Should not throw
    }
}

/// <summary>
/// Mock audio engine for testing
/// </summary>
internal class TestAudioEngine : IAudioEngine
{
    private AudioCallback? _callback;

    public AudioEngineState State { get; private set; } = AudioEngineState.Stopped;
    public double SampleRate => 48000;
    public int BufferSize => 512;
    public int OutputChannels => 2;

    public bool IsStarted => State == AudioEngineState.Running;
    public bool IsStopped => State == AudioEngineState.Stopped;
    public int StartCallCount { get; private set; }
    public int StopCallCount { get; private set; }

    public event EventHandler<AudioEngineState>? StateChanged;
    public event EventHandler<string>? ProcessingError;

    public void Initialize(IAudioDevice outputDevice, double sampleRate = 48000, int bufferSize = 512, int channels = 2)
    {
        // Mock implementation
    }

    public void Start(AudioCallback callback)
    {
        _callback = callback;
        State = AudioEngineState.Running;
        StartCallCount++;
        StateChanged?.Invoke(this, State);
    }

    public void Stop()
    {
        State = AudioEngineState.Stopped;
        StopCallCount++;
        StateChanged?.Invoke(this, State);
    }

    /// <summary>
    /// Simulates calling the audio callback
    /// </summary>
    public bool InvokeCallback(float[]? input, float[] output, int frameCount, int channels)
    {
        return _callback?.Invoke(input, output, frameCount, channels) ?? false;
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// Test audio device for testing
/// </summary>
internal class TestAudioDevice : IAudioDevice
{
    public int Id => 0;
    public string Name => "Test Device";
    public int MaxInputChannels => 0;
    public int MaxOutputChannels => 2;
    public double DefaultSampleRate => 48000;
    public bool IsDefaultInput => false;
    public bool IsDefaultOutput => true;
}
