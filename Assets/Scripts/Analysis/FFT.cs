using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Audio.Analysis
{
    public class FFT : MonoBehaviour
    {
        public static FrequencyData[] Compute(ReadOnlySpan<float> signal, int sampleRate)
        {
            int lenPot = Mathf.NextPowerOfTwo(signal.Length);
            if (lenPot != signal.Length)
            {
                float[] potSig = new float[lenPot];
                for (int i = 0; i < signal.Length; i++)
                {
                    potSig[i] = signal[i];
                }

                signal = potSig;
            }

            double nyquistFreq = sampleRate / 2.0;
            Debug.Assert(signal.Length == Mathf.NextPowerOfTwo(signal.Length), "Length must be a power of 2");

            Complex[] signalC = new Complex[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                signalC[i] = new Complex(signal[i], 0);
            }

            Complex[] result = FFTInternal(signalC);

            int numFrequencies = signal.Length / 2 + 1;
            FrequencyData[] waves = new FrequencyData[numFrequencies];
            for (int i = 0; i < waves.Length; i++)
            {
                bool is0Hz = i == 0;
                // The last frequency is equal to samplerate/2 only if sample count is even
                bool isNyquistFreq = i == waves.Length - 1 && signal.Length % 2 == 0;
                float amplitudeScale = is0Hz || isNyquistFreq ? 1 : 2;

                double f = i / (waves.Length - 1.0) * nyquistFreq;
                waves[i] = new FrequencyData((float)f, (float)(result[i].Magnitude / signal.Length * amplitudeScale), (float)(-result[i].Angle));
                //  waves[i] = new Wave(i / (waves.Length-1.0) * nyquistFreq, com.magnitude * 2, offset);
            }

            return waves;
        }

        static Complex[] FFTInternal(Complex[] values)
        {
            if (values.Length == 1) return values;

            Complex[] evenSteps = FFTInternal(TakeEverySecond(values, true));
            Complex[] oddSteps = FFTInternal(TakeEverySecond(values, false));
            Complex[] results = new Complex[values.Length];

            double angleIncrement = System.Math.PI * 2 / values.Length;


            for (int index = 0; index < values.Length / 2; index++)
            {
                Complex c = Complex.CreateFromPolar(angleIncrement * index, 1);
                results[index] = Complex.Add(evenSteps[index], Complex.Mul(oddSteps[index], c)); // curr += new
                results[index + values.Length / 2] = Complex.Subtract(evenSteps[index], Complex.Mul(oddSteps[index], c)); // curr -= new
            }

            return results;
        }

        public struct Complex
        {
            public double X;
            public double Y;

            public double Magnitude => Math.Sqrt(X * X + Y * Y);
            public double Angle => Math.Atan2(Y, X);

            public Complex(double x, double y)
            {
                X = x;
                Y = y;
            }

            public static Complex CreateFromPolar(double angle, double magnitude)
            {
                return new Complex(Math.Cos(angle) * magnitude, Math.Sin(angle) * magnitude);
            }

            public static Complex Add(Complex a, Complex b)
            {
                return new Complex(a.X + b.X, a.Y + b.Y);
            }

            public static Complex Subtract(Complex a, Complex b)
            {
                return new Complex(a.X - b.X, a.Y - b.Y);
            }

            public static Complex Mul(Complex a, Complex b)
            {
                return new Complex(a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);
            }
        }

        static Complex[] TakeEverySecond(Complex[] values, bool startOnFirst)
        {
            Complex[] result = new Complex[values.Length / 2];
            int offset = startOnFirst ? 0 : 1;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i * 2 + offset];
            }

            return result;
        }
    }
}