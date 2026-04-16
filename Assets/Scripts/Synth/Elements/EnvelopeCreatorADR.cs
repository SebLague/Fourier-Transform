using Audio.Synth;
using UnityEngine;

namespace Audio.Tools
{
    [ExecuteAlways]
    public class EnvelopeCreatorADR : MonoBehaviour
    {
        public float Sustain;
        public DiscreteCurveWidgetBase curveAttack;
        public DiscreteCurveWidgetBase curveDecay;
        public DiscreteCurveWidgetBase curveRelease;
        public bool useDecay;
        public float spacingX;
        public bool spaceVertical;

        // Cached values
        LinearEnvelopeADR envelope;

        readonly DiscreteCurve noDecayCurve = new(new float[] { 1, 1 });

        public LinearEnvelopeADR CreateEnvelope()
        {
            DiscreteCurve attackCurveDiscrete = curveAttack.GetDiscreteCurve();
            DiscreteCurve decayCurveDiscrete = curveDecay.GetDiscreteCurve();
            DiscreteCurve releaseCurveDiscrete = curveRelease.GetDiscreteCurve();

            bool needsNewEnvelope = envelope == null;
            if (envelope != null)
            {
                needsNewEnvelope |= attackCurveDiscrete != envelope.AttackCurve;
                if (useDecay) needsNewEnvelope |= decayCurveDiscrete != envelope.DecayCurve;
                else needsNewEnvelope |= noDecayCurve != envelope.DecayCurve;
                needsNewEnvelope |= releaseCurveDiscrete != envelope.ReleaseCurve;
            }

            if (needsNewEnvelope)
                envelope = new LinearEnvelopeADR(
                    attackCurveDiscrete,
                    useDecay ? decayCurveDiscrete : noDecayCurve,
                    releaseCurveDiscrete
                );

            envelope.Attack = curveAttack.GetXAxisMinMax().y / 1000;
            envelope.Decay = curveDecay.GetXAxisMinMax().y / 1000;
            envelope.Release = curveRelease.GetXAxisMinMax().y / 1000;
            envelope.Sustain = Sustain;

            return envelope;
        }

        void Update()
        {
            if (spaceVertical)
            {
                curveAttack.transform.localPosition = Vector3.up * spacingX;
                curveDecay.transform.localPosition = Vector3.zero;
                curveRelease.transform.localPosition = Vector3.down * spacingX;
            }
            else
            {
                curveAttack.transform.localPosition = Vector3.left * spacingX;
                curveDecay.transform.localPosition = Vector3.zero;
                curveRelease.transform.localPosition = Vector3.right * spacingX;
            }
        }
    }
}