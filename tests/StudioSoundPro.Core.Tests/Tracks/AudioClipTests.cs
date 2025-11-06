using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.Tests.Tracks;

public class AudioClipTests
{
    private static float[] CreateTestAudio(int lengthInSamples, int channels, float amplitude = 0.5f)
    {
        var data = new float[lengthInSamples * channels];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = amplitude * MathF.Sin(2.0f * MathF.PI * i / 100.0f);
        }
        return data;
    }

    [Fact]
    public void Constructor_WithAudioData_CreatesClip()
    {
        var audioData = CreateTestAudio(1000, 2);
        
        var clip = new AudioClip("Test", audioData, 2, 48000);
        
        Assert.Equal("Test", clip.Name);
        Assert.Equal(2, clip.Channels);
        Assert.Equal(48000, clip.SampleRate);
        Assert.Equal(1000, clip.Length);
    }

    [Fact]
    public void Constructor_WithLength_CreatesEmptyClip()
    {
        var clip = new AudioClip("Test", 1000, 2, 48000);
        
        Assert.Equal(2, clip.Channels);
        Assert.Equal(48000, clip.SampleRate);
        Assert.Equal(1000, clip.Length);
    }

    [Fact]
    public void Constructor_ThrowsOnNullAudioData()
    {
        Assert.Throws<ArgumentException>(() => 
            new AudioClip("Test", null!, 2, 48000));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyAudioData()
    {
        Assert.Throws<ArgumentException>(() => 
            new AudioClip("Test", Array.Empty<float>(), 2, 48000));
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidChannels()
    {
        var audioData = CreateTestAudio(100, 2);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new AudioClip("Test", audioData, 0, 48000));
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidSampleRate()
    {
        var audioData = CreateTestAudio(100, 2);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new AudioClip("Test", audioData, 2, 0));
    }

    [Fact]
    public void Constructor_ThrowsOnMismatchedChannels()
    {
        var audioData = new float[101]; // Not divisible by 2
        
        Assert.Throws<ArgumentException>(() => 
            new AudioClip("Test", audioData, 2, 48000));
    }

    [Fact]
    public void AudioData_ReturnsArrayReference()
    {
        var audioData = CreateTestAudio(100, 2);
        var clip = new AudioClip("Test", audioData, 2, 48000);
        
        var data = clip.AudioData;
        
        Assert.NotNull(data);
        Assert.Equal(audioData.Length, data.Length);
        for (int i = 0; i < audioData.Length; i++)
        {
            Assert.Equal(audioData[i], data[i]);
        }
    }

    [Fact]
    public void ReadSamples_ReadsCorrectData()
    {
        var audioData = CreateTestAudio(1000, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 1000
        };
        
        var buffer = new float[100]; // 50 frames * 2 channels
        int samplesRead = clip.ReadSamples(buffer, 0, 50, 0);
        
        Assert.Equal(50, samplesRead);
        // Verify first sample matches (accounting for gain = 1.0)
        Assert.Equal(audioData[0], buffer[0], 0.0001f);
        Assert.Equal(audioData[1], buffer[1], 0.0001f);
    }

    [Fact]
    public void ReadSamples_AppliesGain()
    {
        var audioData = CreateTestAudio(1000, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 1000,
            Gain = 0.5f
        };
        
        var buffer = new float[100];
        clip.ReadSamples(buffer, 0, 50, 0);
        
        // Check gain is applied
        Assert.Equal(audioData[0] * 0.5f, buffer[0], 0.0001f);
        Assert.Equal(audioData[1] * 0.5f, buffer[1], 0.0001f);
    }

    [Fact]
    public void ReadSamples_ReturnsSilenceWhenMuted()
    {
        var audioData = CreateTestAudio(1000, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 1000,
            IsMuted = true
        };
        
        var buffer = new float[100];
        int samplesRead = clip.ReadSamples(buffer, 0, 50, 0);
        
        Assert.Equal(0, samplesRead);
        Assert.All(buffer, sample => Assert.Equal(0.0f, sample));
    }

    [Fact]
    public void ReadSamples_ReturnsSilenceOutsideClipBounds()
    {
        var audioData = CreateTestAudio(1000, 2);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 1000,
            Length = 1000
        };
        
        var buffer = new float[100];
        
        // Before clip
        int samplesRead = clip.ReadSamples(buffer, 0, 50, 0);
        Assert.Equal(0, samplesRead);
        Assert.All(buffer, sample => Assert.Equal(0.0f, sample));
        
        // After clip
        samplesRead = clip.ReadSamples(buffer, 0, 50, 3000);
        Assert.Equal(0, samplesRead);
    }

    [Fact]
    public void ReadSamples_HandlesPartialReads()
    {
        var audioData = CreateTestAudio(100, 2);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        var buffer = new float[400]; // Request 200 stereo samples (400 samples total) but only 100 frames available
        int samplesRead = clip.ReadSamples(buffer, 0, 400, 0);
        
        Assert.Equal(200, samplesRead); // Should return 100 frames * 2 channels = 200 samples
        // Remaining buffer should be cleared
        for (int i = 200; i < buffer.Length; i++)
        {
            Assert.Equal(0.0f, buffer[i]);
        }
    }

    [Fact]
    public void ReadSamples_RespectsSourceOffset()
    {
        var audioData = CreateTestAudio(1000, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 500,
            SourceOffset = 500 // Skip first 500 frames
        };
        
        var buffer = new float[4];
        clip.ReadSamples(buffer, 0, 2, 0);
        
        // Should read from index 500*2 = 1000
        Assert.Equal(audioData[1000], buffer[0], 0.0001f);
        Assert.Equal(audioData[1001], buffer[1], 0.0001f);
    }

    [Fact]
    public void ReadSamples_AppliesFadeIn()
    {
        // Create constant amplitude audio for predictable fade testing
        var audioData = new float[1000];
        for (int i = 0; i < audioData.Length; i++)
            audioData[i] = 1.0f;
            
        var clip = new AudioClip("Test", audioData, 1, 48000)
        {
            StartPosition = 0,
            Length = 1000,
            FadeInLength = 100
        };
        
        var buffer = new float[1];
        
        // At start, should be 0
        clip.ReadSamples(buffer, 0, 1, 0);
        Assert.Equal(0.0f, buffer[0], 0.01f);
        
        // At 50% of fade-in, should be ~0.5
        clip.ReadSamples(buffer, 0, 1, 50);
        Assert.InRange(buffer[0], 0.45f, 0.55f);
        
        // After fade-in, should be full amplitude
        clip.ReadSamples(buffer, 0, 1, 150);
        Assert.Equal(1.0f, buffer[0], 0.01f);
    }

    [Fact]
    public void ReadSamples_AppliesFadeOut()
    {
        // Create constant amplitude audio for predictable fade testing
        var audioData = new float[1000];
        for (int i = 0; i < audioData.Length; i++)
            audioData[i] = 1.0f;
            
        var clip = new AudioClip("Test", audioData, 1, 48000)
        {
            StartPosition = 0,
            Length = 1000,
            FadeOutLength = 100
        };
        
        var buffer = new float[1];
        
        // Before fade-out, should be full amplitude
        clip.ReadSamples(buffer, 0, 1, 850);
        Assert.Equal(1.0f, buffer[0], 0.01f);
        
        // At 50% of fade-out
        clip.ReadSamples(buffer, 0, 1, 950);
        Assert.InRange(buffer[0], 0.45f, 0.55f);
        
        // At end, should be ~0
        clip.ReadSamples(buffer, 0, 1, 999);
        Assert.InRange(buffer[0], 0.0f, 0.05f);
    }

    [Fact]
    public void ReadSamples_ThrowsOnInvalidOffset()
    {
        var clip = new AudioClip("Test", 100, 2, 48000);
        var buffer = new float[100];
        
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            clip.ReadSamples(buffer, -1, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            clip.ReadSamples(buffer, 100, 10, 0));
    }

    [Fact]
    public void ReadSamples_ThrowsOnInvalidCount()
    {
        var clip = new AudioClip("Test", 100, 2, 48000);
        var buffer = new float[100];
        
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            clip.ReadSamples(buffer, 0, -1, 0));
    }

    [Fact]
    public void ReadSamples_ThrowsOnBufferTooSmall()
    {
        var clip = new AudioClip("Test", 100, 2, 48000);
        var buffer = new float[10];
        
        Assert.Throws<ArgumentException>(() => 
            clip.ReadSamples(buffer, 0, 20, 0));
    }

    [Fact]
    public void GetPeakAmplitude_ReturnsCorrectValue()
    {
        var audioData = new float[200]; // 100 stereo frames
        audioData[0] = 0.8f;  // Peak in first sample
        audioData[50] = -0.9f; // Higher peak at sample 25
        
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        float peak = clip.GetPeakAmplitude(0, 100);
        
        Assert.Equal(0.9f, peak, 0.01f);
    }

    [Fact]
    public void GetPeakAmplitude_ReturnsZeroWhenMuted()
    {
        var audioData = CreateTestAudio(100, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100,
            IsMuted = true
        };
        
        float peak = clip.GetPeakAmplitude(0, 100);
        
        Assert.Equal(0.0f, peak);
    }

    [Fact]
    public void GetPeakAmplitude_AppliesGain()
    {
        var audioData = new float[200]; // 100 stereo frames
        audioData[50] = 1.0f;
        
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100,
            Gain = 0.5f
        };
        
        float peak = clip.GetPeakAmplitude(0, 100);
        
        Assert.Equal(0.5f, peak, 0.01f);
    }

    [Fact]
    public void WriteSamples_WritesSamplesToBuffer()
    {
        var clip = new AudioClip("Test", 100, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        var source = new float[20]; // 10 stereo frames = 20 samples
        for (int i = 0; i < source.Length; i++)
            source[i] = 0.5f;
        
        int samplesWritten = clip.WriteSamples(source, 0, 10, 0);
        
        Assert.Equal(10, samplesWritten); // Returns frame count
        
        // Verify data was written - ReadSamples expects sample count (20)
        var readBuffer = new float[20];
        int samplesRead = clip.ReadSamples(readBuffer, 0, 20, 0);
        Assert.Equal(20, samplesRead); // Should return 20 samples
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(0.5f, readBuffer[i]);
        }
    }

    [Fact]
    public void WriteSamples_RespectsPosition()
    {
        var clip = new AudioClip("Test", 100, 2, 48000)
        {
            StartPosition = 50,
            Length = 100
        };
        
        var source = new float[4]; // 2 stereo frames
        for (int i = 0; i < source.Length; i++)
            source[i] = 0.7f;
        
        int samplesWritten = clip.WriteSamples(source, 0, 2, 60);
        
        Assert.Equal(2, samplesWritten);
    }

    [Fact]
    public void GetRmsAmplitude_CalculatesCorrectly()
    {
        var audioData = new float[200]; // 100 stereo frames
        for (int i = 0; i < audioData.Length; i++)
            audioData[i] = 0.5f;
        
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        float rms = clip.GetRmsAmplitude(0, 100);
        
        // RMS of constant 0.5 is 0.5
        Assert.Equal(0.5f, rms, 0.01f);
    }

    [Fact]
    public void GetRmsAmplitude_ReturnsZeroWhenMuted()
    {
        var audioData = CreateTestAudio(100, 2, 1.0f);
        var clip = new AudioClip("Test", audioData, 2, 48000)
        {
            StartPosition = 0,
            Length = 100,
            IsMuted = true
        };
        
        float rms = clip.GetRmsAmplitude(0, 100);
        
        Assert.Equal(0.0f, rms);
    }
}
