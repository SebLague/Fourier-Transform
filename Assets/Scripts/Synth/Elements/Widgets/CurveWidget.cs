using Audio.Synth;
using Seb.Helpers;
using Seb.Stuff;
using Seb.Visualization;
using UnityEngine;

namespace Audio.Tools
{
    [ExecuteAlways]
    public class CurveWidget : DiscreteCurveWidgetBase
    {
        [Space] public bool showAmplitudeCurveInDecibelMode;
        public CurveDisplaySettings displaySettings;

        [Header("Behaviour")]
        public bool useRemapCurve;
        public CurveWidget mappingCurve;
        public float valueDragSensitivity = 50;


        [Header("State")]
        public BezierCurve curve;
        public float sustain;

        [Header("References")]
        public AxesWidgetAddon axisInfo;

        // Private state
        float dragPrevX;
        bool isDraggingValue;
        DiscreteCurve mappingCurveDiscrete;
        int selectedControlIndex = -1;
        int mouseOverID = -1;
        Vector2 handleSelectionOffset;
        bool discretizedValuesUpToDate;
        DiscreteCurve discretizedDisplayCurve;


        void Update()
        {
            UpdateDiscreteCurve();
            DrawWidget();
        }


        void DrawWidget()
        {
            StartLayer();
            DrawPanel(true);
            DrawPanelOutline();

            if (useRemapCurve && showAmplitudeCurveInDecibelMode)
            {
                Vis.DataLine(discreteCurve.Values, Left, Left + Size.x, Size.y, -Size.y / 2, thickness, widgetTheme.amplitudeCurveCol);
            }


            // Anchor -> Control lines
            DrawControlLine(curve.anchorA, curve.controlA);
            DrawControlLine(curve.anchorB, curve.controlB);
            // Curve
            DrawCurve(curve);

            // Anchor and control points

           // DrawHandle(ref curve.anchorA, 2, true);
            //Vector2 susTest = curve.anchorB;
           // DrawHandle(ref curve.anchorB, 3, true);
            DrawAnchor(curve.anchorA);
            DrawAnchor(curve.anchorB);

            //Vector2 susPos = UVToWorld(curve.anchorB) + Vector2.right * 0.25f;
            //if (selectedControlIndex == 3) Vis.Text(uiTheme.font, $"SUSTAIN = {curve.anchorB.y:0.00}", uiTheme.textSize, susPos, Anchor.TextCentreLeft, Color.white);
            // sustain = susTest.y;
            //curve.anchorB = susTest;

            DrawHandle(ref curve.controlA, 0);
            DrawHandle(ref curve.controlB, 1);


            return;

            void DrawAnchor(Vector2 uv)
            {
                Vis.Point(UVToWorld(uv), displaySettings.anchorSize, widgetTheme.colMain);
            }

            void DrawControlLine(Vector2 anchorUV, Vector2 controlUV)
            {
                Vector2 a = UVToWorld(anchorUV);
                Vector2 b = UVToWorld(controlUV);
                Vector2 dir = (b - a).normalized;
                Vis.Line(a, b, thickness / 2, widgetTheme.controlLineCol);
            }
        }

        public override Vector2 UVToWorld(Vector2 uv)
        {
            uv.y = Mathf.Lerp(sustain, 1, uv.y);
            return base.UVToWorld(uv);
        }

        public override Vector2 WorldToUV(Vector2 pos)
        {
            Vector2 uv = base.WorldToUV(pos);
            uv.y = Mathf.InverseLerp(sustain, 1, uv.y);
            return uv;
        }


        void DrawCurve(BezierCurve curve)
        {
            Vector2 posAnchorA = UVToWorld(curve.anchorA);
            Vector2 posAnchorB = UVToWorld(curve.anchorB);
            Vector2 posControlA = UVToWorld(curve.controlA);
            Vector2 posControlB = UVToWorld(curve.controlB);

            Vector2 posPrev = posAnchorA;

            for (int i = 1; i < displaySettings.displayResolution; i++)
            {
                float t = i / (displaySettings.displayResolution - 1f);
                Vector2 pos = EvaluateCurve(posAnchorA, posControlA, posControlB, posAnchorB, t);
                Vis.Line(posPrev, pos, thickness, widgetTheme.colMain);
                posPrev = pos;
            }
        }

        void DrawHandle(ref Vector2 controlUV, int controlIndex, bool lockX = false)
        {
            DrawHandle(ref controlUV, controlIndex, ref selectedControlIndex, ref mouseOverID, ref handleSelectionOffset, lockX);

            if (controlIndex == selectedControlIndex) discretizedValuesUpToDate = false;
        }

        void OnValidate()
        {
            discretizedValuesUpToDate = false;
        }


        const int discretizeResolution = 512;


        public override Vector2 GetXAxisMinMax() => axisInfo.minMaxX;
        public override Vector2 GetYAxisMinMax() => axisInfo.minMaxY;


        public DiscreteCurve GetDiscretizedDisplayCurve()
        {
            UpdateDiscreteCurve();
            return discretizedDisplayCurve;
        }

