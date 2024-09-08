using System.Threading.Tasks;
using static UnityEngine.Mathf;

public static class SignalGenerator
{
    const float TAU = PI * 2;

    public static Signal GenerateSignal(FrequencyData[] waves, int sampleRate, float duration)
    {
        int numPoints = CeilToInt(sampleRate * duration);
        float[] samples = new float[numPoints];

        Parallel.For(0, numPoints, i =>
        {
            float ime = i / (float)(numPoints) * duration;
            float sum = 0;

            foreach (FrequencyData w in waves)
            {
                float angle = ime * TAU * w.Frequency + w.Phase;
                sum += Cos(angle) * w.Amplitude;
            }

            samples[i] = sum;
        });

        return new Signal(samples, sampleRate);
    }
}
