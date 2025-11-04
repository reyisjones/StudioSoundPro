using StudioSoundPro.AudioIO.PortAudio;
using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("StudioSoundPro CLI - Audio Device Manager");
        Console.WriteLine("=====================================");

        if (args.Length > 0 && args[0] == "devices")
        {
            ListAudioDevices();
        }
        else if (args.Length > 0 && args[0] == "test-tone")
        {
            var deviceId = args.Length > 1 && int.TryParse(args[1], out var id) ? id : -1;
            TestSineTone(deviceId);
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
}
