using System;
using System.IO;
using Xunit;
using FluentAssertions;
using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.Core.Tests.Audio;

public class WavFileTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _tempFiles = new();

    public WavFileTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "StudioSoundProTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    private string GetTempFilePath(string filename)
    {
        var path = Path.Combine(_tempDirectory, filename);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void WriteAndRead_16BitStereo_RoundTrips()
    {
        // Arrange
        var samples = new float[] { 0.5f, -0.5f, 0.25f, -0.25f, 0.0f, 1.0f };
        var options = new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 16
        };
        var filePath = GetTempFilePath("test_16bit.wav");

        // Act - Write
        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        // Act - Read
        float[] readSamples;
        AudioFileInfo info;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            info = reader.Info;
            readSamples = reader.ReadAllSamples();
        }

        // Assert
        info.SampleRate.Should().Be(48000);
        info.Channels.Should().Be(2);
        info.BitsPerSample.Should().Be(16);
        info.TotalSamples.Should().Be(3); // 6 samples / 2 channels
        info.Format.Should().Be("PCM");

        readSamples.Should().HaveCount(6);
        
        // 16-bit has limited precision, so we allow small differences
        for (int i = 0; i < samples.Length; i++)
        {
            readSamples[i].Should().BeApproximately(samples[i], 0.001f);
        }
    }

    [Fact]
    public void WriteAndRead_24BitMono_RoundTrips()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f, -0.4f };
        var options = new AudioFileWriteOptions
        {
            SampleRate = 44100,
            Channels = 1,
            BitsPerSample = 24
        };
        var filePath = GetTempFilePath("test_24bit.wav");

        // Act - Write
        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        // Act - Read
        float[] readSamples;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            readSamples = reader.ReadAllSamples();
        }

        // Assert
        readSamples.Should().HaveCount(4);
        for (int i = 0; i < samples.Length; i++)
        {
            readSamples[i].Should().BeApproximately(samples[i], 0.0001f);
        }
    }

    [Fact]
    public void WriteAndRead_32BitFloat_RoundTrips()
    {
        // Arrange
        var samples = new float[] { 0.123456f, -0.987654f, 0.5f, -0.5f };
        var options = new AudioFileWriteOptions
        {
            SampleRate = 96000,
            Channels = 2,
            BitsPerSample = 32,
            UseFloatFormat = true
        };
        var filePath = GetTempFilePath("test_32float.wav");

        // Act - Write
        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        // Act - Read
        float[] readSamples;
        AudioFileInfo info;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            info = reader.Info;
            readSamples = reader.ReadAllSamples();
        }

        // Assert
        info.Format.Should().Be("IEEE Float");
        readSamples.Should().HaveCount(4);
        
        // Float format should be exact
        for (int i = 0; i < samples.Length; i++)
        {
            readSamples[i].Should().BeApproximately(samples[i], 0.000001f);
        }
    }

    [Fact]
    public void Write_ClampsSamples_WhenOutOfRange()
    {
        // Arrange
        var samples = new float[] { 2.0f, -2.0f, 1.5f, -1.5f };
        var options = new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 1,
            BitsPerSample = 16
        };
        var filePath = GetTempFilePath("test_clamped.wav");

        // Act
        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        float[] readSamples;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            readSamples = reader.ReadAllSamples();
        }

        // Assert - values should be clamped to [-1, 1]
        readSamples[0].Should().BeApproximately(1.0f, 0.001f);
        readSamples[1].Should().BeApproximately(-1.0f, 0.001f);
        readSamples[2].Should().BeApproximately(1.0f, 0.001f);
        readSamples[3].Should().BeApproximately(-1.0f, 0.001f);
    }

    [Fact]
    public void Read_NonExistentFile_ThrowsFileNotFoundException()
    {
        using var reader = new WavFileReader();
        
        Action act = () => reader.Open("nonexistent.wav");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Read_InvalidFile_ThrowsInvalidDataException()
    {
        // Arrange - create a file with invalid content
        var filePath = GetTempFilePath("invalid.wav");
        File.WriteAllText(filePath, "This is not a WAV file");

        // Act & Assert
        using var reader = new WavFileReader();
        Action act = () => reader.Open(filePath);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Write_NullSamples_ThrowsArgumentException()
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions();
        
        Action act = () => writer.Write("test.wav", null!, options);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Write_EmptySamples_ThrowsArgumentException()
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions();
        
        Action act = () => writer.Write("test.wav", Array.Empty<float>(), options);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Write_SampleCountNotDivisibleByChannels_ThrowsArgumentException()
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions { Channels = 2 };
        var samples = new float[] { 0.1f, 0.2f, 0.3f }; // 3 samples, not divisible by 2
        
        Action act = () => writer.Write("test.wav", samples, options);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-48000)]
    public void Write_InvalidSampleRate_ThrowsArgumentException(int sampleRate)
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions { SampleRate = sampleRate };
        var samples = new float[] { 0.1f, 0.2f };
        
        Action act = () => writer.Write("test.wav", samples, options);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9)] // More than 8 channels
    public void Write_InvalidChannelCount_ThrowsArgumentException(int channels)
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions { Channels = channels };
        var samples = new float[] { 0.1f, 0.2f };
        
        Action act = () => writer.Write("test.wav", samples, options);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    [InlineData(48)]
    public void Write_UnsupportedBitDepth_ThrowsArgumentException(int bitsPerSample)
    {
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions { BitsPerSample = bitsPerSample };
        var samples = new float[] { 0.1f, 0.2f };
        
        Action act = () => writer.Write("test.wav", samples, options);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AudioFileInfo_CalculatesDuration_Correctly()
    {
        // Arrange
        var samples = new float[96000]; // 1 second at 48kHz stereo
        var options = new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 16
        };
        var filePath = GetTempFilePath("test_duration.wav");

        // Act
        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        AudioFileInfo info;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            info = reader.Info;
        }

        // Assert
        info.Duration.TotalSeconds.Should().BeApproximately(1.0, 0.001);
        info.TotalSamples.Should().Be(48000);
    }

    [Fact]
    public async Task WriteAsync_CreatesValidFile()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var options = new AudioFileWriteOptions
        {
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 16
        };
        var filePath = GetTempFilePath("test_async.wav");

        // Act
        using (var writer = new WavFileWriter())
        {
            await writer.WriteAsync(filePath, samples, options);
        }

        // Assert
        File.Exists(filePath).Should().BeTrue();
        
        using var reader = new WavFileReader();
        reader.Open(filePath);
        var readSamples = reader.ReadAllSamples();
        readSamples.Should().HaveCount(4);
    }

    [Fact]
    public async Task ReadAllSamplesAsync_ReadsFile()
    {
        // Arrange
        var samples = new float[] { 0.5f, -0.5f };
        var options = new AudioFileWriteOptions();
        var filePath = GetTempFilePath("test_read_async.wav");

        using (var writer = new WavFileWriter())
        {
            writer.Write(filePath, samples, options);
        }

        // Act
        float[] readSamples;
        using (var reader = new WavFileReader())
        {
            reader.Open(filePath);
            readSamples = await reader.ReadAllSamplesAsync();
        }

        // Assert
        readSamples.Should().HaveCount(2);
    }
}
