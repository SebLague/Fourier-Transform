using System;
using System.Collections.Generic;
using Audio.Core;
using static Audio.Analysis.SignalGenerator;
using static UnityEngine.Mathf;

namespace Audio.Analysis
{
    public static class STFT
    {
        public static StftResult Compute(Signal signal, int samplesPerSegment, float hopT = 1, bool hannWindow = false, bool useFFT = false)
        {
            List<StftSegment> segments = new();
            int hopLength = (int)Math.Max(1, samplesPerSegment * hopT);

            float[] segmentData = new float[samplesPerSegment];

            for (int offset = 0; offset < signal.NumSamples; offset += hopLength)
            {
                int numSamplesRemaining = signal.NumSamples - offset;
                int segmentLength = Min(samplesPerSegment, numSamplesRemaining);
                Span<float> segmentSpan = signal.Samples.AsSpan(offset, segmentLength);

                for (int i = 0; i < segmentData.Length; i++)
                {
                    segmentData[i] = i < segmentSpan.Length ? segmentSpan[i] : 0;
                }

                if (hannWindow) ApplyHannWindow(segmentData);

                FrequencyData[] waves;
                if (useFFT) waves = FFT.Compute(segmentData, signal.SampleRate);
                else waves = DFT.Compute(segmentData, signal.SampleRate);
                
                StftSegment segment = new(waves, offset, segmentLength);
                segments.Add(segment);
            }

            return new StftResult(segments.ToArray(), signal, hannWindow);
        }


        public static Signal ReconstructSignal(StftResult stft, bool hannWindow = false)
        {
            float[] allSamples = new float[stft.SampleCount];

            foreach (StftSegment segment in stft.Segments)
            {
                float[] segmentSamples = Generate(segment.Spectrum, stft.SampleRate, segment.SampleCount);

                //
                if (hannWindow) ApplyHannWindow(segmentSamples);

                // Add reconstructed segment into full reconstruction
                for (int i = 0; i < segment.SampleCount; i++)
                {
                    allSamples[i + segment.SampleOffset] += segmentSamples[i];
                }
            }

            return new Signal(allSamples, stft.SampleRate);
        }


        public static void ApplyHannWindow(Span<float> samples)
        {
            const float TAU = 2 * PI;

            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (samples.Length - 1f); // [0, 1]
                float smoothWindow = 0.5f * (1 - Cos(t * TAU));
                samples[i] *= smoothWindow;
            }
        }

        public struct StftSegment
        {
            public FrequencyData[] Spectrum;
            public readonly int SampleOffset;
            public readonly int SampleCount;

            public StftSegment(FrequencyData[] waves, int segmentStartIndex, int segmentSampleCount)
            {
                this.SampleOffset = segmentStartIndex;
                this.SampleCount = segmentSampleCount;
                this.Spectrum = waves;
            }
        }

        public readonly struct StftResult
        {
            public readonly StftSegment[] Segments;
            public readonly int SampleRate;
            public readonly int SampleCount;
            public readonly bool useWindow;

            public int FrequencyCount => Segments[0].Spectrum.Length;
            public float FrequencySpacing => Segments[0].Spectrum[1].Frequency - Segments[0].Spectrum[0].Frequency;
            public float SegmentDuration => Segments[0].SampleCount / (float)SampleRate;

            public StftResult(StftSegment[] segments, Signal inputSignal, bool useWindow)
            {
                this.Segments = segments;
                this.SampleCount = inputSignal.NumSamples;
                this.SampleRate = inputSignal.SampleRate;
                this.useWindow = useWindow;
            }

            public StftResult(StftSegment[] segments, int rate, int count, bool useWindow)
            {
                this.Segments = segments;
                this.SampleCount = count;
                this.SampleRate = rate;
                this.useWindow = useWindow;
            }
        }
    }
}