        protected override void UpdateDiscreteCurve()
        {
            if (mappingCurve)
            {
                DiscreteCurve discrete = mappingCurve.GetDiscreteCurve();
                if (discrete != mappingCurveDiscrete)
                {
                    mappingCurveDiscrete = discrete;
                    discretizedValuesUpToDate = false;
                }
            }


            if (!discretizedValuesUpToDate)
            {
                float[] discreteValues = DiscretizeCurve(curve, discretizeResolution);
                // Display curve
                discretizedDisplayCurve = new DiscreteCurve(discreteValues);

                // Amplitude curve
                if (useRemapCurve && mappingCurve)
                {
                    for (int i = 0; i < discreteValues.Length; i++)
                    {
                        float t = i / (discreteValues.Length - 1f);
                        float mapping = mappingCurveDiscrete.Evaluate(discretizedDisplayCurve.Evaluate(t));
                        discreteValues[i] = mapping;
                    }

                    discreteCurve = new DiscreteCurve(discreteValues);
                }
                else discreteCurve = discretizedDisplayCurve;
            }

            discretizedValuesUpToDate = true;
        }

        float[] DiscretizeCurve(BezierCurve curve, int resolution)
        {
            float[] values = new float[resolution];

            for (int i = 0; i < values.Length; i++)
            {
                float t = i / (values.Length - 1f);
                float amplitude = FindPointAtX(curve, t).y;
                values[i] = amplitude;
            }

            return values;
        }

        public static Vector2 EvaluateCurve(Vector2 a1, Vector2 c1, Vector2 c2, Vector2 a2, float t)
        {
            return Maths.CubicBezier(a1, c1, c2, a2, t);
        }

        public static Vector2 EvaluateCubicBezier(BezierCurve curve, float t)
        {
            Vector2 p0 = curve.anchorA;
            Vector2 p1 = curve.controlA;
            Vector2 p2 = curve.controlB;
            Vector2 p3 = curve.anchorB;

            return EvaluateCurve(p0, p1, p2, p3, t);
        }

        const int FixedSearchIterationCount = 16;


        // Search for point as close as can find to given x value
        // Note: each x value assumed to have a unique corresponding y value
        static Vector2 SearchBezierPointAtX(BezierCurve curve, float targetX)
        {
            float t_lower = 0;
            float t_upper = 1;
            Vector2 bestPoint = curve.anchorA;

            for (int i = 0; i < FixedSearchIterationCount; i++)
            {
                // Look up point at current guess for curve time
                float guessT = (t_lower + t_upper) / 2;
                bestPoint = EvaluateCubicBezier(curve, guessT);

                // Restrict the bounds of the search
                if (bestPoint.x > targetX) t_upper = guessT;
                else t_lower = guessT;
            }

            return bestPoint;
        }


        static Vector2 FindPointAtX(BezierCurve curve, float targetX)
        {
            if (targetX <= 0) return new Vector2(0, curve.anchorA.y);
            if (targetX >= 1) return new Vector2(1, curve.anchorB.y);

            Vector2 p0 = curve.anchorA;
            Vector2 p1 = curve.controlA;
            Vector2 p2 = curve.controlB;
            Vector2 p3 = curve.anchorB;

            float t_lower = 0;
            float t_upper = 1;
            float guessT = (t_lower + t_upper) / 2;
            Vector2 point = p0;

            const int fixedIterationCount = 16;

            for (int i = 0; i < fixedIterationCount; i++)
            {
                point = EvaluateCurve(p0, p1, p2, p3, guessT);

                if (point.x > targetX) t_upper = guessT;
                else t_lower = guessT;

                guessT = (t_lower + t_upper) / 2;
            }

            return point;
        }

        public Color CurveCol => widgetTheme.colMain;


        [System.Serializable]
        public class BezierCurve
        {
            public Vector2 anchorA;
            public Vector2 controlA;
            public Vector2 controlB;
            public Vector2 anchorB;
        }

        [System.Serializable]
        public class CurveDisplaySettings
        {
            public int displayResolution = 128;
            public float anchorSize = 0.1f;
            public Vector2 inputFieldSize;
            public float inputFieldTextPad;
        }
    }

    [System.Serializable]
    public class WidgetTheme
    {
        public Color panelCol = Color.white;
        public Color panelBorderCol = Color.white;
        public Color colMain = Color.white;
        public Color colMainHighlight = Color.white;
        public Color amplitudeCurveCol = Color.white;

        // ---- Properties ----
        public Color controlPointCol => Color.black;
        public Color controlPointHoverCol => controlPointOutlineCol;
        public Color controlPointSelectedCol => Color.white;
        public Color controlPointOutlineCol => colMain;
        public Color panelAxisCol => panelBorderCol.WithAlpha(0.25f);
        public Color controlLineCol => ColHelper.Brighten(panelBorderCol, 0.25f);
        public Color titleCol => colMain;
        public Color textSecondaryCol => ColHelper.Darken(colMain, 0.1f);
    }
}