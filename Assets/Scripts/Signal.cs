using System;

public class Signal
{
    public float[] Samples;
    public int SampleRate;

    public int NumSamples => Samples.Length;
    public float Duration => NumSamples / (float)SampleRate;

    public Signal(float[] samples, int sampleRate)
    {
        Samples = samples;
        SampleRate = sampleRate;
    }

    public Signal Clone()
    {
        float[] copy = new float[Samples.Length];
        Array.Copy(Samples, copy, copy.Length);
        return new Signal(copy, SampleRate);
    }
}
