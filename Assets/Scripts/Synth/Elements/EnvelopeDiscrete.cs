using System.Collections;
using System.Collections.Generic;
using Seb.Helpers;
using UnityEngine;

namespace Audio.Synth
{
    [System.Serializable]
    public class EnvelopeDiscrete
    {
        public readonly float attackMs; // time in ms to reach max volume after key press
        public readonly float holdMs; // time in ms to maintain max volume once reached
        public readonly float decayMs; // time in ms to reach sustain volume after attack
        //public float releaseMs = 50; // time in ms for volume to go to zero after key release
        //public float sustain = 0.8f; // fraction of full volume that note has when held down

        public readonly float[] attackAmplitudes;
        public readonly float[] decayAmplitudes;

        public EnvelopeDiscrete(float attackMs, float holdMs, float decayMs, float[] attackAmplitudes, float[] decayAmplitudes)
        {
            this.attackMs = attackMs;
            this.holdMs = holdMs;
            this.decayMs = decayMs;
            this.attackAmplitudes = attackAmplitudes;
            this.decayAmplitudes = decayAmplitudes;
        }

        public float GetGain(float timeSincePress, float timeSinceRelease)
        {
            float msSincePress = timeSincePress * 1000;

            // Attack
            if (msSincePress <= attackMs)
            {
                float attackT = msSincePress / attackMs;
                return Maths.SampleData(attackAmplitudes, attackT);
            }

            // Hold
            if (msSincePress < attackMs + holdMs)
            {
                return 1;
            }

            if (msSincePress < attackMs + holdMs + decayMs)
            {
                float msSinceDecayStart = msSincePress - (attackMs + holdMs);
                float decayT = msSinceDecayStart / decayMs;
                return Maths.SampleData(decayAmplitudes, decayT);
            }

            return 0;
        }
    }
}