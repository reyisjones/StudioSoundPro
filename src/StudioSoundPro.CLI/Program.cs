using StudioSoundPro.AudioIO.PortAudio;
using StudioSoundPro.Core.Audio;
using StudioSoundPro.Core.Transport;

namespace StudioSoundPro.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("StudioSoundPro CLI - Audio & Transport Manager");
        Console.WriteLine("==============================================");

        if (args.Length > 0 && args[0] == "devices")
        {
            ListAudioDevices();
        }
        else if (args.Length > 0 && args[0] == "test-tone")
        {
            var deviceId = args.Length > 1 && int.TryParse(args[1], out var id) ? id : -1;
            TestSineTone(deviceId);
        }
        else if (args.Length > 0 && args[0] == "transport")
        {
            TestTransport();
        }
        else if (args.Length > 0 && args[0] == "clock")
        {
            TestClock();
        }
        else if (args.Length > 0 && args[0] == "metronome")
        {
            TestMetronome();
        }
        else
        {
            ShowHelp();
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  StudioSoundPro.CLI devices          - List all available audio devices");
        Console.WriteLine("  StudioSoundPro.CLI test-tone [id]   - Test sine tone on device (default device if no ID specified)");
        Console.WriteLine("  StudioSoundPro.CLI transport        - Test transport controls (play/pause/stop)");
        Console.WriteLine("  StudioSoundPro.CLI clock            - Test clock timing calculations");
        Console.WriteLine("  StudioSoundPro.CLI metronome        - Test metronome with transport");
        Console.WriteLine();
    }

    static void ListAudioDevices()
    {
        var deviceService = new PortAudioDeviceService();

        try
        {
            deviceService.Initialize();
            var devices = deviceService.GetAvailableDevices();

            Console.WriteLine("Available Audio Devices:");
            Console.WriteLine("-----------------------");

            foreach (var device in devices)
            {
                var flags = new List<string>();
                if (device.IsDefaultInput) flags.Add("DEFAULT IN");
                if (device.IsDefaultOutput) flags.Add("DEFAULT OUT");
                
                var flagsText = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
                
                Console.WriteLine($"ID: {device.Id:D2} | {device.Name}{flagsText}");
                Console.WriteLine($"      Input channels: {device.MaxInputChannels}, Output channels: {device.MaxOutputChannels}");
                Console.WriteLine($"      Default sample rate: {device.DefaultSampleRate:F0} Hz");
                Console.WriteLine();
            }

            var defaultOutput = deviceService.GetDefaultOutputDevice();
            Console.WriteLine($"Default output device: {defaultOutput?.Name ?? "None"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing devices: {ex.Message}");
        }
        finally
        {
            deviceService.Dispose();
        }
    }

    static void TestSineTone(int deviceId = -1)
    {
        var deviceService = new PortAudioDeviceService();
        var audioEngine = new PortAudioEngine();
        var sineGenerator = new SineGenerator();

        try
        {
            deviceService.Initialize();
            
            var outputDevice = deviceId >= 0 
                ? deviceService.GetDeviceById(deviceId) 
                : deviceService.GetDefaultOutputDevice();

            if (outputDevice == null)
            {
                Console.WriteLine($"Error: Could not find audio device {(deviceId >= 0 ? $"with ID {deviceId}" : "(default)")}");
                return;
            }

            Console.WriteLine($"Testing sine tone on device: {outputDevice.Name}");
            Console.WriteLine("Press any key to stop...");

            // Initialize components
            sineGenerator.Initialize(48000);
            sineGenerator.Frequency = 440; // A4
            sineGenerator.SetAmplitudeDb(-12); // -12 dBFS
            sineGenerator.IsEnabled = true;

            audioEngine.Initialize(outputDevice, 48000, 512, 2);

            // Start audio with callback
            audioEngine.Start((input, output, frameCount, channelCount) =>
            {
                sineGenerator.GenerateAudio(output, frameCount, channelCount);
                return !Console.KeyAvailable; // Stop when key is pressed
            });

            // Wait for key press
            Console.ReadKey(true);

            audioEngine.Stop();
            Console.WriteLine("\nSine tone test completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing sine tone: {ex.Message}");
        }
        finally
        {
            audioEngine.Dispose();
            deviceService.Dispose();
        }
    }

    static void TestTransport()
    {
        Console.WriteLine("Transport Control Test");
        Console.WriteLine("=====================");

        var clock = new Clock(48000); // 48 kHz
        var transport = new StudioSoundPro.Core.Transport.Transport(clock);

        // Subscribe to events
        transport.StateChanged += (sender, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Transport state: {e.PreviousState} -> {e.CurrentState}");
        };

        transport.PositionChanged += (sender, e) =>
        {
            if (e.SamplePosition % (48000 / 4) == 0) // Every quarter second
            {
                Console.WriteLine($"Position: {e.TimeSeconds:F2}s | {e.Bar}.{e.Beat}.{e.Tick:D3}");
            }
        };

        Console.WriteLine("Commands: [P]lay, [S]top, [R]ecord, [Space] Pause, [Q]uit");
        Console.WriteLine("Transport will advance simulation at 48kHz...");

        var running = true;
        var simulationTask = Task.Run(async () =>
        {
            while (running)
            {
                if (transport.IsRunning)
                {
                    transport.Advance(1200); // Simulate 25ms chunks (1200 samples at 48kHz)
                }
                await Task.Delay(25); // 25ms intervals
            }
        });

        while (running)
        {
            var key = Console.ReadKey(true);
            switch (char.ToUpper(key.KeyChar))
            {
                case 'P':
                    transport.PlayAsync().Wait();
                    break;
                case 'S':
                    transport.StopAsync().Wait();
                    break;
                case 'R':
                    transport.RecordAsync().Wait();
                    break;
                case ' ':
                    transport.PauseAsync().Wait();
                    break;
                case 'Q':
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid command. Use P/S/R/Space/Q");
                    break;
            }
        }

        transport.StopAsync().Wait();
        simulationTask.Wait();
        Console.WriteLine("Transport test completed.");
    }

    static void TestClock()
    {
        Console.WriteLine("Clock Timing Test");
        Console.WriteLine("================");

        var clock = new Clock(48000, 480); // 48 kHz, 480 ticks per quarter note

        Console.WriteLine($"Sample Rate: {clock.SampleRate} Hz");
        Console.WriteLine($"Ticks Per Quarter Note: {clock.TicksPerQuarterNote}");
        Console.WriteLine();

        // Test various tempos
        var tempos = new[] { 60.0, 120.0, 140.0, 180.0 };
        
        foreach (var tempo in tempos)
        {
            clock.Tempo = tempo;
            Console.WriteLine($"Tempo: {tempo} BPM");
            
            var beatLength = clock.GetBeatLengthInSamples();
            var barLength = clock.GetBarLengthInSamples();
            
            Console.WriteLine($"  Beat length: {beatLength} samples ({clock.SamplesToSeconds(beatLength):F3}s)");
            Console.WriteLine($"  Bar length: {barLength} samples ({clock.SamplesToSeconds(barLength):F3}s)");
            
            // Test conversion round-trips
            var testPositions = new long[] { 0, beatLength, barLength, barLength * 2 + beatLength };
            
            foreach (var pos in testPositions)
            {
                var (bar, beat, tick) = clock.SamplesToMusicalTime(pos);
                var converted = clock.MusicalTimeToSamples(bar, beat, tick);
                var timeSeconds = clock.SamplesToSeconds(pos);
                
                Console.WriteLine($"  {pos} samples -> {bar}.{beat}.{tick:D3} -> {converted} samples | {timeSeconds:F3}s");
            }
            Console.WriteLine();
        }

        // Test time signatures
        Console.WriteLine("Time Signature Tests:");
        clock.Tempo = 120.0;
        
        var signatures = new[] { (3, 4), (4, 4), (6, 8), (7, 8) };
        foreach (var (num, den) in signatures)
        {
            clock.TimeSignatureNumerator = num;
            clock.TimeSignatureDenominator = den;
            
            var barLength = clock.GetBarLengthInSamples();
            Console.WriteLine($"  {num}/{den}: Bar = {barLength} samples ({clock.SamplesToSeconds(barLength):F3}s)");
        }
    }

    static void TestMetronome()
    {
        Console.WriteLine("Metronome Test");
        Console.WriteLine("=============");

        var clock = new Clock(48000);
        var transport = new StudioSoundPro.Core.Transport.Transport(clock);
        var metronome = new Metronome();

        // Configure metronome
        metronome.IsEnabled = true;
        metronome.Volume = 0.3f;
        metronome.AccentFirstBeat = true;
        metronome.DownbeatFrequency = 1000.0f;
        metronome.BeatFrequency = 800.0f;
        metronome.ClickDurationMs = 50;

        Console.WriteLine($"Metronome configured:");
        Console.WriteLine($"  Enabled: {metronome.IsEnabled}");
        Console.WriteLine($"  Volume: {metronome.Volume}");
        Console.WriteLine($"  Accent first beat: {metronome.AccentFirstBeat}");
        Console.WriteLine($"  Downbeat frequency: {metronome.DownbeatFrequency} Hz");
        Console.WriteLine($"  Beat frequency: {metronome.BeatFrequency} Hz");
        Console.WriteLine($"  Click duration: {metronome.ClickDurationMs} ms");
        Console.WriteLine();

        // Test metronome buffer generation
        Console.WriteLine("Testing metronome click generation...");
        var buffer = new float[2400]; // 50ms at 48kHz
        
        // Generate clicks at beat boundaries
        var beatLength = clock.GetBeatLengthInSamples();
        Console.WriteLine($"Beat length: {beatLength} samples");

        for (int beat = 0; beat < 8; beat++)
        {
            var startPosition = beat * beatLength;
            Array.Clear(buffer);
            
            metronome.ProcessBuffer(buffer, buffer.Length, startPosition, clock);
            
            // Check if click was generated
            var hasClick = buffer.Any(sample => Math.Abs(sample) > 0.01f);
            var maxAmplitude = buffer.Max(sample => Math.Abs(sample));
            
            var (bar, beatNum, tick) = clock.SamplesToMusicalTime(startPosition);
            var isDownbeat = beatNum == 1;
            
            Console.WriteLine($"Beat {beat + 1} (Bar {bar}, Beat {beatNum}): " +
                            $"Click = {hasClick}, Max amplitude = {maxAmplitude:F3}" +
                            (isDownbeat ? " [DOWNBEAT]" : ""));
        }

        Console.WriteLine("\nMetronome test completed.");
    }
}
