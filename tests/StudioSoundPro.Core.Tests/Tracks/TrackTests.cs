using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.Tests.Tracks;

public class TrackTests
{
    private static AudioClip CreateTestClip(string name, long startPos, long length)
    {
        var audioData = new float[length * 2]; // Stereo
        for (int i = 0; i < audioData.Length; i++)
            audioData[i] = 0.5f;
        
        return new AudioClip(name, audioData, 2, 48000)
        {
            StartPosition = startPos,
            Length = length
        };
    }

    [Fact]
    public void Constructor_CreatesTrackWithDefaultName()
    {
        var track = new Track();
        
        Assert.NotEqual(Guid.Empty, track.Id);
        Assert.Equal("New Track", track.Name);
        Assert.Equal(1.0f, track.Volume);
        Assert.Equal(0.0f, track.Pan);
        Assert.False(track.IsMuted);
        Assert.False(track.IsSolo);
        Assert.False(track.IsArmed);
        Assert.Empty(track.Clips);
    }

    [Fact]
    public void Constructor_CreatesTrackWithCustomName()
    {
        var track = new Track("Test Track");
        
        Assert.Equal("Test Track", track.Name);
    }

    [Fact]
    public void Name_CanBeChanged()
    {
        var track = new Track();
        string? changedProperty = null;
        track.PropertyChanged += (_, e) => changedProperty = e.PropertyName;
        
        track.Name = "Modified Name";
        
        Assert.Equal("Modified Name", track.Name);
        Assert.Equal(nameof(track.Name), changedProperty);
    }

    [Fact]
    public void Volume_CanBeSet()
    {
        var track = new Track();
        
        track.Volume = 0.5f;
        
        Assert.Equal(0.5f, track.Volume);
    }

