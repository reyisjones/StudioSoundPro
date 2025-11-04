using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using StudioSoundPro.Core.Transport;

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
        _clock = new Clock(48000);
        _clock.Tempo = 120.0;
        _transport = new StudioSoundPro.Core.Transport.Transport(_clock);
        _metronome = new Metronome();
        _metronome.IsEnabled = true;
    }

    [Benchmark]
    public void ClockSampleToSecondsConversion()
    {
        // Test sample-to-time conversion at various positions
        _clock.SamplesToSeconds(240000); // 5 seconds at 48kHz
    }
    
    [Benchmark]
    public void ClockSecondsToSampleConversion()
    {
        // Test time-to-sample conversion
        _clock.SecondsToSamples(5.0);
    }
    
    [Benchmark]
    public void ClockSamplesToMusicalTime()
    {
        // Test sample-to-musical conversion performance
        _clock.SamplesToMusicalTime(240000); // 5 seconds worth of samples
    }
    
    [Benchmark]
    public void ClockMusicalTimeToSamples()
    {
        // Test musical-to-sample conversion performance
        _clock.MusicalTimeToSamples(10, 2, 240);
    }
    
    [Benchmark]
    public async Task TransportStateOperations()
    {
        // Test rapid state changes
        await _transport.PlayAsync();
        await _transport.PauseAsync();
        await _transport.StopAsync();
    }
    
    [Benchmark]
    public void TransportPositionAccess()
    {
        // Test position access performance
        var position = _transport.Position;
        _transport.Position = position + 1000;
    }
    
    [Benchmark]
    public void MetronomeProcessBuffer()
    {
        // Test metronome buffer processing performance
        var buffer = new float[512];
        _metronome.ProcessBuffer(buffer, 512, 48000, _clock); // 1 second position
    }
}

// Program to run benchmarks
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<TransportBenchmarks>();
        Console.WriteLine($"Benchmark completed. Results saved to: {summary.ResultsDirectoryPath}");
    }
}
