namespace StudioSoundPro.Core.Tracks;

/// <summary>
/// Represents an audio clip with sample data
/// </summary>
public class AudioClip : Clip, IAudioClip
{
    private readonly float[] _audioData;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly object _audioLock = new();

    /// <summary>
    /// Creates a new audio clip with the specified audio data
    /// </summary>
    /// <param name="name">Clip name</param>
    /// <param name="audioData">Interleaved audio samples</param>
    /// <param name="channels">Number of audio channels</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    public AudioClip(string name, float[] audioData, int channels, int sampleRate)
        : base(name)
    {
        if (audioData == null || audioData.Length == 0)
            throw new ArgumentException("Audio data cannot be null or empty", nameof(audioData));
        if (channels < 1)
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be at least 1");
        if (sampleRate < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be at least 1");
        if (audioData.Length % channels != 0)
            throw new ArgumentException($"Audio data length ({audioData.Length}) must be divisible by channel count ({channels})", nameof(audioData));

        _audioData = audioData;
        _channels = channels;
        _sampleRate = sampleRate;
        
        // Set clip length to match audio data
        Length = audioData.Length / channels;
    }

    /// <summary>
    /// Creates a new audio clip with pre-allocated buffer
    /// </summary>
    /// <param name="name">Clip name</param>
    /// <param name="lengthInSamples">Length in sample frames</param>
    /// <param name="channels">Number of audio channels</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    public AudioClip(string name, long lengthInSamples, int channels, int sampleRate)
        : base(name)
    {
        if (lengthInSamples < 0)
            throw new ArgumentOutOfRangeException(nameof(lengthInSamples), "Length must be non-negative");
        if (channels < 1)
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be at least 1");
        if (sampleRate < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be at least 1");

        _audioData = new float[lengthInSamples * channels];
        _channels = channels;
        _sampleRate = sampleRate;
        Length = lengthInSamples;
    }

    public int Channels => _channels;

    public int SampleRate => _sampleRate;

    public float[]? AudioData
    {
        get
        {
            lock (_audioLock)
            {
                return _audioData;
            }
        }
        set
        {
            // Read-only for this implementation
            throw new NotSupportedException("AudioData is read-only after construction");
        }
    }

    public int ReadSamples(float[] buffer, int offset, int count, long position)
    {
        if (offset < 0 || offset >= buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of buffer range");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
        if (offset + count > buffer.Length)
            throw new ArgumentException("Buffer is too small for the requested offset and count");
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative");
        if (count % _channels != 0)
            throw new ArgumentException($"Count ({count}) must be divisible by channel count ({_channels})");

        if (IsMuted || count == 0)
        {
            Array.Clear(buffer, offset, count);
            return 0;
        }

        lock (_audioLock)
        {
            int frameCount = count / _channels;
            
            // Calculate relative position within clip (in frames)
            long relativePosition = position - StartPosition;
            
            // Check if position is outside clip bounds
            if (relativePosition < 0 || relativePosition >= Length)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }

            // Calculate source position accounting for source offset
            long sourcePosition = relativePosition + SourceOffset;
            long sourceLength = _audioData.Length / _channels;
            
            // Calculate how many FRAMES we can actually read
            long availableInClip = Length - relativePosition;
            long availableInSource = sourceLength - sourcePosition;
            long available = Math.Min(availableInClip, availableInSource);
            int framesToRead = (int)Math.Min(frameCount, available);

            if (framesToRead <= 0)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }

            // Read and apply gain + fades
            float gain = Gain;
            int sourceIndex = (int)sourcePosition * _channels;
            
            for (int i = 0; i < framesToRead; i++)
            {
                float fadeEnvelope = CalculateFadeEnvelope(relativePosition + i);
                float combinedGain = gain * fadeEnvelope;

                for (int ch = 0; ch < _channels; ch++)
                {
                    int bufferIndex = offset + (i * _channels) + ch;
                    int audioIndex = sourceIndex + (i * _channels) + ch;
                    
                    buffer[bufferIndex] = _audioData[audioIndex] * combinedGain;
                }
            }

            // Clear remaining buffer if we read less than requested
            int samplesRead = framesToRead * _channels;
            if (samplesRead < count)
            {
                int remainingStart = offset + samplesRead;
                int remainingCount = count - samplesRead;
                Array.Clear(buffer, remainingStart, remainingCount);
            }

            return samplesRead;
        }
    }

    public float GetPeakAmplitude(long position, int windowSize = 1024)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative");
        if (windowSize < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1");

        if (IsMuted)
            return 0.0f;

        lock (_audioLock)
        {
            // Calculate relative position within clip
            long relativePosition = position - StartPosition;
            
            // Check if position is outside clip bounds
            if (relativePosition < 0 || relativePosition >= Length)
                return 0.0f;

            // Calculate source position
            long sourcePosition = relativePosition + SourceOffset;
            long sourceLength = _audioData.Length / _channels;
            
            if (sourcePosition >= sourceLength)
                return 0.0f;

            // Calculate window bounds
            long windowEnd = Math.Min(sourcePosition + windowSize, sourceLength);
            long actualWindow = windowEnd - sourcePosition;
            
            if (actualWindow <= 0)
                return 0.0f;

            // Find peak amplitude in window
            float peak = 0.0f;
            int startIndex = (int)sourcePosition * _channels;
            int endIndex = (int)windowEnd * _channels;

            for (int i = startIndex; i < endIndex; i++)
            {
                float abs = Math.Abs(_audioData[i]);
                if (abs > peak)
                    peak = abs;
            }

            // Apply gain and fade envelope at the start of the window
            float fadeEnvelope = CalculateFadeEnvelope(relativePosition);
            return peak * Gain * fadeEnvelope;
        }
    }

    /// <summary>
    /// Writes samples to the audio buffer (useful for recording)
    /// </summary>
    /// <param name="source">Source buffer with interleaved samples</param>
    /// <param name="offset">Offset in source buffer</param>
    /// <param name="count">Number of sample frames to write</param>
    /// <param name="position">Position in timeline to write at</param>
    /// <returns>Number of sample frames actually written</returns>
    public int WriteSamples(ReadOnlySpan<float> source, int offset, int count, long position)
    {
        if (offset < 0 || offset >= source.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of source range");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
        if (offset + (count * _channels) > source.Length)
            throw new ArgumentException("Source buffer is too small for the requested offset and count");
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative");

        lock (_audioLock)
        {
            long relativePosition = position - StartPosition;
            
            if (relativePosition < 0 || relativePosition >= Length)
                return 0;

            long sourcePosition = relativePosition + SourceOffset;
            long sourceLength = _audioData.Length / _channels;
            long available = sourceLength - sourcePosition;
            int samplesToWrite = (int)Math.Min(count, available);

            if (samplesToWrite <= 0)
                return 0;

            int sourceIndex = offset;
            int destIndex = (int)sourcePosition * _channels;
            int totalSamples = samplesToWrite * _channels;

            source.Slice(sourceIndex, totalSamples).CopyTo(_audioData.AsSpan(destIndex, totalSamples));

            return samplesToWrite;
        }
    }

    /// <summary>
    /// Gets the RMS (Root Mean Square) amplitude for a window of samples
    /// </summary>
    public float GetRmsAmplitude(long position, int windowSize = 1024)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative");
        if (windowSize < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1");

        if (IsMuted)
            return 0.0f;

        lock (_audioLock)
        {
            long relativePosition = position - StartPosition;
            
            if (relativePosition < 0 || relativePosition >= Length)
                return 0.0f;

            long sourcePosition = relativePosition + SourceOffset;
            long sourceLength = _audioData.Length / _channels;
            
            if (sourcePosition >= sourceLength)
                return 0.0f;

            long windowEnd = Math.Min(sourcePosition + windowSize, sourceLength);
            long actualWindow = windowEnd - sourcePosition;
            
            if (actualWindow <= 0)
                return 0.0f;

            double sumSquares = 0.0;
            int startIndex = (int)sourcePosition * _channels;
            int endIndex = (int)windowEnd * _channels;
            int sampleCount = endIndex - startIndex;

            for (int i = startIndex; i < endIndex; i++)
            {
                float sample = _audioData[i];
                sumSquares += sample * sample;
            }

            float rms = (float)Math.Sqrt(sumSquares / sampleCount);
            float fadeEnvelope = CalculateFadeEnvelope(relativePosition);
            
            return rms * Gain * fadeEnvelope;
        }
    }
}
