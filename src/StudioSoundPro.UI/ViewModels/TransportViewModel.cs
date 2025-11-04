using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using StudioSoundPro.Core.Transport;
using CommunityToolkit.Mvvm.Input;

namespace StudioSoundPro.UI.ViewModels;

/// <summary>
/// View model for transport controls (play, pause, stop, record)
/// </summary>
public class TransportViewModel : INotifyPropertyChanged
{
    private readonly ITransport _transport;
    private readonly IClock _clock;
    private string _positionText = "00:00:00.000";
    private string _musicalPositionText = "1.1.000";
    private double _tempo = 120.0;
    private int _timeSignatureNumerator = 4;
    private int _timeSignatureDenominator = 4;
    private bool _isLooping = false;
    private string _loopRangeText = "Loop: Off";

    public TransportViewModel(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _clock = transport.Clock;

        // Subscribe to transport events
        _transport.StateChanged += OnTransportStateChanged;
        _transport.PositionChanged += OnTransportPositionChanged;
        _clock.TempoChanged += OnTempoChanged;
        _clock.TimeSignatureChanged += OnTimeSignatureChanged;

        // Initialize commands
        PlayCommand = new AsyncRelayCommand(PlayAsync, CanPlay);
        PauseCommand = new AsyncRelayCommand(PauseAsync, CanPause);
        StopCommand = new AsyncRelayCommand(StopAsync, CanStop);
        RecordCommand = new AsyncRelayCommand(RecordAsync, CanRecord);
        RewindCommand = new AsyncRelayCommand(RewindAsync);

        // Initialize display values
        UpdateDisplayValues();
    }

    #region Properties

    public TransportState State => _transport.State;

    public bool IsPlaying => _transport.State == TransportState.Playing;
    public bool IsPaused => _transport.State == TransportState.Paused;
    public bool IsStopped => _transport.State == TransportState.Stopped;
    public bool IsRecording => _transport.State == TransportState.Recording;
    public bool IsRunning => _transport.IsRunning;

    public string PositionText
    {
        get => _positionText;
        private set => SetProperty(ref _positionText, value);
    }

    public string MusicalPositionText
    {
        get => _musicalPositionText;
        private set => SetProperty(ref _musicalPositionText, value);
    }

    public double Tempo
    {
        get => _tempo;
        set
        {
            if (SetProperty(ref _tempo, value))
            {
                _clock.Tempo = value;
            }
        }
    }

    public int TimeSignatureNumerator
    {
        get => _timeSignatureNumerator;
        set
        {
            if (SetProperty(ref _timeSignatureNumerator, value))
            {
                _clock.TimeSignatureNumerator = value;
                UpdateMusicalPositionDisplay();
            }
        }
    }