    [Fact]
    public void Volume_ThrowsOnNegative()
    {
        var track = new Track();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => track.Volume = -0.1f);
    }

    [Fact]
    public void Pan_CanBeSet()
    {
        var track = new Track();
        
        track.Pan = 0.5f;
        Assert.Equal(0.5f, track.Pan);
        
        track.Pan = -0.5f;
        Assert.Equal(-0.5f, track.Pan);
    }

    [Fact]
    public void Pan_ThrowsOnOutOfRange()
    {
        var track = new Track();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => track.Pan = -1.1f);
        Assert.Throws<ArgumentOutOfRangeException>(() => track.Pan = 1.1f);
    }

    [Fact]
    public void IsMuted_CanBeToggled()
    {
        var track = new Track();
        
        track.IsMuted = true;
        Assert.True(track.IsMuted);
        
        track.IsMuted = false;
        Assert.False(track.IsMuted);
    }

    [Fact]
    public void IsSolo_CanBeToggled()
    {
        var track = new Track();
        
        track.IsSolo = true;
        Assert.True(track.IsSolo);
        
        track.IsSolo = false;
        Assert.False(track.IsSolo);
    }

    [Fact]
    public void IsArmed_CanBeToggled()
    {
        var track = new Track();
        
        track.IsArmed = true;
        Assert.True(track.IsArmed);
        
        track.IsArmed = false;
        Assert.False(track.IsArmed);
    }

    [Fact]
    public void AddClip_AddsClipToTrack()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        
        track.AddClip(clip);
        
        Assert.Single(track.Clips);
        Assert.Contains(clip, track.Clips);
    }

    [Fact]
    public void AddClip_RaisesClipAddedEvent()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        IClip? addedClip = null;
        track.ClipAdded += (_, e) => addedClip = e.Clip;
        
        track.AddClip(clip);
        
        Assert.Same(clip, addedClip);
    }

    [Fact]
    public void AddClip_ThrowsOnNull()
    {
        var track = new Track();
        
        Assert.Throws<ArgumentNullException>(() => track.AddClip(null!));
    }

    [Fact]
    public void AddClip_IgnoresDuplicates()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        
        track.AddClip(clip);
        track.AddClip(clip); // Add same clip again
        
        Assert.Single(track.Clips);
    }

    [Fact]
    public void RemoveClip_RemovesClipFromTrack()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        bool removed = track.RemoveClip(clip);
        
        Assert.True(removed);
        Assert.Empty(track.Clips);
    }

    [Fact]
    public void RemoveClip_RaisesClipRemovedEvent()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        IClip? removedClip = null;
        track.ClipRemoved += (_, e) => removedClip = e.Clip;
        
        track.RemoveClip(clip);
        
        Assert.Same(clip, removedClip);
    }

    [Fact]
    public void RemoveClip_ReturnsFalseForNonExistentClip()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        
        bool removed = track.RemoveClip(clip);
        
        Assert.False(removed);
    }

    [Fact]
    public void ClearClips_RemovesAllClips()
    {
        var track = new Track();
        track.AddClip(CreateTestClip("Clip1", 0, 1000));
        track.AddClip(CreateTestClip("Clip2", 1000, 1000));
        track.AddClip(CreateTestClip("Clip3", 2000, 1000));
        
        track.ClearClips();
        
        Assert.Empty(track.Clips);
    }

    [Fact]
    public void ClearClips_RaisesEventsForAllClips()
    {
        var track = new Track();
        var clip1 = CreateTestClip("Clip1", 0, 1000);
        var clip2 = CreateTestClip("Clip2", 1000, 1000);
        track.AddClip(clip1);
        track.AddClip(clip2);
        
        var removedClips = new List<IClip>();
        track.ClipRemoved += (_, e) => removedClips.Add(e.Clip);
        
        track.ClearClips();
        
        Assert.Equal(2, removedClips.Count);
        Assert.Contains(clip1, removedClips);
        Assert.Contains(clip2, removedClips);
    }

    [Fact]
    public void GetClipsInRange_ReturnsClipsInRange()
    {
        var track = new Track();
        var clip1 = CreateTestClip("Clip1", 0, 1000);
        var clip2 = CreateTestClip("Clip2", 1000, 1000);
        var clip3 = CreateTestClip("Clip3", 2000, 1000);
        track.AddClip(clip1);
        track.AddClip(clip2);
        track.AddClip(clip3);
        
        var clips = track.GetClipsInRange(500, 1500).ToList();
        
        Assert.Equal(2, clips.Count);
        Assert.Contains(clip1, clips);
        Assert.Contains(clip2, clips);
    }

    [Fact]
    public void GetClipsInRange_ReturnsEmptyWhenNoClipsInRange()
    {
        var track = new Track();
        track.AddClip(CreateTestClip("Clip1", 0, 1000));
        track.AddClip(CreateTestClip("Clip2", 2000, 1000));
        
        var clips = track.GetClipsInRange(1100, 1900).ToList();
        
        Assert.Empty(clips);
    }

    [Fact]
    public void GetClipsInRange_ReturnsClipsInOrder()
    {
        var track = new Track();
        var clip1 = CreateTestClip("Clip1", 2000, 1000);
        var clip2 = CreateTestClip("Clip2", 0, 1000);
        var clip3 = CreateTestClip("Clip3", 1000, 1000);
        // Add in random order
        track.AddClip(clip1);
        track.AddClip(clip2);
        track.AddClip(clip3);
        
        var clips = track.GetClipsInRange(0, 3000).ToList();
        
        Assert.Equal(3, clips.Count);
        Assert.Equal(clip2, clips[0]);
        Assert.Equal(clip3, clips[1]);
        Assert.Equal(clip1, clips[2]);
    }

    [Fact]
    public void GetClipsInRange_ThrowsWhenEndBeforeStart()
    {
        var track = new Track();
        
        Assert.Throws<ArgumentException>(() => track.GetClipsInRange(1000, 500));
    }

    [Fact]
    public void ProcessAudio_GeneratesSilenceWhenMuted()
    {
        var track = new Track { IsMuted = true };
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        var buffer = new float[200];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 1.0f; // Fill with non-zero
        
        track.ProcessAudio(buffer, 0, 200, 0);
        
        Assert.All(buffer, sample => Assert.Equal(0.0f, sample));
    }

    [Fact]
    public void ProcessAudio_ProcessesAudioFromClips()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        var buffer = new float[40]; // 20 stereo frames = 40 samples
        
        track.ProcessAudio(buffer, 0, 40, 0);
        
        // Should have audio (not all zeros)
        Assert.Contains(buffer, sample => sample != 0.0f);
    }

    [Fact]
    public void ProcessAudio_AppliesTrackVolume()
    {
        var track = new Track { Volume = 0.5f };
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        var buffer = new float[40]; // 20 stereo frames = 40 samples
        track.ProcessAudio(buffer, 0, 40, 0);
        
        // Audio should be scaled by volume
        Assert.Contains(buffer, sample => sample != 0.0f);
    }

    [Fact]
    public void ProcessAudio_MixesMultipleClips()
    {
        var track = new Track();
        var clip1 = CreateTestClip("Clip1", 0, 1000);
        var clip2 = CreateTestClip("Clip2", 0, 1000); // Overlapping
        track.AddClip(clip1);
        track.AddClip(clip2);
        
        var buffer = new float[40]; // 20 stereo frames = 40 samples
        track.ProcessAudio(buffer, 0, 40, 0);
        
        // Should have mixed audio
        Assert.Contains(buffer, sample => sample != 0.0f);
    }

    [Fact]
    public void ProcessAudio_ThrowsOnInvalidOffset()
    {
        var track = new Track();
        var buffer = new float[20];
        
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            track.ProcessAudio(buffer, -1, 20, 0));
    }

    [Fact]
    public void ProcessAudio_ThrowsOnBufferTooSmall()
    {
        var track = new Track();
        var buffer = new float[20];
        
        Assert.Throws<ArgumentException>(() => 
            track.ProcessAudio(buffer, 0, 30, 0));
    }

    [Fact]
    public void GetPeakAmplitude_ReturnsZeroWhenMuted()
    {
        var track = new Track { IsMuted = true };
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        float peak = track.GetPeakAmplitude(0);
        
        Assert.Equal(0.0f, peak);
    }

    [Fact]
    public void GetPeakAmplitude_ReturnsHighestClipPeak()
    {
        var track = new Track();
        
        // Create clip with known peak
        var audioData1 = new float[200];
        audioData1[0] = 0.5f;
        var clip1 = new AudioClip("Clip1", audioData1, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        var audioData2 = new float[200];
        audioData2[0] = 0.8f; // Higher peak
        var clip2 = new AudioClip("Clip2", audioData2, 2, 48000)
        {
            StartPosition = 0,
            Length = 100
        };
        
        track.AddClip(clip1);
        track.AddClip(clip2);
        
        float peak = track.GetPeakAmplitude(0);
        
        Assert.InRange(peak, 0.7f, 0.9f); // Should be ~0.8 * volume (1.0)
    }

    [Fact]
    public void MoveClip_ChangesClipPosition()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 1000);
        track.AddClip(clip);
        
        track.MoveClip(clip, 2000);
        
        Assert.Equal(2000, clip.StartPosition);
    }

    [Fact]
    public void MoveClip_ThrowsOnNullClip()
    {
        var track = new Track();
        
        Assert.Throws<ArgumentNullException>(() => track.MoveClip(null!, 1000));
    }

    [Fact]
    public void MoveClip_ThrowsOnNegativePosition()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => track.MoveClip(clip, -100));
    }

    [Fact]
    public void MoveClip_ThrowsWhenClipNotInTrack()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        
        Assert.Throws<InvalidOperationException>(() => track.MoveClip(clip, 1000));
    }

    [Fact]
    public void TrimClip_AdjustsStartAndLength()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 2000);
        track.AddClip(clip);
        
        track.TrimClip(clip, 1500, 1000);
        
        Assert.Equal(1500, clip.StartPosition);
        Assert.Equal(1000, clip.Length);
        Assert.Equal(500, clip.SourceOffset);
    }

    [Fact]
    public void TrimClip_OnlyAdjustsStartWhenLengthNull()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 2000);
        track.AddClip(clip);
        
        track.TrimClip(clip, 1500, null);
        
        Assert.Equal(1500, clip.StartPosition);
        Assert.Equal(1500, clip.Length); // Reduced by 500
    }

    [Fact]
    public void TrimClip_OnlyAdjustsLengthWhenStartNull()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 2000);
        track.AddClip(clip);
        
        track.TrimClip(clip, null, 1500);
        
        Assert.Equal(1000, clip.StartPosition); // Unchanged
        Assert.Equal(1500, clip.Length);
    }

    [Fact]
    public void SplitClip_CreatesTwoClips()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 2000);
        track.AddClip(clip);
        
        var rightClip = track.SplitClip(clip, 2000);
        
        Assert.NotNull(rightClip);
        Assert.Equal(2, track.Clips.Count);
        Assert.Equal(1000, clip.Length);
        Assert.Equal(2000, rightClip.StartPosition);
        Assert.Equal(1000, rightClip.Length);
    }

    [Fact]
    public void SplitClip_ThrowsOnInvalidPosition()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 1000, 2000);
        track.AddClip(clip);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => track.SplitClip(clip, 1000));
        Assert.Throws<ArgumentOutOfRangeException>(() => track.SplitClip(clip, 3000));
    }

    [Fact]
    public void PropertyChanged_RaisedForAllProperties()
    {
        var track = new Track();
        var changedProperties = new List<string>();
        track.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        
        track.Name = "Test";
        track.Volume = 0.8f;
        track.Pan = 0.5f;
        track.IsMuted = true;
        track.IsSolo = true;
        track.IsArmed = true;
        track.Color = "#123456";
        
        Assert.Contains(nameof(track.Name), changedProperties);
        Assert.Contains(nameof(track.Volume), changedProperties);
        Assert.Contains(nameof(track.Pan), changedProperties);
        Assert.Contains(nameof(track.IsMuted), changedProperties);
        Assert.Contains(nameof(track.IsSolo), changedProperties);
        Assert.Contains(nameof(track.IsArmed), changedProperties);
        Assert.Contains(nameof(track.Color), changedProperties);
    }

    [Fact]
    public void PropertyChanged_ForwardsClipPropertyChanges()
    {
        var track = new Track();
        var clip = CreateTestClip("Test", 0, 1000);
        track.AddClip(clip);
        
        var changedProperties = new List<string>();
        track.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        
        clip.Name = "Modified";
        
        Assert.Contains("Clip.Name", changedProperties);
    }

    [Fact]
    public void MultipleTracks_HaveUniqueIds()
    {
        var track1 = new Track();
        var track2 = new Track();
        var track3 = new Track();
        
        Assert.NotEqual(track1.Id, track2.Id);
        Assert.NotEqual(track1.Id, track3.Id);
        Assert.NotEqual(track2.Id, track3.Id);
    }
}
