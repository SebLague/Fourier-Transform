using UnityEngine;
using static UnityEngine.Mathf;
using Seb.Vis;

[ExecuteAlways]
public class FourierTest : MonoBehaviour
{
    public int reconstructCount;
    public Vector2 size;
    public float radius;
    public float thickness;
    public Color colBG;
    public Color colReconstruct;

    [Header("Inputs")]
    public UIField[] freqs;
    public UIField[] phas;
    public UIField[] amps;
    public UIField sampleInp;

    FrequencyData[] inputWaves;
    FrequencyData[] outputWaves;

    void Init()
    {
        if (inputWaves == null || inputWaves.Length != 3)
        {
            inputWaves = new FrequencyData[3];
        }

        for (int i = 0; i < inputWaves.Length; i++)
        {
            inputWaves[i].Frequency = freqs[i].GetValue();
            inputWaves[i].Amplitude = amps[i].GetValue();
            inputWaves[i].Phase = phas[i].GetValue();
        }
    }

    public void Update()
    {
        Init();

        int sampleRate = CeilToInt(sampleInp.value);
        float[] samples = SignalGenerator.GenerateSignal(inputWaves, sampleRate, 1).Samples;

        outputWaves = Fourier.DFT(samples, sampleRate);
        float[] reconstructedSamples = SignalGenerator.GenerateSignal(outputWaves, reconstructCount, 1).Samples;

        Draw.StartLayerIfNotInMatching(transform.position, 1, false);
        Draw.Quad(Vector2.zero, size, colBG);

        using (Draw.CreateMaskScope(-size / 2, size / 2))
        {
            // Draw reconstruction
            Vector2[] reconstructedPoints = new Vector2[reconstructedSamples.Length];
            for (int i = 0; i < reconstructedPoints.Length; i++)
            {
                reconstructedPoints[i] = GetPos(i, reconstructedSamples);
            }
            Draw.LinePath(reconstructedPoints, thickness, colReconstruct);

            // Draw samples
            for (int i = 0; i < samples.Length; i++)
            {
                Draw.Point(GetPos(i, samples), radius, Color.white);
            }
        }

      

        Vector2 GetPos(int i, float[] vals)
        {
            float timeBetweenSamples = 1f / vals.Length;

            float x = -size.x / 2f + size.x * i * timeBetweenSamples;
            float y = vals[i];
            return new Vector2(x, y);
        }
    }

}