    public int TimeSignatureDenominator
    {
        get => _timeSignatureDenominator;
        set
        {
            if (SetProperty(ref _timeSignatureDenominator, value))
            {
                _clock.TimeSignatureDenominator = value;
                UpdateMusicalPositionDisplay();
            }
        }
    }

    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            if (SetProperty(ref _isLooping, value))
            {
                _transport.IsLooping = value;
                UpdateLoopRangeDisplay();
            }
        }
    }

    public string LoopRangeText
    {
        get => _loopRangeText;
        private set => SetProperty(ref _loopRangeText, value);
    }

    public long Position
    {
        get => _transport.Position;
        set => _transport.Position = value;
    }

    public long LoopStart
    {
        get => _transport.LoopStart;
        set
        {
            _transport.LoopStart = value;
            UpdateLoopRangeDisplay();
        }
    }

    public long LoopEnd
    {
        get => _transport.LoopEnd;
        set
        {
            _transport.LoopEnd = value;
            UpdateLoopRangeDisplay();
        }
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand PlayCommand { get; }
    public IAsyncRelayCommand PauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand RecordCommand { get; }
    public IAsyncRelayCommand RewindCommand { get; }

    private async Task PlayAsync()
    {
        await _transport.PlayAsync();
    }

    private async Task PauseAsync()
    {
        await _transport.PauseAsync();
    }

    private async Task StopAsync()
    {
        await _transport.StopAsync();
    }

    private async Task RecordAsync()
    {
        await _transport.RecordAsync();
    }

    private async Task RewindAsync()
    {
        await _transport.RewindAsync();
    }

    private bool CanPlay() => _transport.State != TransportState.Playing;
    private bool CanPause() => _transport.State == TransportState.Playing || _transport.State == TransportState.Recording;
    private bool CanStop() => _transport.State != TransportState.Stopped;
    private bool CanRecord() => true; // Can always start recording

    #endregion

    #region Event Handlers

    private void OnTransportStateChanged(object? sender, TransportStateChangedEventArgs e)
    {
        // Update command availability
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RecordCommand.NotifyCanExecuteChanged();

        // Notify UI of state changes
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(IsRecording));
        OnPropertyChanged(nameof(IsRunning));
    }

    private void OnTransportPositionChanged(object? sender, TransportPositionChangedEventArgs e)
    {
        UpdateTimePositionDisplay(e.TimeSeconds);
        UpdateMusicalPositionDisplay(e.Bar, e.Beat, e.Tick);
        OnPropertyChanged(nameof(Position));
    }

    private void OnTempoChanged(object? sender, double newTempo)
    {
        _tempo = newTempo;
        OnPropertyChanged(nameof(Tempo));
        UpdateMusicalPositionDisplay();
    }

    private void OnTimeSignatureChanged(object? sender, (int numerator, int denominator) signature)
    {
        _timeSignatureNumerator = signature.numerator;
        _timeSignatureDenominator = signature.denominator;
        OnPropertyChanged(nameof(TimeSignatureNumerator));
        OnPropertyChanged(nameof(TimeSignatureDenominator));
        UpdateMusicalPositionDisplay();
    }

    #endregion

    #region Display Updates

    private void UpdateDisplayValues()
    {
        var currentPosition = _transport.Position;
        var timeSeconds = _clock.SamplesToSeconds(currentPosition);
        var (bar, beat, tick) = _clock.SamplesToMusicalTime(currentPosition);

        UpdateTimePositionDisplay(timeSeconds);
        UpdateMusicalPositionDisplay(bar, beat, tick);
        UpdateLoopRangeDisplay();

        _tempo = _clock.Tempo;
        _timeSignatureNumerator = _clock.TimeSignatureNumerator;
        _timeSignatureDenominator = _clock.TimeSignatureDenominator;
        _isLooping = _transport.IsLooping;

        OnPropertyChanged(nameof(Tempo));
        OnPropertyChanged(nameof(TimeSignatureNumerator));
        OnPropertyChanged(nameof(TimeSignatureDenominator));
        OnPropertyChanged(nameof(IsLooping));
    }

    private void UpdateTimePositionDisplay(double timeSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(timeSeconds);
        PositionText = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}:{timeSpan.Milliseconds:D3}";
    }

    private void UpdateMusicalPositionDisplay()
    {
        var currentPosition = _transport.Position;
        var (bar, beat, tick) = _clock.SamplesToMusicalTime(currentPosition);
        UpdateMusicalPositionDisplay(bar, beat, tick);
    }

    private void UpdateMusicalPositionDisplay(int bar, int beat, int tick)
    {
        MusicalPositionText = $"{bar}.{beat}.{tick:D3}";
    }

    private void UpdateLoopRangeDisplay()
    {
        if (_transport.IsLooping)
        {
            var startSeconds = _clock.SamplesToSeconds(_transport.LoopStart);
            var endSeconds = _clock.SamplesToSeconds(_transport.LoopEnd);
            var startTime = TimeSpan.FromSeconds(startSeconds);
            var endTime = TimeSpan.FromSeconds(endSeconds);
            
            LoopRangeText = $"Loop: {(int)startTime.TotalMinutes:D2}:{startTime.Seconds:D2}.{startTime.Milliseconds:D3} - {(int)endTime.TotalMinutes:D2}:{endTime.Seconds:D2}.{endTime.Milliseconds:D3}";
        }
        else
        {
            LoopRangeText = "Loop: Off";
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    public void Dispose()
    {
        _transport.StateChanged -= OnTransportStateChanged;
        _transport.PositionChanged -= OnTransportPositionChanged;
        _clock.TempoChanged -= OnTempoChanged;
        _clock.TimeSignatureChanged -= OnTimeSignatureChanged;
    }
}