using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Vis;

public class WaveformVis : MonoBehaviour
{
    public string fileName;
    public float lineThickness = 0.01f;
    public float heightMultiplier = 1;
    public Color col = Color.green;
    Signal signal;

    void Start()
    {
        signal = Wav.ReadWav(Wav.GetWavPath("Input", fileName));    
    }

  
    void Update()
    {
        Draw.StartLayer(Vector2.zero, 1, false);

        float x = -signal.NumSamples / 2f * lineThickness ;

        for (int i = 0; i < signal.NumSamples; i++)
        {
            float amplitude = Mathf.Abs(signal.Samples[i]);
            Vector2 posA = new Vector2(x, -amplitude * heightMultiplier);
            Vector2 posB = new Vector2(x, amplitude * heightMultiplier);
            Draw.Line(posA, posB, lineThickness, col);
            x += lineThickness;

        }
    }
}
