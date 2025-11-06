using StudioSoundPro.Core.Tracks;

namespace StudioSoundPro.Tests.Tracks;

public class ClipTests
{
    [Fact]
    public void Constructor_CreatesClipWithDefaultName()
    {
        var clip = new Clip();
        
        Assert.NotEqual(Guid.Empty, clip.Id);
        Assert.Equal("New Clip", clip.Name);
        Assert.Equal(0, clip.StartPosition);
        Assert.Equal(0, clip.Length);
        Assert.Equal(0, clip.EndPosition);
    }

    [Fact]
    public void Constructor_CreatesClipWithCustomName()
    {
        var clip = new Clip("Test Clip");
        
        Assert.Equal("Test Clip", clip.Name);
    }

    [Fact]
    public void Name_CanBeChanged()
    {
        var clip = new Clip();
        string? changedProperty = null;
        clip.PropertyChanged += (_, e) => changedProperty = e.PropertyName;
        
        clip.Name = "Modified Name";
        
        Assert.Equal("Modified Name", clip.Name);
        Assert.Equal(nameof(clip.Name), changedProperty);
    }

    [Fact]
    public void StartPosition_CanBeSet()
    {
        var clip = new Clip { Length = 1000 };
        
        clip.StartPosition = 5000;
        
        Assert.Equal(5000, clip.StartPosition);
        Assert.Equal(6000, clip.EndPosition);
    }

    [Fact]
    public void StartPosition_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.StartPosition = -1);
    }

    [Fact]
    public void StartPosition_RaisesPropertyChanged()
    {
        var clip = new Clip();
        var changedProperties = new List<string>();
        clip.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        
        clip.StartPosition = 1000;
        
        Assert.Contains(nameof(clip.StartPosition), changedProperties);
        Assert.Contains(nameof(clip.EndPosition), changedProperties);
    }

    [Fact]
    public void Length_CanBeSet()
    {
        var clip = new Clip { StartPosition = 1000 };
        
        clip.Length = 5000;
        
        Assert.Equal(5000, clip.Length);
        Assert.Equal(6000, clip.EndPosition);
    }

    [Fact]
    public void Length_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.Length = -1);
    }

    [Fact]
    public void EndPosition_CalculatesCorrectly()
    {
        var clip = new Clip
        {
            StartPosition = 1000,
            Length = 2000
        };
        
        Assert.Equal(3000, clip.EndPosition);
    }

    [Fact]
    public void SourceOffset_CanBeSet()
    {
        var clip = new Clip();
        
        clip.SourceOffset = 500;
        
        Assert.Equal(500, clip.SourceOffset);
    }

    [Fact]
    public void SourceOffset_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.SourceOffset = -1);
    }

    [Fact]
    public void IsMuted_DefaultsToFalse()
    {
        var clip = new Clip();
        
        Assert.False(clip.IsMuted);
    }

    [Fact]
    public void IsMuted_CanBeToggled()
    {
        var clip = new Clip();
        
        clip.IsMuted = true;
        Assert.True(clip.IsMuted);
        
        clip.IsMuted = false;
        Assert.False(clip.IsMuted);
    }

    [Fact]
    public void Gain_DefaultsToOne()
    {
        var clip = new Clip();
        
        Assert.Equal(1.0f, clip.Gain);
    }

    [Fact]
    public void Gain_CanBeSet()
    {
        var clip = new Clip();
        
        clip.Gain = 0.5f;
        
        Assert.Equal(0.5f, clip.Gain);
    }

    [Fact]
    public void Gain_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.Gain = -0.1f);
    }

    [Fact]
    public void FadeInLength_DefaultsToZero()
    {
        var clip = new Clip();
        
        Assert.Equal(0, clip.FadeInLength);
    }

    [Fact]
    public void FadeInLength_CanBeSet()
    {
        var clip = new Clip();
        
        clip.FadeInLength = 1000;
        
        Assert.Equal(1000, clip.FadeInLength);
    }

    [Fact]
    public void FadeInLength_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.FadeInLength = -1);
    }

    [Fact]
    public void FadeOutLength_DefaultsToZero()
    {
        var clip = new Clip();
        
        Assert.Equal(0, clip.FadeOutLength);
    }

    [Fact]
    public void FadeOutLength_CanBeSet()
    {
        var clip = new Clip();
        
        clip.FadeOutLength = 1000;
        
        Assert.Equal(1000, clip.FadeOutLength);
    }

    [Fact]
    public void FadeOutLength_ThrowsOnNegative()
    {
        var clip = new Clip();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => clip.FadeOutLength = -1);
    }

    [Fact]
    public void Color_HasDefaultValue()
    {
        var clip = new Clip();
        
        Assert.Equal("#4A90E2", clip.Color);
    }

    [Fact]
    public void Color_CanBeChanged()
    {
        var clip = new Clip();
        
        clip.Color = "#FF5733";
        
        Assert.Equal("#FF5733", clip.Color);
    }

    [Fact]
    public void PropertyChanged_RaisedForAllProperties()
    {
        var clip = new Clip();
        var changedProperties = new List<string>();
        clip.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        
        clip.Name = "Test";
        clip.StartPosition = 100;
        clip.Length = 200;
        clip.SourceOffset = 50;
        clip.IsMuted = true;
        clip.Gain = 0.8f;
        clip.FadeInLength = 10;
        clip.FadeOutLength = 20;
        clip.Color = "#123456";
        
        Assert.Contains(nameof(clip.Name), changedProperties);
        Assert.Contains(nameof(clip.StartPosition), changedProperties);
        Assert.Contains(nameof(clip.Length), changedProperties);
        Assert.Contains(nameof(clip.SourceOffset), changedProperties);
        Assert.Contains(nameof(clip.IsMuted), changedProperties);
        Assert.Contains(nameof(clip.Gain), changedProperties);
        Assert.Contains(nameof(clip.FadeInLength), changedProperties);
        Assert.Contains(nameof(clip.FadeOutLength), changedProperties);
        Assert.Contains(nameof(clip.Color), changedProperties);
    }

    [Fact]
    public void PropertyChanged_IncludesClipReference()
    {
        var clip = new Clip();
        IClip? changedClip = null;
        clip.PropertyChanged += (_, e) => changedClip = e.Clip;
        
        clip.Name = "Test";
        
        Assert.Same(clip, changedClip);
    }

    [Fact]
    public void MultipleClips_HaveUniqueIds()
    {
        var clip1 = new Clip();
        var clip2 = new Clip();
        var clip3 = new Clip();
        
        Assert.NotEqual(clip1.Id, clip2.Id);
        Assert.NotEqual(clip1.Id, clip3.Id);
        Assert.NotEqual(clip2.Id, clip3.Id);
    }
}
