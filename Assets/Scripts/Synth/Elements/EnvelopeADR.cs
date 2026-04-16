using System;

namespace Audio.Synth
{
    public class EnvelopeADR
    {
        // Times are in seconds
        public float attack; // time to reach max volume after key press
        public float decay; // time to reach sustain volume after attack
        public float release; // time for volume to go to zero after key release
        //public float sustain = 0.8f; // fraction of full volume that note has when held down

        public readonly DiscreteCurve attackCurve;
        public readonly DiscreteCurve decayCurve;
        public readonly DiscreteCurve releaseCurve;

        public EnvelopeADR(DiscreteCurve attackCurve, DiscreteCurve decayCurve, DiscreteCurve releaseCurve)
        {
            this.attackCurve = attackCurve;
            this.decayCurve = decayCurve;
            this.releaseCurve = releaseCurve;
        }
        
        public float Evaluate(Note note)
        {
            float amplitude = 0;

            // Attack
            if (note.PressedTimer <= attack)
            {
                float attackT = note.PressedTimer / attack;
                amplitude = attackCurve.Evaluate(attackT);
            }
            // Decay
            else
            {
                float timeSinceDecayStart = note.PressedTimer - attack + note.ReleasedTimer;
                float decayT = timeSinceDecayStart / decay;
                amplitude = decayCurve.Evaluate(decayT);
            }

            // Release
            if (!note.IsPressed)
            {
                float releaseT = note.ReleasedTimer / release;
                amplitude *= releaseCurve.Evaluate(releaseT);
            }

            return amplitude;
        }

        public float Evaluate(float timeSincePress, float timeSinceRelease)
        {
            float gain = 0;

            // Attack
            if (timeSincePress <= attack)
            {
                float attackT = timeSincePress / attack;
                gain = attackCurve.Evaluate(attackT);
            }
            // Decay
            else
            {
                float msSinceDecayStart = timeSincePress - attack;
                float decayT = msSinceDecayStart / decay;
                gain = decayCurve.Evaluate(decayT);
            }

            // Release
            if (timeSinceRelease > 0)
            {
                float releaseT = timeSinceRelease / release;
                gain *= releaseCurve.Evaluate(releaseT);
            }

            return gain;
        }
    }
}