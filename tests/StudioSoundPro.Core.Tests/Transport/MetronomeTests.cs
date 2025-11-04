using FluentAssertions;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Tests.Transport;

public class MetronomeTests
{
    private readonly IClock _clock;

    public MetronomeTests()
    {
        _clock = new Clock(48000);
        _clock.Tempo = 120.0; // 2 beats per second for easy testing
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var metronome = new Metronome();

        // Assert
        metronome.IsEnabled.Should().BeFalse();
        metronome.PlayDuringPlayback.Should().BeTrue();
        metronome.PlayDuringRecording.Should().BeTrue();
        metronome.Volume.Should().Be(0.5f);
        metronome.AccentFirstBeat.Should().BeTrue();
        metronome.DownbeatFrequency.Should().Be(1000.0f);
        metronome.BeatFrequency.Should().Be(800.0f);
        metronome.ClickDurationMs.Should().Be(50);
    }

    [Fact]
    public void IsEnabled_SetValue_FiresSettingsChangedEvent()
    {
        // Arrange
        var metronome = new Metronome();
        bool eventFired = false;
        metronome.SettingsChanged += (_, _) => eventFired = true;

        // Act
        metronome.IsEnabled = true;

        // Assert
        metronome.IsEnabled.Should().BeTrue();
        eventFired.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Volume_SetValidValue_UpdatesProperty(float volume)
    {
        // Arrange
        var metronome = new Metronome();

        // Act
        metronome.Volume = volume;

        // Assert
        metronome.Volume.Should().Be(volume);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(2.0f)]
    public void Volume_SetInvalidValue_ThrowsException(float volume)
    {
        // Arrange
        var metronome = new Metronome();

        // Act & Assert
        var action = () => metronome.Volume = volume;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(100.0f)]
    [InlineData(440.0f)]
    [InlineData(1000.0f)]
    public void DownbeatFrequency_SetValidValue_UpdatesProperty(float frequency)
    {
        // Arrange
        var metronome = new Metronome();

        // Act
        metronome.DownbeatFrequency = frequency;

        // Assert
        metronome.DownbeatFrequency.Should().Be(frequency);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-100.0f)]
    public void DownbeatFrequency_SetInvalidValue_ThrowsException(float frequency)
    {
        // Arrange
        var metronome = new Metronome();

        // Act & Assert
        var action = () => metronome.DownbeatFrequency = frequency;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(100.0f)]
    [InlineData(440.0f)]
    [InlineData(1000.0f)]
    public void BeatFrequency_SetValidValue_UpdatesProperty(float frequency)
    {
        // Arrange
        var metronome = new Metronome();

        // Act
        metronome.BeatFrequency = frequency;

        // Assert
        metronome.BeatFrequency.Should().Be(frequency);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-100.0f)]
    public void BeatFrequency_SetInvalidValue_ThrowsException(float frequency)
    {
        // Arrange
        var metronome = new Metronome();

        // Act & Assert
        var action = () => metronome.BeatFrequency = frequency;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void ClickDurationMs_SetValidValue_UpdatesProperty(int duration)
    {
        // Arrange
        var metronome = new Metronome();

        // Act
        metronome.ClickDurationMs = duration;

        // Assert
        metronome.ClickDurationMs.Should().Be(duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ClickDurationMs_SetInvalidValue_ThrowsException(int duration)
    {
        // Arrange
        var metronome = new Metronome();

        // Act & Assert
        var action = () => metronome.ClickDurationMs = duration;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProcessBuffer_DisabledMetronome_DoesNotModifyBuffer()
    {
        // Arrange
        var metronome = new Metronome();
        metronome.IsEnabled = false;
        var buffer = new float[1024];
        var originalBuffer = new float[1024];

        // Act
        metronome.ProcessBuffer(buffer, 1024, 0, _clock);

        // Assert
        buffer.Should().BeEquivalentTo(originalBuffer);
    }

    [Fact]
    public void ProcessBuffer_EnabledMetronome_ModifiesBufferOnBeatBoundary()
    {
        // Arrange
        var metronome = new Metronome();
        metronome.IsEnabled = true;
        metronome.Volume = 0.5f;
        var buffer = new float[1024];
        var originalBuffer = new float[1024];

        // Act - Process buffer starting at beat boundary (sample 0)
        metronome.ProcessBuffer(buffer, 1024, 0, _clock);

        // Assert
        buffer.Should().NotBeEquivalentTo(originalBuffer);
        // Should have some non-zero values due to click generation
        buffer.Should().Contain(sample => Math.Abs(sample) > 0.001f);
    }

    [Fact]
    public void ProcessBuffer_ZeroVolume_DoesNotModifyBuffer()
    {
        // Arrange
        var metronome = new Metronome();
        metronome.IsEnabled = true;
        metronome.Volume = 0.0f;
        var buffer = new float[1024];
        var originalBuffer = new float[1024];

        // Act
        metronome.ProcessBuffer(buffer, 1024, 0, _clock);

        // Assert
        buffer.Should().BeEquivalentTo(originalBuffer);
    }

    [Fact]
    public void ProcessBuffer_ZeroSampleCount_DoesNotModifyBuffer()
    {
        // Arrange
        var metronome = new Metronome();
        metronome.IsEnabled = true;
        var buffer = new float[1024];
        var originalBuffer = new float[1024];

        // Act
        metronome.ProcessBuffer(buffer, 0, 0, _clock);

        // Assert
        buffer.Should().BeEquivalentTo(originalBuffer);
    }

    [Fact]
    public void AllProperties_FireSettingsChangedEvent()
    {
        // Arrange
        var metronome = new Metronome();
        var eventCount = 0;
        metronome.SettingsChanged += (_, _) => eventCount++;

        // Act - Change all properties
        metronome.IsEnabled = true;
        metronome.PlayDuringPlayback = false;
        metronome.PlayDuringRecording = false;
        metronome.Volume = 0.8f;
        metronome.AccentFirstBeat = false;
        metronome.DownbeatFrequency = 880.0f;
        metronome.BeatFrequency = 660.0f;
        metronome.ClickDurationMs = 75;

        // Assert
        eventCount.Should().Be(8); // One event per property change
    }

    [Fact]
    public void ProcessBuffer_ClickGeneration_ProducesExpectedWaveform()
    {
        // Arrange
        var metronome = new Metronome();
        metronome.IsEnabled = true;
        metronome.Volume = 1.0f;
        metronome.BeatFrequency = 1000.0f; // 1kHz for predictable waveform
        metronome.ClickDurationMs = 10; // Short click for testing

        var sampleRate = _clock.SampleRate;
        var clickSamples = 10 * sampleRate / 1000; // 480 samples for 10ms at 48kHz
        var buffer = new float[clickSamples];

        // Act - Process at beat boundary
        metronome.ProcessBuffer(buffer, clickSamples, 0, _clock);

        // Assert
        // Should have generated a sine wave click
        buffer.Should().Contain(sample => Math.Abs(sample) > 0.1f);
        
        // The waveform should start with non-zero amplitude (attack phase)
        Math.Abs(buffer[10]).Should().BeGreaterThan(0.01f); // After initial attack
        
        // Should decay towards the end
        Math.Abs(buffer[^10]).Should().BeLessThan(Math.Abs(buffer[50]));
    }
}