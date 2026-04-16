using System;
using Seb.Helpers;
using UnityEngine;
using static UnityEngine.Mathf;

namespace Audio.Synth
{
    public class DiscreteCurve
    {
        public readonly float[] Values;
        public readonly float[] ValuesDirect;
        readonly int endIndexDirect;

        public DiscreteCurve(float[] values)
        {
            this.Values = ArrayHelper.CreateCopy(values);

            // --- Init direct lookup values ---
            ValuesDirect = new float[Values.Length * 64];
            //Debug.Log(Values.Length + " -> " + ValuesDirect.Length);
            for (int i = 0; i < ValuesDirect.Length; i++)
            {
                float t = i / (ValuesDirect.Length - 1f);
                ValuesDirect[i] = Evaluate(t);
            }

            endIndexDirect = ValuesDirect.Length - 1;
        }


        // Get value along curve (0 = start, 1 = end)
        public float Evaluate(float t)
        {
            // Look up values to left and right of t
            float fractionalIndex = Clamp(t * (Values.Length - 1), 0, Values.Length - 1);
            int indexA = (int)fractionalIndex;
            int indexB = Min(indexA + 1, Values.Length - 1);

            if (indexA < 0 || indexA >= Values.Length) throw new IndexOutOfRangeException($"Index {indexA} is out of range ({Values.Length}) t = {t} t01 = {Mathf.Clamp01(t)}");
            float valA = Values[indexA];
            float valB = Values[indexB];

            // Interpolate based on where t lies between A and B
            float frac = fractionalIndex - indexA;
            return Lerp(valA, valB, frac);
        }

        public float EvaluateDirect(float t)
        {
            int i = (int)(t * endIndexDirect);
            i = Clamp(i, 0, endIndexDirect);

            return ValuesDirect[i];
        }
    }
}