using FluentAssertions;
using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.Core.Tests.Audio;

public class SineGeneratorTests
{
    [Fact]
    public void SineGenerator_produces_expected_frequency_and_level()
    {
        // Arrange
        var generator = new SineGenerator();
        var sampleRate = 48000.0;
        var frequency = 440.0;
        var expectedAmplitude = 0.25; // -12 dBFS approximately
        
        generator.Initialize(sampleRate);
        generator.Frequency = frequency;
        generator.SetAmplitudeDb(-12.0); // -12 dBFS
        generator.IsEnabled = true;

        // Act
        var buffer = new float[1024]; // 1024 samples for testing
        generator.GenerateAudio(buffer, 512, 2); // 512 frames, 2 channels

        // Assert
        generator.Frequency.Should().Be(frequency);
        generator.GetAmplitudeDb().Should().BeApproximately(-12.0, 0.1);
        
        // Check that audio was generated (not silence)
        buffer.Should().NotBeEmpty();
        buffer.Should().Contain(sample => Math.Abs(sample) > 0.01f);
        
        // Check amplitude is approximately correct
        var maxAmplitude = buffer.Max(sample => Math.Abs(sample));
        maxAmplitude.Should().BeApproximately((float)expectedAmplitude, 0.05f);
    }

    [Fact]
    public void SineGenerator_disabled_produces_silence()
    {
        // Arrange
        var generator = new SineGenerator();
        generator.Initialize(48000);
        generator.Frequency = 440.0;
        generator.Amplitude = 0.5;
        generator.IsEnabled = false; // Disabled

        // Act
        var buffer = new float[1024];
        generator.GenerateAudio(buffer, 512, 2);

        // Assert
        buffer.Should().AllSatisfy(sample => sample.Should().Be(0.0f));
    }

    [Fact]
    public void SineGenerator_reset_clears_phase()
    {
        // Arrange
        var generator = new SineGenerator();
        generator.Initialize(48000);
        generator.Frequency = 440.0;
        generator.Amplitude = 0.5;
        generator.IsEnabled = true;

        // Generate some audio to advance the phase
        var buffer = new float[2048];
        generator.GenerateAudio(buffer, 1024, 2);
        var firstSample = buffer[0];

        // Act
        generator.Reset();
        generator.GenerateAudio(buffer, 1024, 2);
        var resetSample = buffer[0];

        // Assert
        resetSample.Should().BeApproximately(0.0f, 0.001f); // Should start from zero phase
    }
}
