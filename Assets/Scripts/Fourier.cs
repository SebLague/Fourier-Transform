using UnityEngine;
using static UnityEngine.Mathf;
using System.Threading.Tasks;

public static class Fourier
{
    const float TAU = 2 * PI;

    // Discrete Fourier Transform (extremely slow implementation for learning purposes)
    public static FrequencyData[] DFT(float[] samples, int sampleRate)
    {
        // If a signal is sampled for 1 second, only integer frequencies appear periodic.
        // With a duration of 2 seconds, every increment of 0.5 Hz appears periodic, and so on.
        // Additionally, from Nyquist we know we can detect a maximum frequency of sampleRate / 2.
        // So, the number of frequencies is sampleRate / 2 * duration. (Equivalently: numSamples / 2)
        int numFrequencies = samples.Length / 2 + 1; // Add one since we want to start at 0 Hz
        FrequencyData[] spectrum = new FrequencyData[numFrequencies];
        // Calculate the size of the frequency steps such that the last value in the spectrum will be the
        // max frequency (Note: max frequency only exactly represented when sample count is even) 
        float frequencyStep = sampleRate / (float)samples.Length; // Equivalent to 1 / duration

        Parallel.For(0, spectrum.Length, freqIndex =>
        {
            Vector2 sampleSum = Vector2.zero;
            for (int i = 0; i < samples.Length; i++)
            {
                float angle = i / (float)(samples.Length) * TAU * freqIndex;
                Vector2 testPoint = new(Cos(angle), Sin(angle));
                sampleSum += testPoint * samples[i];
            }

            Vector2 sampleCentre = sampleSum / samples.Length;

            bool is0Hz = freqIndex == 0;
            // The last frequency is equal to samplerate/2 only if sample count is even
            bool isNyquistFreq = freqIndex == spectrum.Length - 1 && samples.Length % 2 == 0;
            float amplitudeScale = is0Hz || isNyquistFreq ? 1 : 2;
            float amplitude = sampleCentre.magnitude * amplitudeScale;

            float frequency = freqIndex * frequencyStep;
            float phase = -Atan2(sampleCentre.y, sampleCentre.x);
            spectrum[freqIndex] = new FrequencyData(frequency, amplitude, phase);
        });

        return spectrum;
    }

}
