using System;
using System.Collections.Generic;
using Audio.Synth;
using Seb.Helpers;
using Seb.Visualization;
using UnityEngine;

namespace Audio.Tools
{
    [ExecuteAlways]
    public class LineWidget : DiscreteCurveWidgetBase
    {
        public int discreteResolution = 256;
        public bool linearScale;
        public int smoothDst;
        public int smoothIts;


        public AxesWidgetAddon axisInfo;
        public bool showScaleLines = true;
        public Color scaleLinesCol;
        public Color scaleToolLineCol;

        [Header("State")]
        public Vector2[] timeValuePairs = { new Vector2(0, 1), new Vector2(1, 0) };
        public bool currentDataInLinearScale;

        [Header("Debug")]
        public bool showTestLine;


        int selectedID = -1;
        int mouseOverID = -1;
        Vector2 mouseOffset;

        // Scale state
        bool isScaling;
        float scaleStartT;
        int scaleIndex;
        Vector2[] scaleInitState;

        float[] discreteVals;
        bool discreteCurveUpToDate;

        void Update()
        {
            HandleLinearConversion();
            DrawWidget();

            Test();
        }

        void DrawWidget()
        {
            StartLayer();
            DrawPanel(false);
            DrawScaleLines();
            DrawPanelOutline();

            DrawLines();
        }

        void Test()
        {
            if (!showTestLine) return;
            UpdateDiscreteCurve();

            Vector2 posPrev = Vector2.zero;

            for (int i = 0; i < discreteResolution; i++)
            {
                float t = i / (discreteResolution - 1f);
                float x = Left + Size.x * t;
                Vector2 pos = new Vector2(x, Bottom);
                Vector2 posY = pos + Vector2.up * discreteVals[i] * Size.y;
                Vis.Line(pos, posY, thickness / 3, Color.red);

                if (i > 0)
                {
                    Vis.Line(posPrev, posY, thickness / 3, Color.yellow);
                }

                posPrev = posY;
            }
        }

        void DrawScaleLines()
        {
            if (!showScaleLines) return;

            for (int i = 1; i <= 9; i++)
            {
                float ty = i / 10f;
                if (!linearScale) ty = Mathf.Sqrt(ty);

                float y = Bottom + ty * Size.y;
                Vis.LineCentered(new Vector2(0, y), Vector2.right * Size.x / 2, thickness, scaleLinesCol);
            }
        }

        void HandleLinearConversion()
        {
            if (currentDataInLinearScale != linearScale)
            {
                currentDataInLinearScale = linearScale;
                for (int i = 0; i < timeValuePairs.Length; i++)
                {
                    float y = timeValuePairs[i].y;
                    if (linearScale) y *= y;
                    else y = Mathf.Sqrt(y);

                    timeValuePairs[i] = new Vector2(timeValuePairs[i].x, y);
                }
            }
        }

