using UnityEngine;
using static UnityEngine.Mathf;
using Seb.Vis;

[ExecuteAlways]
public class FourierVis : MonoBehaviour
{
    [Header("FourierVis")]
    public Vector2 visOffset;
    public float pointSize;
    public float centreSize;
    public float circleThick;
    public Color circleCol;
    public Color circleLineCol;
    public Color colMain;
    public Color colFade;
    public float fadeDst = 0.4f;

    [Header("FlatVis")]
    public float flatRadMul = 0.9f;
    public float flatWidth = 10;
    public bool showFlatTime;
    public float flatScaleY;
    public float flatPosY;
    public Vector2 flatPad;
    public Color flatBGCol;
    public Color flatBGColOut;

    [Header("Inputs")]
    public WaveInputs[] waveInputs;
    public UIField testFreqInput;
    public UIField sampleRateUI;
    public UIField durationUI;

    float[] samples;
    const float TAU = PI * 2;

    int SampleRate => CeilToInt(sampleRateUI.value);
    int NumSamples => CeilToInt(SampleRate * Duration);
    float Duration => durationUI.value;

    private void Update()
    {
        float[] samples = GetSamples();

        Draw.StartLayerIfNotInMatching(transform.position, 1, false);
        VisFlat(samples);

        Draw.StartLayerIfNotInMatching(transform.position + (Vector3)visOffset, 1, false);
        DrawFourierVis(samples);
    }

    void DrawFourierVis(float[] samples)
    {
        Draw.Point(Vector2.zero, 1 + circleThick, circleCol);
        Draw.Point(Vector2.zero, 1, flatBGCol);
        Draw.Line(Vector2.left, Vector2.right, circleThick / 4f, circleLineCol);
        Draw.Line(Vector2.up, Vector2.down, circleThick / 4f, circleLineCol);

        float testFreq = testFreqInput.GetValue();

        Vector2 sampleSum = Vector2.zero;
        Color colMain = this.colMain;
        Color colFade = this.colFade;

        for (int i = 0; i < samples.Length; i++)
        {
            float angle = i / (float)(samples.Length) * TAU * testFreq;
            Vector2 testPoint = new Vector2(Cos(angle), Sin(angle));
            Vector2 samplePoint = testPoint * samples[i];
            sampleSum += samplePoint;

            float fade = FadeT(i);
            Color col = Color.Lerp(colMain, colFade, fade);
            Draw.Point(samplePoint, pointSize, col);
        }

        Vector2 sampleCentre = sampleSum / samples.Length;
        Draw.Point(sampleCentre, centreSize * 1.35f, Color.white);
        Draw.Point(sampleCentre, centreSize, Color.black);
    }



    void VisFlat(float[] samples)
    {
        Vector2 centre = new Vector2(0, flatPosY);
        Vector2 size = new Vector2(flatWidth, flatScaleY * 2) + flatPad;
        Draw.Quad(centre, size + Vector2.one * 0.02f, flatBGColOut);
        Draw.Quad(centre, size, flatBGCol);

        using (Draw.CreateMaskScope(centre - size / 2, centre + size / 2))
        {
            for (int i = 0; i < samples.Length; i++)
            {
                Color col = Color.Lerp(colMain, colFade, FadeT(i));
                Draw.Point(GetPos(i), pointSize * flatRadMul, col);
            }
        }

        Vector2 GetPos(int i)
        {
            float t;
            t = i / (float)(samples.Length - 1);

            float x = -flatWidth / 2f + flatWidth * t;
            float y = samples[i] * flatScaleY + flatPosY;
            return new Vector3(x, y);
        }
    }

    float[] GetSamples()
    {
        if (samples == null || samples.Length != NumSamples) samples = new float[NumSamples];

        for (int i = 0; i < samples.Length; i++)
        {
            float sum = 0;

            foreach (var w in waveInputs)
            {
                float t = i / (float)(samples.Length) * Duration;
                sum += Cos(t * TAU * w.Freq + w.Phase) * w.Amplitude;
            }
            samples[i] = sum;
        }
        return samples;
    }

    float FadeT(float i)
    {
        float dst = Abs(NumSamples - i);
        return Clamp01(dst / (NumSamples * fadeDst));
    }

    [System.Serializable]
    public class WaveInputs
    {
        public UIField freqInput;
        public UIField phaseInput;
        public UIField ampInput;

        public float Freq => freqInput.GetValue();
        public float Phase => phaseInput.GetValue();
        public float Amplitude => ampInput.GetValue();
    }
}
