using UnityEngine;

namespace Audio.Helpers
{
    public static class AudioHelper
    {
        public static float AmplitudeToDecibels(float amplitude)
        {
            return 20 * Mathf.Log10(amplitude);
        }

        public static float SignedAmplitudeToDecibels(float amplitude)
        {
            return 10 * Mathf.Log10(amplitude * amplitude);
        }

        public static float DecibelsToAmplitude(float db)
        {
            return Mathf.Pow(10, db / 20);
        }
    }
}