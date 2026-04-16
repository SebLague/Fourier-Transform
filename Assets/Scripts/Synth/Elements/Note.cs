using UnityEngine;

namespace Audio.Synth
{
    public class Note
    {
        public int Frequency;
        public bool IsPressed;
        public float[] Phases;
        public float[] DecayDurationFactors;
        public float[] HarmonicAmplitudes;
        public bool IsActive;

        public float PressedTimer;
        public float ReleasedTimer = float.MaxValue;
        
        public float TimeSincePressed_MainThread;
        public float TimeSinceReleased_MainThread;
        public float Velocity;
        
        public int MidiIndex;

        public void Press(float velocity = 1)
        {
            IsPressed = true;
            IsActive = true;
            PressedTimer = 0;
            ReleasedTimer = 0;

            TimeSincePressed_MainThread = 0;
            TimeSinceReleased_MainThread = 0;
            Velocity = velocity;
        }

        public void Release() => IsPressed = false;
    }
}