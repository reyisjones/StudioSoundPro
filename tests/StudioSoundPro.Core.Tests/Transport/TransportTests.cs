using FluentAssertions;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Tests.Transport;

public class TransportTests
{
    private readonly IClock _clock;

    public TransportTests()
    {
        _clock = new Clock(48000);
    }

    [Fact]
    public void Constructor_SetsInitialState()
    {
        // Act
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Assert
        transport.State.Should().Be(TransportState.Stopped);
        transport.Position.Should().Be(0);
        transport.IsRunning.Should().BeFalse();
        transport.IsLooping.Should().BeFalse();
        transport.Clock.Should().Be(_clock);
    }

    [Fact]
    public void Constructor_NullClock_ThrowsException()
    {
        // Act & Assert
        var action = () => new StudioSoundPro.Core.Transport.Transport(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PlayAsync_FromStopped_ChangesStateToPlaying()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        TransportStateChangedEventArgs? eventArgs = null;
        transport.StateChanged += (_, args) => eventArgs = args;

        // Act
        await transport.PlayAsync();

        // Assert
        transport.State.Should().Be(TransportState.Playing);
        transport.IsRunning.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.PreviousState.Should().Be(TransportState.Stopped);
        eventArgs.CurrentState.Should().Be(TransportState.Playing);
    }

    [Fact]
    public async Task PauseAsync_FromPlaying_ChangeStateToPaused()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        await transport.PlayAsync();
        TransportStateChangedEventArgs? eventArgs = null;
        transport.StateChanged += (_, args) => eventArgs = args;

        // Act
        await transport.PauseAsync();

        // Assert
        transport.State.Should().Be(TransportState.Paused);
        transport.IsRunning.Should().BeFalse();
        eventArgs.Should().NotBeNull();
        eventArgs!.PreviousState.Should().Be(TransportState.Playing);
        eventArgs.CurrentState.Should().Be(TransportState.Paused);
    }

    [Fact]
    public async Task StopAsync_FromPlaying_ChangesStateToStopped()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        await transport.PlayAsync();
        TransportStateChangedEventArgs? eventArgs = null;
        transport.StateChanged += (_, args) => eventArgs = args;

        // Act
        await transport.StopAsync();

        // Assert
        transport.State.Should().Be(TransportState.Stopped);
        transport.IsRunning.Should().BeFalse();
        eventArgs.Should().NotBeNull();
        eventArgs!.PreviousState.Should().Be(TransportState.Playing);
        eventArgs.CurrentState.Should().Be(TransportState.Stopped);
    }

    [Fact]
    public async Task RecordAsync_FromStopped_ChangesStateToRecording()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        TransportStateChangedEventArgs? eventArgs = null;
        transport.StateChanged += (_, args) => eventArgs = args;

        // Act
        await transport.RecordAsync();

        // Assert
        transport.State.Should().Be(TransportState.Recording);
        transport.IsRunning.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.PreviousState.Should().Be(TransportState.Stopped);
        eventArgs.CurrentState.Should().Be(TransportState.Recording);
    }

    [Fact]
    public async Task SeekAsync_ValidPosition_UpdatesPosition()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        const long seekPosition = 48000; // 1 second at 48kHz
        TransportPositionChangedEventArgs? eventArgs = null;
        transport.PositionChanged += (_, args) => eventArgs = args;

        // Act
        await transport.SeekAsync(seekPosition);

        // Assert
        transport.Position.Should().Be(seekPosition);
        eventArgs.Should().NotBeNull();
        eventArgs!.SamplePosition.Should().Be(seekPosition);
        eventArgs.TimeSeconds.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public async Task SeekAsync_NegativePosition_ThrowsException()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Act & Assert
        var action = async () => await transport.SeekAsync(-1000);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task RewindAsync_SetsPositionToZero()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        await transport.SeekAsync(48000); // Move to 1 second

        // Act
        await transport.RewindAsync();

        // Assert
        transport.Position.Should().Be(0);
    }

    [Fact]
    public void Advance_WhilePlaying_UpdatesPosition()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        transport.PlayAsync().Wait();
        const int sampleCount = 1024;
        TransportPositionChangedEventArgs? eventArgs = null;
        transport.PositionChanged += (_, args) => eventArgs = args;

        // Act
        transport.Advance(sampleCount);

        // Assert
        transport.Position.Should().Be(sampleCount);
        eventArgs.Should().NotBeNull();
        eventArgs!.SamplePosition.Should().Be(sampleCount);
    }

    [Fact]
    public void Advance_WhileStopped_DoesNotUpdatePosition()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        const int sampleCount = 1024;
        var initialPosition = transport.Position;

        // Act
        transport.Advance(sampleCount);

        // Assert
        transport.Position.Should().Be(initialPosition);
    }

    [Fact]
    public void Advance_WithLooping_WrapsAroundAtLoopEnd()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        transport.IsLooping = true;
        transport.LoopStart = 0;
        transport.LoopEnd = 1000;
        transport.Position = 900; // Near end of loop
        transport.PlayAsync().Wait();

        // Act
        transport.Advance(200); // This should wrap around

        // Assert
        transport.Position.Should().Be(100); // 900 + 200 - 1000 = 100
    }

    [Fact]
    public void Position_SetValidValue_UpdatesAndFiresEvent()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        const long newPosition = 24000;
        TransportPositionChangedEventArgs? eventArgs = null;
        transport.PositionChanged += (_, args) => eventArgs = args;

        // Act
        transport.Position = newPosition;

        // Assert
        transport.Position.Should().Be(newPosition);
        eventArgs.Should().NotBeNull();
        eventArgs!.SamplePosition.Should().Be(newPosition);
    }

    [Fact]
    public void Position_SetNegativeValue_ThrowsException()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Act & Assert
        var action = () => transport.Position = -1000;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoopStart_SetValidValue_UpdatesLoopBounds()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        const long loopStart = 1000;

        // Act
        transport.LoopStart = loopStart;

        // Assert
        transport.LoopStart.Should().Be(loopStart);
        transport.LoopEnd.Should().BeGreaterThan(loopStart); // Should auto-adjust
    }

    [Fact]
    public void LoopEnd_SetValidValue_UpdatesLoopBounds()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        const long loopEnd = 5000;

        // Act
        transport.LoopEnd = loopEnd;

        // Assert
        transport.LoopEnd.Should().Be(loopEnd);
        transport.LoopStart.Should().BeLessThan(loopEnd); // Should auto-adjust if needed
    }

    [Fact]
    public void LoopStart_SetNegativeValue_ThrowsException()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Act & Assert
        var action = () => transport.LoopStart = -1000;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoopEnd_SetNegativeValue_ThrowsException()
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Act & Assert
        var action = () => transport.LoopEnd = -1000;
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(TransportState.Stopped, false)]
    [InlineData(TransportState.Paused, false)]
    [InlineData(TransportState.Playing, true)]
    [InlineData(TransportState.Recording, true)]
    public async Task IsRunning_ReflectsCorrectState(TransportState targetState, bool expectedIsRunning)
    {
        // Arrange
        var transport = new StudioSoundPro.Core.Transport.Transport(_clock);

        // Act
        switch (targetState)
        {
            case TransportState.Playing:
                await transport.PlayAsync();
                break;
            case TransportState.Recording:
                await transport.RecordAsync();
                break;
            case TransportState.Paused:
                await transport.PlayAsync();
                await transport.PauseAsync();
                break;
            case TransportState.Stopped:
                // Already stopped by default
                break;
        }

        // Assert
        transport.IsRunning.Should().Be(expectedIsRunning);
    }
}