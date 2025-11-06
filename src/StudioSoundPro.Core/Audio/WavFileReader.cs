using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Pure C# WAV file reader supporting PCM formats
/// </summary>
public class WavFileReader : IAudioFileReader
{
    private Stream? _stream;
    private bool _ownsStream;
    private AudioFileInfo? _info;
    private long _dataStartPosition;
    private int _dataChunkSize;
    private bool _disposed;

    public AudioFileInfo Info => _info ?? throw new InvalidOperationException("File not opened");

    public void Open(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath);

        var stream = File.OpenRead(filePath);
        _ownsStream = true;
        Open(stream);
    }

    public void Open(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _info = ReadWavHeader(stream);
    }

    public float[] ReadAllSamples()
    {
        if (_stream == null || _info == null)
            throw new InvalidOperationException("File not opened");

        _stream.Position = _dataStartPosition;

        int totalSamples = (int)_info.TotalSamples * _info.Channels;
        var samples = new float[totalSamples];

        ReadSamplesFromStream(_stream, samples, _info.BitsPerSample, _info.Channels);

        return samples;
    }

    public async Task<float[]> ReadAllSamplesAsync()
    {
        return await Task.Run(() => ReadAllSamples());
    }

    private AudioFileInfo ReadWavHeader(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        // Read RIFF header
        var riffId = new string(reader.ReadChars(4));
        if (riffId != "RIFF")
            throw new InvalidDataException("Not a valid WAV file (missing RIFF header)");

        var fileSize = reader.ReadInt32();
        
        var waveId = new string(reader.ReadChars(4));
        if (waveId != "WAVE")
            throw new InvalidDataException("Not a valid WAV file (missing WAVE header)");

        // Find fmt chunk
        AudioFileInfo info = new();
        bool foundFmt = false;
        bool foundData = false;

        while (stream.Position < stream.Length && (!foundFmt || !foundData))
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();
            var chunkStart = stream.Position;

            if (chunkId == "fmt ")
            {
                var audioFormat = reader.ReadInt16(); // 1 = PCM, 3 = IEEE float
                info.Channels = reader.ReadInt16();
                info.SampleRate = reader.ReadInt32();
                var byteRate = reader.ReadInt32();
                var blockAlign = reader.ReadInt16();
                info.BitsPerSample = reader.ReadInt16();

                if (audioFormat != 1 && audioFormat != 3)
                    throw new NotSupportedException($"Unsupported audio format: {audioFormat} (only PCM and IEEE float are supported)");

                info.Format = audioFormat == 1 ? "PCM" : "IEEE Float";
                foundFmt = true;
            }
            else if (chunkId == "data")
            {
                _dataStartPosition = stream.Position;
                _dataChunkSize = chunkSize;
                
                int bytesPerSample = info.BitsPerSample / 8;
                int totalSamplesInData = chunkSize / bytesPerSample;
                info.TotalSamples = totalSamplesInData / info.Channels;
                info.Duration = TimeSpan.FromSeconds((double)info.TotalSamples / info.SampleRate);
                
                foundData = true;
            }

            // Skip to next chunk
            stream.Position = chunkStart + chunkSize;
        }

        if (!foundFmt)
            throw new InvalidDataException("WAV file missing fmt chunk");
        if (!foundData)
            throw new InvalidDataException("WAV file missing data chunk");

        return info;
    }

    private void ReadSamplesFromStream(Stream stream, float[] output, int bitsPerSample, int channels)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        
        int sampleCount = output.Length;

        switch (bitsPerSample)
        {
            case 16:
                // 16-bit PCM: -32768 to 32767
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = reader.ReadInt16();
                    output[i] = sample / 32768.0f;
                }
                break;

            case 24:
                // 24-bit PCM: -8388608 to 8388607
                for (int i = 0; i < sampleCount; i++)
                {
                    byte b1 = reader.ReadByte();
                    byte b2 = reader.ReadByte();
                    byte b3 = reader.ReadByte();
                    
                    int sample = (b3 << 16) | (b2 << 8) | b1;
                    
                    // Sign extend if negative
                    if ((sample & 0x800000) != 0)
                        sample |= unchecked((int)0xFF000000);
                    
                    output[i] = sample / 8388608.0f;
                }
                break;

            case 32:
                // Could be 32-bit PCM or 32-bit float
                // Try to detect based on format chunk
                if (_info?.Format == "IEEE Float")
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        output[i] = reader.ReadSingle();
                    }
                }
                else
                {
                    // 32-bit PCM
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int sample = reader.ReadInt32();
                        output[i] = sample / 2147483648.0f;
                    }
                }
                break;

            default:
                throw new NotSupportedException($"Unsupported bit depth: {bitsPerSample}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsStream && _stream != null)
        {
            _stream.Dispose();
        }

        _stream = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
