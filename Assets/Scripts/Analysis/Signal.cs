using System;

namespace Audio.Core
{
    public class Signal
    {
        public float[] Samples;
        public int SampleRate;
        public string Name;

        public int NumSamples => Samples.Length;
        public float Duration => NumSamples / (float)SampleRate;

        public Signal(float[] samples, int sampleRate, string name = "Untitled")
        {
            Samples = samples;
            SampleRate = sampleRate;
            Name = name;
        }

        public Signal Clone()
        {
            float[] copy = new float[Samples.Length];
            Array.Copy(Samples, copy, copy.Length);
            return new Signal(copy, SampleRate);
        }
    }
}