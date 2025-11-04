using FluentAssertions;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Tests.Transport;

public class ClockTests
{
    private const int SampleRate = 48000;
    private const int TicksPerQuarterNote = 480;

    [Fact]
    public void Constructor_SetsInitialValues()
    {
        // Act
        var clock = new Clock(SampleRate, TicksPerQuarterNote);

        // Assert
        clock.SampleRate.Should().Be(SampleRate);
        clock.TicksPerQuarterNote.Should().Be(TicksPerQuarterNote);
        clock.Tempo.Should().Be(120.0);
        clock.TimeSignatureNumerator.Should().Be(4);
        clock.TimeSignatureDenominator.Should().Be(4);
    }

    [Fact]
    public void Tempo_SetValidValue_UpdatesAndFiresEvent()
    {
        // Arrange
        var clock = new Clock(SampleRate);
        double firedTempo = 0;
        clock.TempoChanged += (_, tempo) => firedTempo = tempo;

        // Act
        clock.Tempo = 140.0;

        // Assert
        clock.Tempo.Should().Be(140.0);
        firedTempo.Should().Be(140.0);
    }

    [Fact]
    public void Tempo_SetInvalidValue_ThrowsException()
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act & Assert
        clock.Invoking(c => c.Tempo = 0).Should().Throw<ArgumentOutOfRangeException>();
        clock.Invoking(c => c.Tempo = -10).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(48000, 1.0)] // 1 second = 48000 samples at 48kHz
    [InlineData(24000, 0.5)] // 0.5 seconds = 24000 samples at 48kHz
    [InlineData(96000, 2.0)] // 2 seconds = 96000 samples at 48kHz
    public void SamplesToSeconds_ConvertsCorrectly(long samples, double expectedSeconds)
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act
        var result = clock.SamplesToSeconds(samples);

        // Assert
        result.Should().BeApproximately(expectedSeconds, 0.001);
    }

    [Theory]
    [InlineData(1.0, 48000)] // 1 second = 48000 samples at 48kHz
    [InlineData(0.5, 24000)] // 0.5 seconds = 24000 samples at 48kHz
    [InlineData(2.0, 96000)] // 2 seconds = 96000 samples at 48kHz
    public void SecondsToSamples_ConvertsCorrectly(double seconds, long expectedSamples)
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act
        var result = clock.SecondsToSamples(seconds);

        // Assert
        result.Should().Be(expectedSamples);
    }

    [Fact]
    public void GetBeatLengthInSamples_120BPM_ReturnsCorrectLength()
    {
        // Arrange
        var clock = new Clock(SampleRate);
        clock.Tempo = 120.0; // 120 BPM = 2 beats per second = 0.5 seconds per beat

        // Act
        var beatLength = clock.GetBeatLengthInSamples();

        // Assert
        // At 120 BPM, one beat = 0.5 seconds = 24000 samples at 48kHz
        beatLength.Should().Be(24000);
    }

    [Fact]
    public void GetBarLengthInSamples_4_4Time_ReturnsCorrectLength()
    {
        // Arrange
        var clock = new Clock(SampleRate);
        clock.Tempo = 120.0;
        clock.TimeSignatureNumerator = 4;
        clock.TimeSignatureDenominator = 4;

        // Act
        var barLength = clock.GetBarLengthInSamples();

        // Assert
        // 4 beats per bar * 24000 samples per beat = 96000 samples
        barLength.Should().Be(96000);
    }

    [Theory]
    [InlineData(0, 1, 1, 0)]       // Start of song
    [InlineData(24000, 1, 2, 0)]   // Second beat of first bar
    [InlineData(96000, 2, 1, 0)]   // First beat of second bar
    [InlineData(120000, 2, 2, 0)]  // Second beat of second bar
    public void SamplesToMusicalTime_ConvertsCorrectly(long samples, int expectedBar, int expectedBeat, int expectedTick)
    {
        // Arrange
        var clock = new Clock(SampleRate);
        clock.Tempo = 120.0; // 2 beats per second, 0.5 seconds per beat

        // Act
        var (bar, beat, tick) = clock.SamplesToMusicalTime(samples);

        // Assert
        bar.Should().Be(expectedBar);
        beat.Should().Be(expectedBeat);
        tick.Should().Be(expectedTick);
    }

    [Theory]
    [InlineData(1, 1, 0, 0)]       // Start of song
    [InlineData(1, 2, 0, 24000)]   // Second beat of first bar
    [InlineData(2, 1, 0, 96000)]   // First beat of second bar
    [InlineData(2, 2, 0, 120000)]  // Second beat of second bar
    public void MusicalTimeToSamples_ConvertsCorrectly(int bar, int beat, int tick, long expectedSamples)
    {
        // Arrange
        var clock = new Clock(SampleRate);
        clock.Tempo = 120.0; // 2 beats per second, 0.5 seconds per beat

        // Act
        var result = clock.MusicalTimeToSamples(bar, beat, tick);

        // Assert
        result.Should().Be(expectedSamples);
    }

    [Fact]
    public void MusicalTimeToSamples_InvalidInput_ThrowsException()
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act & Assert
        clock.Invoking(c => c.MusicalTimeToSamples(0, 1, 0)).Should().Throw<ArgumentOutOfRangeException>();
        clock.Invoking(c => c.MusicalTimeToSamples(1, 0, 0)).Should().Throw<ArgumentOutOfRangeException>();
        clock.Invoking(c => c.MusicalTimeToSamples(1, 1, -1)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TimeSignature_SetValidValues_UpdatesAndFiresEvent()
    {
        // Arrange
        var clock = new Clock(SampleRate);
        (int numerator, int denominator) firedSignature = (0, 0);
        clock.TimeSignatureChanged += (_, sig) => firedSignature = sig;

        // Act
        clock.TimeSignatureNumerator = 3;
        clock.TimeSignatureDenominator = 8;

        // Assert
        clock.TimeSignatureNumerator.Should().Be(3);
        clock.TimeSignatureDenominator.Should().Be(8);
        firedSignature.numerator.Should().Be(3);
        firedSignature.denominator.Should().Be(8);
    }

    [Fact]
    public void TimeSignatureDenominator_SetInvalidValue_ThrowsException()
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act & Assert
        clock.Invoking(c => c.TimeSignatureDenominator = 0).Should().Throw<ArgumentOutOfRangeException>();
        clock.Invoking(c => c.TimeSignatureDenominator = -1).Should().Throw<ArgumentOutOfRangeException>();
        clock.Invoking(c => c.TimeSignatureDenominator = 3).Should().Throw<ArgumentOutOfRangeException>(); // Not power of 2
        clock.Invoking(c => c.TimeSignatureDenominator = 6).Should().Throw<ArgumentOutOfRangeException>(); // Not power of 2
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void TimeSignatureDenominator_SetValidPowerOfTwo_Succeeds(int denominator)
    {
        // Arrange
        var clock = new Clock(SampleRate);

        // Act & Assert
        clock.Invoking(c => c.TimeSignatureDenominator = denominator).Should().NotThrow();
        clock.TimeSignatureDenominator.Should().Be(denominator);
    }
}