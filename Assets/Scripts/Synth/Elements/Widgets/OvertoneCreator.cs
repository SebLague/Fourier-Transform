using Seb.Helpers;
using Seb.Visualization;
using UnityEngine;

namespace Audio.Display
{
    [ExecuteAlways]
    public class OvertoneCreator : Widget
    {
        [Header("Settings")]
        public int overtoneCount = 16;
        public bool linearViewMode = true;
        public float multiplier = 1;

        [Header("Display Settings")]
        public float spacing = 1.0f;
        public float overtoneTextOffset;
        public Vector2 labelBgSize;
        public WaveDisplay waveDisplay;

        [Header("State")]
        [SerializeField] public float[] values;
        public float[] valuesScaled;

        // Private state
        bool panelHasMouseFocus;
        float[] waveValues;
        string[] labelStrings;
        [HideInInspector] public bool modifiedThisFrame;

        void Update()
        {
            Init();
            DrawWidget();
            modifiedThisFrame = false;
        }


        void UpdateScaledAmplitudes()
        {
            ArrayHelper.Resize(ref valuesScaled, values.Length);

            float scaledSum = 0;

            for (int i = 0; i < valuesScaled.Length; i++)
            {
                float t = values[i];
                valuesScaled[i] = t;
                scaledSum += valuesScaled[i];
            }

            // Normalize
            if (scaledSum > 0)
            {
                for (int i = 0; i < valuesScaled.Length; i++)
                {
                    valuesScaled[i] /= scaledSum;
                    valuesScaled[i] *= multiplier;
                }
            }
        }

        void Init()
        {
            if (values == null)
            {
                values = new float[overtoneCount];
                values[0] = 1;
            }
            else ArrayHelper.Resize(ref values, overtoneCount);

            if (ArrayHelper.Resize(ref labelStrings, overtoneCount))
            {
                for (int i = 0; i < labelStrings.Length; i++)
                {
                    labelStrings[i] = $"{i + 1}x";
                }
            }
        }

        void DrawWidget()
        {
            StartLayer();
            DrawPanel(false);
            DrawOvertonesBars();
            DrawPanelOutline();

            if (modifiedThisFrame) UpdateScaledAmplitudes();

            DrawWave();
        }

        void DrawWave()
        {
            if (modifiedThisFrame) UpdateWaveValues();


            Vector2 waveCentre = Vector2.up * (Size.y / 2 + waveDisplay.height / 2 + waveDisplay.offsetY);
            Vis.Quad(waveCentre, new Vector2(Size.x, waveDisplay.height), widgetTheme.panelCol);
            float yMul = waveDisplay.height * 0.5f * 0.95f;

            Vis.DataLine(waveValues, -Size.x / 2, Size.x / 2, yMul, waveCentre.y, waveDisplay.thickness, widgetTheme.colMain);

            Vis.QuadOutline(waveCentre, new Vector2(Size.x, waveDisplay.height), uiTheme.panelOutlineThickness, widgetTheme.panelBorderCol);

            Vis.Point(new Vector2(-Size.x / 2, waveValues[0] * yMul + waveCentre.y), 0.075f, widgetTheme.colMain);
            Vis.Point(new Vector2(Size.x / 2, waveValues[^1] * yMul + waveCentre.y), 0.075f, widgetTheme.colMain);

            return;

            void UpdateWaveValues()
            {
                ArrayHelper.Resize(ref waveValues, waveDisplay.resolution);
                for (int i = 0; i < waveValues.Length; i++)
                {
                    float t = i / (waveValues.Length - 1f);
                    float sum = 0;

                    for (int overtone = 0; overtone < overtoneCount; overtone++)
                    {
                        float f = overtone + 1;
                        sum += Mathf.Sin(t * Mathf.PI * 2 * f) * (valuesScaled[overtone] / multiplier);
                    }

                    waveValues[i] = sum;
                }
            }
        }
        

        void DrawOvertonesBars()
        {
            // Panel
            Vis.Quad(Vector2.zero, Size, widgetTheme.panelBorderCol);

            // ---- Overtone bars ----
            Vector2 bottomLeft = -Size / 2f;
            float barWidth = Maths.GetElementSizeForSpacedLayout(overtoneCount, Size.x, spacing);
            float barX = bottomLeft.x + barWidth / 2;
            Vector2 mousePos = MousePos;

            if (InputHelper.IsMouseHeld(MouseButton.Left) && Maths.QuadContainsPoint(mousePos, Vector2.zero, Size)) panelHasMouseFocus = true;
            if (InputHelper.IsMouseUpThisFrame(MouseButton.Left)) panelHasMouseFocus = false;

            for (int i = 0; i < overtoneCount; i++)
            {
                float amplitudeT = values[i];
                if (!linearViewMode) amplitudeT = Mathf.Sqrt(amplitudeT);
                float barHeight = Size.y * amplitudeT;

                Vector2 regionPos = new(barX, 0);
                Vector2 regionSize = new(barWidth, Size.y);
                Vector2 barPos = new(barX, -regionSize.y / 2 + barHeight / 2);
                Vector2 barSize = new(barWidth, barHeight);

                bool barHasMouseFocus;
                if (panelHasMouseFocus)
                {
                    barHasMouseFocus = Maths.QuadContainsPoint(mousePos, regionPos, regionSize + Vector2.up * 1000);
                }
                else barHasMouseFocus = Maths.QuadContainsPoint(mousePos, regionPos, regionSize);


                Color barCol = barHasMouseFocus ? widgetTheme.colMainHighlight : widgetTheme.colMain;
                Color bgCol = barHasMouseFocus ? ColHelper.Brighten(widgetTheme.panelCol, 0.1f) : widgetTheme.panelCol;

                Vis.Quad(regionPos, regionSize,bgCol);
                Vis.Quad(barPos, barSize, barCol);


                Vector2 textPos = new Vector2(barX, bottomLeft.y + overtoneTextOffset);
                Vis.Quad(textPos, labelBgSize, widgetTheme.panelCol);
                Vis.QuadOutline(textPos, labelBgSize, uiTheme.panelOutlineThickness, widgetTheme.panelBorderCol);
                Vis.Text(uiTheme.font, labelStrings[i], uiTheme.textSize, textPos, Anchor.Centre, widgetTheme.textSecondaryCol);

                barX += barWidth + spacing;

                if (InputHelper.IsMouseHeld(MouseButton.Left) && barHasMouseFocus)
                {
                    values[i] = Mathf.Clamp01(Mathf.InverseLerp(-regionSize.y / 2, regionSize.y / 2, mousePos.y));
                    if (!linearViewMode) values[i] *= values[i];
                    modifiedThisFrame = true;
                }
                else if (InputHelper.IsMouseHeld(MouseButton.Right) && barHasMouseFocus)
                {
                    values[i] = 0;
                    modifiedThisFrame = true;
                }
            }
        }

        void OnValidate()
        {
            modifiedThisFrame = true;
        }

        [System.Serializable]
        public struct WaveDisplay
        {
            public float offsetY;
            public float height;
            public float thickness;
            public float thickness_shapes;
            public int resolution;
        }
    }
}