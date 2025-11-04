using StudioSoundPro.Core.Audio;

namespace StudioSoundPro.Core.Audio;

/// <summary>
/// Generates sine wave audio signals at specified frequency and amplitude.
/// </summary>
public class SineGenerator : ISineGenerator
{
    private double _frequency = 440.0;
    private double _amplitude = 0.25; // -12 dBFS approximately
    private bool _isEnabled = false;
    private double _sampleRate = 48000.0;
    private double _phase = 0.0;
    private double _phaseIncrement = 0.0;

    public double Frequency
    {
        get => _frequency;
        set
        {
            _frequency = Math.Max(0.0, value);
            UpdatePhaseIncrement();
        }
    }

    public double Amplitude
    {
        get => _amplitude;
        set => _amplitude = Math.Clamp(value, 0.0, 1.0);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Initialize(double sampleRate)
    {
        _sampleRate = Math.Max(1.0, sampleRate);
        UpdatePhaseIncrement();
        Reset();
    }

    public void GenerateAudio(float[] buffer, int frameCount, int channels)
    {
        if (!_isEnabled || frameCount <= 0 || channels <= 0)
        {
            // Fill with silence
            if (buffer != null)
            {
                Array.Fill(buffer, 0.0f);
            }
            return;
        }

        if (buffer == null)
        {
            return;
        }

        var bufferIndex = 0;
        for (int frame = 0; frame < frameCount; frame++)
        {
            // Generate sine wave sample
            var sample = (float)(_amplitude * Math.Sin(_phase));
            
            // Fill all channels with the same sample (mono to multi-channel)
            for (int channel = 0; channel < channels; channel++)
            {
                buffer[bufferIndex++] = sample;
            }

            // Advance phase
            _phase += _phaseIncrement;
            
            // Wrap phase to prevent accumulation of floating-point errors
            if (_phase >= 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }
        }
    }

    public void Reset()
    {
        _phase = 0.0;
    }

    private void UpdatePhaseIncrement()
    {
        _phaseIncrement = 2.0 * Math.PI * _frequency / _sampleRate;
    }

    /// <summary>
    /// Sets amplitude in decibels.
    /// </summary>
    /// <param name="dB">Amplitude in decibels (0 dB = full scale).</param>
    public void SetAmplitudeDb(double dB)
    {
        // Convert dB to linear amplitude
        Amplitude = Math.Pow(10.0, dB / 20.0);
    }

    /// <summary>
    /// Gets the current amplitude in decibels.
    /// </summary>
    /// <returns>Amplitude in decibels.</returns>
    public double GetAmplitudeDb()
    {
        return 20.0 * Math.Log10(Math.Max(_amplitude, 1e-10)); // Avoid log(0)
    }
}