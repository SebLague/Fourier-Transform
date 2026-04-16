using UnityEngine;
using Seb.Visualization;
using System;
using Audio.Core;
using Audio.Helpers;
using Seb.Types;

namespace Audio.Tools
{
    public class WaveformDisplay : MonoBehaviour
    {
        public AudioClip audioClip;
        [Header("Waveform Display")]
        public Color waveformCol;
        public bool abs;
        public float yMul = 1;
        public float thicknessMul = 1;
        public float thicknessMul2 = 1;
        public float width = 10;
        public bool normalize;

        [Header("Other Display")]
        public float fontSize;
        public FontType fontType;
        public Vector2 titleOffset;
        public float boundsThick;
        public Vector2 boundsPadding;
        public Color boundsCol;
        public Color boundsColBG;
        public Color axisLabelsCol;
        public float axisLabelTargetSpacingX;

        [Header("Animation")]
        public float playAnimT;
        public float animOffset;
        public float playAnimScale;
        public float playAnimFalloff;

        // State
        Vector2 boundsDisplayMin;
        Vector2 boundsDisplayMax;
        Signal signal;
        bool playing;

        void Start()
        {
            signal = AudioFileHelper.SignalFromAudioClip(audioClip);
        }


        void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                playing = true;
            }

            if (playing) playAnimT += Time.deltaTime / (float)signal.Duration;

            Vis.StartLayerIfNotInMatching(transform.position, 1, false);

            double x = -width / 2;
            double thickness = width / signal.NumSamples;

            Vector2 boundsMin = Vector2.one * float.MaxValue;
            Vector2 boundsMax = Vector2.one * float.MinValue;
            var bgId = Vis.ReserveQuad();

            double min = double.MaxValue;
            double max = double.MinValue;

            if (normalize)
            {
                for (int i = 0; i < signal.Samples.Length; i++)
                {
                    min = Math.Min(min, signal.Samples[i]);
                    max = Math.Max(max, signal.Samples[i]);
                }
            }

            for (int i = 0; i < signal.Samples.Length; i++)
            {
                float t = i / (signal.NumSamples - 1f);
                float dst = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(t - (playAnimT + animOffset))), playAnimFalloff);
                float playScale = 1 + dst * playAnimScale;

                float y = (float)(signal.Samples[i] * yMul);
                if (normalize)
                {
                    double norm = signal.Samples[i] / Math.Max(Math.Abs(min), Math.Abs(max));
                    y = (float)norm * yMul;
                }

                Vector2 a = new((float)x, 0);
                Vector2 b = new((float)x, y);
                float animT = 1;

                if (abs)
                {
                    Vector2 aAbs = new((float)x, -Mathf.Abs(y) * animT);
                    Vector2 bAbs = new((float)x, Mathf.Abs(y) * animT);
                    Vis.Line(aAbs, bAbs, (float)(thickness / 2 * playScale * thicknessMul * thicknessMul2), Color.Lerp(waveformCol, Color.white, dst), 1);
                    a = aAbs;
                    b = bAbs;
                }
                else
                {
                    Vis.Line(a, b, (float)(thickness / 2 * playScale * thicknessMul * thicknessMul2), Color.Lerp(waveformCol, Color.white, dst), animT);
                }

                x += thickness * thicknessMul;

                boundsMax = Vector2.Max(boundsMax, Vector2.Max(a, b));
                boundsMin = Vector2.Min(boundsMin, Vector2.Min(a, b));
            }

            boundsDisplayMin = boundsMin - boundsPadding / 2;
            boundsDisplayMax = boundsMax + boundsPadding / 2;
            Bounds2D bounds = new(boundsDisplayMin, boundsDisplayMax);

            Vis.QuadOutlineMinMax(boundsDisplayMin, boundsDisplayMax, boundsThick, boundsCol);
            Vis.ModifyQuad(bgId, (boundsDisplayMin + boundsDisplayMax), boundsDisplayMax - boundsDisplayMin, boundsColBG);

            Vector2 titlePos = new Vector2(boundsMin.x, boundsMax.y) + titleOffset;
            Vis.Text(fontType, audioClip.name + ".wav", fontSize, titlePos, Anchor.BottomLeft, Color.white);

            DrawLabels(bounds);
        }

        void DrawLabels(Bounds2D bounds)
        {
            int count = Mathf.CeilToInt(width / Mathf.Max(0.1f, axisLabelTargetSpacingX));

            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                float x = -width / 2 + width * t;
                float y = bounds.Bottom - fontSize / 2;
                Vector2 pos = new Vector2(x, y);

                float sec = signal.Duration * t;
                string text = $"{sec:0.0}";
                Vis.Text(fontType, text, fontSize, pos, Anchor.CentreTop, axisLabelsCol);
            }
        }
    }
}