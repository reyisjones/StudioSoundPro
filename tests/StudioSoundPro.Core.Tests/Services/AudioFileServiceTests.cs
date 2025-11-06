using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Tracks;
using StudioSoundPro.Core.Services;

namespace StudioSoundPro.Core.Tests.Services;

public class AudioFileServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _tempFiles = new();
    private readonly AudioFileService _service;

    public AudioFileServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AudioFileServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _service = new AudioFileService();
    }

    public void Dispose()
    {
        _service?.Dispose();

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

    private string CreateTestWavFile(string filename, float[] samples, int channels = 2, int sampleRate = 48000)
    {
        var filePath = GetTempFilePath(filename);
        using var writer = new WavFileWriter();
        var options = new AudioFileWriteOptions
        {
            SampleRate = sampleRate,
            Channels = channels,
            BitsPerSample = 16
        };
        writer.Write(filePath, samples, options);
        return filePath;
    }

    [Fact]
    public void ImportAudioFile_StereoFile_CreatesClip()
    {
        // Arrange
        var samples = new float[] { 0.1f, -0.1f, 0.2f, -0.2f, 0.3f, -0.3f };
        var filePath = CreateTestWavFile("stereo.wav", samples, channels: 2);

        // Act
        var clip = _service.ImportAudioFile(filePath);

        // Assert
        clip.Should().NotBeNull();
        clip.Name.Should().Be("stereo");
        clip.Length.Should().Be(3); // 6 samples / 2 channels
        clip.Channels.Should().Be(2);
        clip.SampleRate.Should().Be(48000);
    }

    [Fact]
    public void ImportAudioFile_MonoFile_ConvertedToStereo()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f };
        var filePath = CreateTestWavFile("mono.wav", samples, channels: 1);

        // Act
        var clip = _service.ImportAudioFile(filePath);

        // Assert
        clip.Length.Should().Be(3);
        clip.Channels.Should().Be(2);
        
        // Verify stereo conversion by checking audio data
        var audioData = clip.AudioData;
        audioData.Should().NotBeNull();
        audioData!.Length.Should().Be(6); // 3 samples * 2 channels
        
        // Both channels should have the same values (mono -> stereo)
        audioData[0].Should().BeApproximately(audioData[1], 0.001f);
        audioData[2].Should().BeApproximately(audioData[3], 0.001f);
        audioData[4].Should().BeApproximately(audioData[5], 0.001f);
    }

    [Fact]
    public void ImportAudioFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Action act = () => _service.ImportAudioFile("nonexistent.wav");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ImportAudioFile_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => _service.ImportAudioFile(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImportAudioFile_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => _service.ImportAudioFile("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ImportAudioFileAsync_CreatesClip()
    {
        // Arrange
        var samples = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var filePath = CreateTestWavFile("async.wav", samples);

        // Act
        var clip = await _service.ImportAudioFileAsync(filePath);

        // Assert
        clip.Should().NotBeNull();
        clip.Name.Should().Be("async");
        clip.Channels.Should().Be(2);
        clip.Length.Should().Be(2);
    }

    [Fact]
    public void ExportTrack_WithClips_CreatesWavFile()
    {
        // Arrange
        var track = new Track("Test Track");
        
        // Create simple test audio
        var audioData = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var clip = new AudioClip("Test Clip", audioData, 2, 48000);
        clip.StartPosition = 0;
        
        track.AddClip(clip);
        
        var outputPath = GetTempFilePath("export.wav");

        // Act
        _service.ExportTrack(track, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        
        // Verify by reading back
        using var reader = new WavFileReader();
        reader.Open(outputPath);
        reader.Info.Channels.Should().Be(2);
        reader.Info.SampleRate.Should().Be(48000);
    }

    [Fact]
    public void ExportTrack_EmptyTrack_ThrowsInvalidOperationException()
    {
        // Arrange
        var track = new Track("Empty Track");
        var outputPath = GetTempFilePath("empty.wav");

        // Act & Assert
        Action act = () => _service.ExportTrack(track, outputPath);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExportTrack_NullTrack_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => _service.ExportTrack(null!, "output.wav");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExportTrack_WithCustomOptions_UsesOptions()
    {
        // Arrange
        var track = new Track("Test Track");
        var audioData = new float[] { 0.1f, -0.1f };
        var clip = new AudioClip("Test Clip", audioData, 2, 48000);
        clip.StartPosition = 0;
        track.AddClip(clip);
        
        var outputPath = GetTempFilePath("custom.wav");
        var options = new AudioFileWriteOptions
        {
            SampleRate = 44100,
            Channels = 2,
            BitsPerSample = 24
        };

        // Act
        _service.ExportTrack(track, outputPath, options);

        // Assert
        using var reader = new WavFileReader();
        reader.Open(outputPath);
        reader.Info.SampleRate.Should().Be(44100);
        reader.Info.BitsPerSample.Should().Be(24);
    }

    [Fact]
    public async Task ExportTrackAsync_CreatesFile()
    {
        // Arrange
        var track = new Track("Async Track");
        var audioData = new float[] { 0.1f, -0.1f };
        var clip = new AudioClip("Async Clip", audioData, 2, 48000);
        clip.StartPosition = 0;
        track.AddClip(clip);
        
        var outputPath = GetTempFilePath("async_export.wav");

        // Act
        await _service.ExportTrackAsync(track, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public void ImportAndExport_RoundTrip_PreservesAudio()
    {
        // Arrange - create original file
        var originalSamples = new float[] { 0.5f, -0.5f, 0.25f, -0.25f };
        var importPath = CreateTestWavFile("roundtrip.wav", originalSamples);

        // Act - import
        var clip = _service.ImportAudioFile(importPath);
        var track = new Track("Round Trip");
        track.AddClip(clip);

        // Act - export
        var exportPath = GetTempFilePath("roundtrip_export.wav");
        _service.ExportTrack(track, exportPath);

        // Assert - read back and compare
        using var reader = new WavFileReader();
        reader.Open(exportPath);
        var exportedSamples = reader.ReadAllSamples();
        
        exportedSamples.Should().HaveCount(4);
        for (int i = 0; i < originalSamples.Length; i++)
        {
            exportedSamples[i].Should().BeApproximately(originalSamples[i], 0.01f);
        }
    }

    [Fact]
    public void ExportTrack_MultipleClips_MixesTogether()
    {
        // Arrange
        var track = new Track("Multi Clip");
        
        // Create audio data for 2 frames (4 samples interleaved: L,R,L,R)
        var audioData1 = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
        var clip1 = new AudioClip("Clip 1", audioData1, 2, 48000);
        clip1.StartPosition = 0;
        
        var audioData2 = new float[] { 0.3f, 0.3f };
        var clip2 = new AudioClip("Clip 2", audioData2, 2, 48000);
        clip2.StartPosition = 1; // Starts one frame later
        
        track.AddClip(clip1);
        track.AddClip(clip2);
        
        var outputPath = GetTempFilePath("mixed.wav");

        // Act
        _service.ExportTrack(track, outputPath);

        // Assert
        using var reader = new WavFileReader();
        reader.Open(outputPath);
        var samples = reader.ReadAllSamples();
        
        // First frame (samples 0,1): only clip1
        samples[0].Should().BeApproximately(0.5f, 0.01f);
        samples[1].Should().BeApproximately(0.5f, 0.01f);
        
        // Second frame (samples 2,3): both clips mixed
        samples[2].Should().BeApproximately(0.8f, 0.01f); // 0.5 + 0.3
        samples[3].Should().BeApproximately(0.8f, 0.01f); // 0.5 + 0.3
    }
}
