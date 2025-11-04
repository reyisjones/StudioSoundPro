using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.Core.Tests.Transport;

[MemoryDiagnoser]
[SimpleJob]
public class TransportBenchmarks
{
    private Clock _clock = null!;
    private StudioSoundPro.Core.Transport.Transport _transport = null!;
    private Metronome _metronome = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _clock = new Clock(48000, 120.0, new TimeSignature(4, 4));
        _transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        _metronome = new Metronome(_clock);
    }

    [Benchmark]
    public void ClockAdvance()
    {
        // Simulate one audio buffer advance (512 samples)
        _clock.Advance(512);
    }
    
    [Benchmark]
    public void TransportAdvance()
    {
        // Simulate transport advancing with one audio buffer
        _transport.Advance(512);
    }
    
    [Benchmark]
    public void ClockTimeToMusicalPosition()
    {
        // Test time-to-musical conversion performance
        _clock.TimeToMusicalPosition(5.0);
    }
    
    [Benchmark]
    public void ClockMusicalPositionToTime()
    {
        // Test musical-to-time conversion performance
        _clock.MusicalPositionToTime(new MusicalPosition(10, 2, 240));
    }
    
    [Benchmark]
    public void MetronomeGenerateBuffer()
    {
        // Test metronome buffer generation (512 samples)
        var buffer = new float[512];
        _metronome.GenerateBuffer(buffer, 512);
    }
    
    [Benchmark]
    public void ClockSampleToTimeConversion()
    {
        // Test sample-to-time conversion at various positions
        _clock.SamplesToTime(240000); // 5 seconds at 48kHz
    }
    
    [Benchmark]
    public void ClockTimeToSampleConversion()
    {
        // Test time-to-sample conversion
        _clock.TimeToSamples(5.0);
    }
    
    [Benchmark]
    public void TransportStateOperations()
    {
        // Test rapid state changes
        _transport.Play();
        _transport.Pause();
        _transport.Stop();
    }
}

// Program to run benchmarks
public class BenchmarkProgram
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<TransportBenchmarks>();
    }
}