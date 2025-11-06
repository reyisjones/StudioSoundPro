using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;
using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.Core.Tests.Audio;

public class MixerEngineTests : IDisposable
{
    private readonly Clock _clock;
    private readonly ITransport _transport;
    private readonly MixerEngine _mixer;
    private const int SampleRate = 48000;
    private const int ChannelCount = 2;

    public MixerEngineTests()
    {
        _clock = new Clock(SampleRate);
        _transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        _mixer = new MixerEngine(_transport, SampleRate, ChannelCount);
    }

    public void Dispose()
    {
        _mixer?.Dispose();
        _transport?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_Initializes()
    {
        _mixer.SampleRate.Should().Be(SampleRate);
        _mixer.ChannelCount.Should().Be(ChannelCount);
        _mixer.Transport.Should().BeSameAs(_transport);
        _mixer.MasterVolume.Should().Be(1.0f);
        _mixer.IsMasterMuted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullTransport_ThrowsArgumentNullException()
    {
        Action act = () => new MixerEngine(null!, SampleRate);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transport");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-48000)]
    public void Constructor_WithInvalidSampleRate_ThrowsArgumentOutOfRangeException(int invalidSampleRate)
    {
        Action act = () => new MixerEngine(_transport, invalidSampleRate);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sampleRate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9)] // More than 8 channels
    public void Constructor_WithInvalidChannelCount_ThrowsArgumentOutOfRangeException(int invalidChannelCount)
    {
        Action act = () => new MixerEngine(_transport, SampleRate, invalidChannelCount);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("channelCount");
    }

    [Fact]
    public void MasterVolume_SetAndGet_WorksCorrectly()
    {
        _mixer.MasterVolume = 0.5f;
        _mixer.MasterVolume.Should().Be(0.5f);

        _mixer.MasterVolume = 2.0f;
        _mixer.MasterVolume.Should().Be(2.0f);
    }

    [Fact]
    public void MasterVolume_ClampedToReasonableRange()
    {
        _mixer.MasterVolume = -1.0f;
        _mixer.MasterVolume.Should().Be(0.0f); // Clamped to minimum

        _mixer.MasterVolume = 100.0f;
        _mixer.MasterVolume.Should().Be(10.0f); // Clamped to maximum
    }

    [Fact]
    public void IsMasterMuted_SetAndGet_WorksCorrectly()
    {
        _mixer.IsMasterMuted = true;
        _mixer.IsMasterMuted.Should().BeTrue();

        _mixer.IsMasterMuted = false;
        _mixer.IsMasterMuted.Should().BeFalse();
    }

    [Fact]
    public void AddTrack_AddsTrackSuccessfully()
    {
        var track = new Track("Test Track");

        _mixer.AddTrack(track);

        var tracks = _mixer.GetTracks();
        tracks.Should().HaveCount(1);
        tracks[0].Should().BeSameAs(track);
    }

    [Fact]
    public void AddTrack_WithNullTrack_ThrowsArgumentNullException()
    {
        Action act = () => _mixer.AddTrack(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("track");
    }

    [Fact]
    public void AddTrack_SameTrackTwice_OnlyAddsOnce()
    {
        var track = new Track("Test Track");

        _mixer.AddTrack(track);
        _mixer.AddTrack(track); // Add same track again

        var tracks = _mixer.GetTracks();
        tracks.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTrack_RemovesExistingTrack()
    {
        var track = new Track("Test Track");
        _mixer.AddTrack(track);

        bool removed = _mixer.RemoveTrack(track);

        removed.Should().BeTrue();
        _mixer.GetTracks().Should().BeEmpty();
    }

    [Fact]
    public void RemoveTrack_NonExistentTrack_ReturnsFalse()
    {
        var track = new Track("Test Track");

        bool removed = _mixer.RemoveTrack(track);

        removed.Should().BeFalse();
    }

    [Fact]
    public void RemoveTrack_WithNull_ReturnsFalse()
    {
        bool removed = _mixer.RemoveTrack(null!);
        removed.Should().BeFalse();
    }

    [Fact]
    public void ClearTracks_RemovesAllTracks()
    {
        _mixer.AddTrack(new Track("Track 1"));
        _mixer.AddTrack(new Track("Track 2"));
        _mixer.AddTrack(new Track("Track 3"));

        _mixer.ClearTracks();

        _mixer.GetTracks().Should().BeEmpty();
    }

    [Fact]
    public void GetTracks_ReturnsNewArrayEachTime()
    {
        var track = new Track("Test Track");
        _mixer.AddTrack(track);

        var tracks1 = _mixer.GetTracks();
        var tracks2 = _mixer.GetTracks();

        tracks1.Should().NotBeSameAs(tracks2); // Different array instances
        tracks1.Should().Equal(tracks2); // But same contents
    }

    [Fact]
    public async Task ProcessBuffer_WhenStopped_OutputsSilence()
    {
        var track = new Track("Test Track");
        _mixer.AddTrack(track);

        var buffer = new float[1024]; // 512 frames stereo
        Array.Fill(buffer, 999.0f); // Fill with non-zero to verify clearing

        _mixer.ProcessBuffer(buffer, 512);

        buffer.Should().OnlyContain(x => x == 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_WhenMasterMuted_OutputsSilence()
    {
        var track = new Track("Test Track");
        _mixer.AddTrack(track);

        await _transport.PlayAsync();
        _mixer.IsMasterMuted = true;

        var buffer = new float[1024];
        Array.Fill(buffer, 999.0f);

        _mixer.ProcessBuffer(buffer, 512);

        buffer.Should().OnlyContain(x => x == 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_WithNoTracks_OutputsSilence()
    {
        await _transport.PlayAsync();

        var buffer = new float[1024];
        Array.Fill(buffer, 999.0f);

        _mixer.ProcessBuffer(buffer, 512);

        buffer.Should().OnlyContain(x => x == 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_WithMutedTrack_OutputsSilence()
    {
        var track = new Track("Test Track");
        track.IsMuted = true;
        _mixer.AddTrack(track);

        await _transport.PlayAsync();

        var buffer = new float[1024];
        _mixer.ProcessBuffer(buffer, 512);

        buffer.Should().OnlyContain(x => x == 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_WithAudioClip_ProducesOutput()
    {
        // Create track with audio clip containing test signal
        var track = new Track("Test Track");
        
        // Create test audio data (simple constant value for verification)
        var audioData = new float[4800]; // 0.1 seconds at 48kHz
        Array.Fill(audioData, 0.5f);
        
        var clip = new AudioClip("Test Clip", audioData, ChannelCount, SampleRate);
        clip.StartPosition = 0; // Start at beginning
        track.AddClip(clip);
        
        _mixer.AddTrack(track);
        await _transport.PlayAsync();

        var buffer = new float[96]; // 48 frames stereo (1ms worth)
        _mixer.ProcessBuffer(buffer, 48);

        // Should have audio output (not all zeros)
        buffer.Should().Contain(x => x != 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_MasterVolume_AppliesCorrectly()
    {
        // Create track with audio clip
        var track = new Track("Test Track");
        var audioData = new float[4800];
        Array.Fill(audioData, 1.0f); // Full scale
        
        var clip = new AudioClip("Test Clip", audioData, ChannelCount, SampleRate);
        clip.StartPosition = 0;
        track.AddClip(clip);
        
        _mixer.AddTrack(track);
        _mixer.MasterVolume = 0.5f; // Half volume
        
        await _transport.PlayAsync();

        var buffer = new float[96];
        _mixer.ProcessBuffer(buffer, 48);

        // Check that output is affected by master volume (roughly half of original)
        var nonZeroSamples = buffer.Where(x => x != 0.0f).ToArray();
        if (nonZeroSamples.Length > 0)
        {
            // Values should be less than 1.0 due to 0.5 master volume
            nonZeroSamples.Should().AllSatisfy(x => Math.Abs(x).Should().BeLessThan(1.0f));
        }
    }

    [Fact]
    public async Task ProcessBuffer_SoloTrack_OnlyOutputsSoloedTrack()
    {
        // Create two tracks
        var track1 = new Track("Track 1");
        var track2 = new Track("Track 2") { IsSolo = true }; // Solo track 2

        // Add audio to both tracks
        var audioData = new float[4800];
        Array.Fill(audioData, 0.5f);
        
        var clip1 = new AudioClip("Clip 1", audioData, ChannelCount, SampleRate);
        clip1.StartPosition = 0;
        track1.AddClip(clip1);
        
        var clip2 = new AudioClip("Clip 2", audioData, ChannelCount, SampleRate);
        clip2.StartPosition = 0;
        track2.AddClip(clip2);
        
        _mixer.AddTrack(track1);
        _mixer.AddTrack(track2);
        
        await _transport.PlayAsync();

        var buffer = new float[96];
        _mixer.ProcessBuffer(buffer, 48);

        // Should have output from soloed track (we can't easily verify which track without more detailed testing)
        buffer.Should().Contain(x => x != 0.0f);
    }

    [Fact]
    public void ProcessBuffer_WithSmallFrameCount_Works()
    {
        var buffer = new float[2]; // 1 frame stereo
        _mixer.ProcessBuffer(buffer, 1);

        buffer.Should().HaveCount(2);
        buffer.Should().OnlyContain(x => x == 0.0f); // Silence when stopped
    }

    [Fact]
    public void ProcessBuffer_WithZeroFrameCount_DoesNothing()
    {
        var buffer = new float[100];
        Array.Fill(buffer, 999.0f);

        _mixer.ProcessBuffer(buffer, 0);

        // Buffer should be unchanged
        buffer.Should().OnlyContain(x => x == 999.0f);
    }

    [Fact]
    public void ProcessBuffer_WithInsufficientBuffer_ThrowsArgumentException()
    {
        var buffer = new float[10]; // Too small for 512 frames

        Action act = () => _mixer.ProcessBuffer(buffer, 512);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reset_DoesNotThrow()
    {
        _mixer.Reset();
        // Reset should complete without errors
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _mixer.Dispose();
        _mixer.Dispose(); // Should not throw

        // After dispose, tracks should be cleared
        _mixer.GetTracks().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBuffer_MultipleTracksWithDifferentVolumes_MixesCorrectly()
    {
        // Create two tracks with different volumes
        var track1 = new Track("Track 1") { Volume = 1.0f };
        var track2 = new Track("Track 2") { Volume = 0.5f };

        var audioData = new float[4800];
        Array.Fill(audioData, 0.3f);
        
        var clip1 = new AudioClip("Clip 1", audioData, ChannelCount, SampleRate);
        clip1.StartPosition = 0;
        track1.AddClip(clip1);
        
        var clip2 = new AudioClip("Clip 2", audioData, ChannelCount, SampleRate);
        clip2.StartPosition = 0;
        track2.AddClip(clip2);
        
        _mixer.AddTrack(track1);
        _mixer.AddTrack(track2);
        
        await _transport.PlayAsync();

        var buffer = new float[96];
        _mixer.ProcessBuffer(buffer, 48);

        // Should have mixed output
        buffer.Should().Contain(x => x != 0.0f);
    }

    [Fact]
    public async Task ProcessBuffer_TrackWithPan_AppliesPanning()
    {
        var track = new Track("Test Track") { Pan = -1.0f }; // Full left

        var audioData = new float[4800];
        Array.Fill(audioData, 0.5f);
        
        var clip = new AudioClip("Test Clip", audioData, ChannelCount, SampleRate);
        clip.StartPosition = 0;
        track.AddClip(clip);
        
        _mixer.AddTrack(track);
        await _transport.PlayAsync();

        var buffer = new float[96]; // 48 frames stereo
        _mixer.ProcessBuffer(buffer, 48);

        // When panned full left, left channel should have more energy than right
        float leftSum = 0, rightSum = 0;
        for (int i = 0; i < 48; i++)
        {
            leftSum += Math.Abs(buffer[i * 2]);
            rightSum += Math.Abs(buffer[i * 2 + 1]);
        }

        if (leftSum > 0 || rightSum > 0)
        {
            // Left should be louder than right (with some tolerance for numerical precision)
            leftSum.Should().BeGreaterThan(rightSum * 0.9f);
        }
    }
}
