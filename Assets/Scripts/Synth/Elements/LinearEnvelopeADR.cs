using Audio.Synth;
using static UnityEngine.Mathf;


public class LinearEnvelopeADR
{
    // Durations in seconds
    public float Attack;
    public float Decay;
    public float Release;
    public readonly DiscreteCurve AttackCurve;
    public readonly DiscreteCurve DecayCurve;
    public readonly DiscreteCurve ReleaseCurve;

    public float Sustain = 0;

    public LinearEnvelopeADR(DiscreteCurve attackCurve, DiscreteCurve decayCurve, DiscreteCurve releaseCurve)
    {
        AttackCurve = attackCurve;
        DecayCurve = decayCurve;
        ReleaseCurve = releaseCurve;
    }

    public float Evaluate(Note note, float decayRate = 1)
    {
        float value = 1;

        // Attack!
        if (note.PressedTimer < Attack)
        {
            value = AttackCurve.EvaluateDirect(note.PressedTimer / Attack);
        }
        else
        {
            // Decay!
            float decayTimer = note.PressedTimer - Attack + note.ReleasedTimer;
            float decayValue = DecayCurve.EvaluateDirect((decayTimer * decayRate) / Decay);
            value = Sustain + (1 - Sustain) * decayValue;
        }


        // Release!
        if (!note.IsPressed) value *= ReleaseCurve.EvaluateDirect((note.ReleasedTimer * decayRate) / Release);
        return value;
    }
}