        void DrawLines()
        {
            if (showTestLine) return;
            int mouseOverLineIndex = -1;


            // Lines
            for (int i = 0; i < timeValuePairs.Length - 1; i++)
            {
                Vector2 a = ValToPoint(timeValuePairs[i]);
                Vector2 b = ValToPoint(timeValuePairs[i + 1]);

                float mouseDst = Maths.DistanceToLineSegment(MousePos, a, b);
                bool hover = mouseDst < handleSizeSelected * 1.5f && selectedID == -1 && mouseOverID == -1 && mouseOverLineIndex == -1;
                if (hover) mouseOverLineIndex = i;

                //Color col = theme.colMain;
                Color col = hover ? widgetTheme.colMainHighlight : widgetTheme.colMain;
                Vis.Line(a, b, thickness, col);
            }

            // Split line
            if (mouseOverLineIndex != -1 && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
            {
                Vector2 uv = GetMouseUV(true);
                List<Vector2> edited = new List<Vector2>(timeValuePairs);
                edited.Insert(mouseOverLineIndex + 1, uv);
                timeValuePairs = edited.ToArray();
            }

            // Handles
            bool outOfOrder = false;
            float xPrev = 0;
            mouseOverID = -1;

            for (int i = 0; i < timeValuePairs.Length; i++)
            {
                bool lockX = i == 0 || i == timeValuePairs.Length - 1;
                DrawHandle(ref timeValuePairs[i], i, ref selectedID, ref mouseOverID, ref mouseOffset, lockX);

                if (timeValuePairs[i].x > xPrev)
                {
                    outOfOrder = true;
                    xPrev = timeValuePairs[i].x;
                }
            }

            // Reorder handles
            if (outOfOrder)
            {
                if (selectedID != -1)
                {
                    int[] orderIndices = ArrayHelper.CreateSortedIndices(timeValuePairs, (a, b) => a.x.CompareTo(b.x));
                    selectedID = orderIndices[selectedID];
                }

                ArrayHelper.SortArray(timeValuePairs, (a, b) => a.x.CompareTo(b.x));
            }

            // Delete handle
            bool deleteInput = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete) || InputHelper.IsMouseDownThisFrame(MouseButton.Right);
            if ((mouseOverID != -1 || selectedID != -1) && deleteInput)
            {
                int deleteIndex = selectedID == -1 ? mouseOverID : selectedID;
                if (deleteIndex != 0 && deleteIndex != timeValuePairs.Length - 1)
                {
                    List<Vector2> edited = new List<Vector2>(timeValuePairs);
                    edited.RemoveAt(deleteIndex);
                    timeValuePairs = edited.ToArray();
                    mouseOverID = -1;
                    selectedID = -1;
                    NotifyModified();
                }
            }

            // Scaling
            if (InputHelper.IsMouseHeld(MouseButton.Left) && InputHelper.AltIsHeld)
            {
                float mouseT = GetMouseUV(true).x;
                if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && MouseInBounds)
                {
                    isScaling = true;
                    scaleStartT = mouseT;
                    ArrayHelper.ResizeAndCopy(ref scaleInitState, timeValuePairs);
                }

                if (MouseInBounds) Vis.LineCentered(new Vector2(MousePos.x, 0), Vector2.up * Size.y / 2, thickness, scaleToolLineCol);

                if (isScaling)
                {
                    NotifyModified();
                    //Vis.LineCentered(UVToWorld(new Vector2(scaleStartT, 0.5f)), Vector2.up * Size.y / 2, thickness, scaleToolLineCol);
                    bool scalingLeft = mouseT < scaleStartT;

                    float scaleFrac = scalingLeft ? mouseT / scaleStartT : 1 - Mathf.InverseLerp(scaleStartT, 1, mouseT);

                    for (int i = 0; i < scaleInitState.Length; i++)
                    {
                        float t = scaleInitState[i].x;
                        float y = scaleInitState[i].y;

                        if (t < scaleStartT == scalingLeft)
                        {
                            if (scalingLeft) timeValuePairs[i] = new Vector2(t * scaleFrac, y);
                            else timeValuePairs[i] = new Vector2(1 - (1 - t) * scaleFrac, y);
                        }
                    }
                }
            }

            if (selectedID != -1) NotifyModified();


            return;

            Vector2 ValToPoint(Vector2 timeValuePair)
            {
                float x = (timeValuePair.x - 0.5f) * Size.x;
                float y = (timeValuePair.y - 0.5f) * Size.y;
                return new Vector2(x, y);
            }
        }


        public override Vector2 GetXAxisMinMax() => axisInfo.minMaxX;
        public override Vector2 GetYAxisMinMax() => axisInfo.minMaxY;

        protected override void UpdateDiscreteCurve()
        {
            if (discreteCurveUpToDate) return;
            discreteCurveUpToDate = true;

            Maths.SampleYValsAtEqualIntervals(timeValuePairs, discreteResolution, ref discreteVals);
            if (!currentDataInLinearScale)
            {
                for (int i = 0; i < discreteVals.Length; i++)
                {
                    discreteVals[i] *= discreteVals[i];
                }
            }

            if (smoothDst > 0)
            {
                for (int pass = 0; pass < smoothIts; pass++)
                {
                    float[] smoothCopy = new float[discreteVals.Length];
                    for (int i = 0; i < discreteVals.Length; i++)
                    {
                        if (i == 0 || i == discreteVals.Length - 1)
                        {
                            smoothCopy[i] = discreteVals[i];
                            continue;
                        }

                        float sum = 0;
                        float weightSum = 0;

                        for (int offset = -smoothDst; offset <= smoothDst; offset++)
                        {
                            sum += discreteVals[Mathf.Clamp(i + offset, 0, discreteVals.Length - 1)];
                            weightSum += 1;
                        }

                        smoothCopy[i] = sum / weightSum;
                    }

                    for (int i = 0; i < discreteVals.Length; i++)
                    {
                        discreteVals[i] = smoothCopy[i];
                    }
                }
            }

            discreteCurve = new DiscreteCurve(discreteVals);
        }

        void NotifyModified()
        {
            discreteCurveUpToDate = false;
        }

        void OnValidate()
        {
            NotifyModified();
        }
    }
}