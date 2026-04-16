using System;
using Audio.Display;
using Seb.Helpers;
using Seb.Stuff;
using Seb.Visualization;
using Seb.Visualization.UI;
using UnityEngine;

namespace Audio.Tools
{
    [ExecuteAlways]
    public class AxesWidgetAddon : MonoBehaviour
    {
        public bool drawInputField = true;
        [Header("Axis Values")]
        public Vector2 minMaxX = new(0, 1);
        public Vector2 minMaxY = new(0, 1);

        [Header("Axes")]
        public bool showAxisLabelsX;
        public bool showAxisLabelsY;
        public float xAxisLabelTargetSpacing;
        public float yAxisLabelTargetSpacing;
        public Color labelCol = Color.white;
        public float padFactor = 1;

        [Header("References")]
        public Widget parentWidget;

        UIHandle inputFieldHandle;
        bool isDraggingValue;
        float dragPrevX;

        string[] labelsX;
        string[] labelsY;
        bool labelsUpToDate;

        const float valueDragSensitivity = 10;


        void OnEnable()
        {
            inputFieldHandle = new("Handle", gameObject.GetInstanceID());
            InputFieldState inputFieldState = UI.GetInputFieldState(inputFieldHandle);
            inputFieldState.SetText($"{maxX:0}", false);
        }

        void Update()
        {
            Vector2 pos = new Vector2(parentWidget.Right, parentWidget.Top + parentWidget.titleOffset.y);
            DrawInputField(pos);

            Vis.StartLayerIfNotInMatching(parentWidget.Centre, 1, false);
            DrawAxisLabels();
        }

        void DrawAxisLabels()
        {
            if (!labelsUpToDate)
            {
                labelsUpToDate = true;
                UpdateLabelStrings();
            }


            if (showAxisLabelsX)
            {
                for (int i = 0; i < labelsX.Length; i++)
                {
                    float t = i / (labelsX.Length - 1f);
                    float x = parentWidget.Left + parentWidget.Size.x * t;
                    float y = parentWidget.Bottom - GlobalTheme.ActiveUITheme.textSize / 2 * padFactor;
                    Vector2 pos = new (x, y);

                    Vis.Text(GlobalTheme.ActiveUITheme.font, labelsX[i], GlobalTheme.ActiveUITheme.textSize, pos, Anchor.CentreTop, labelCol);
                }
            }

            if (showAxisLabelsY)
            {
                for (int i = 0; i < labelsY.Length; i++)
                {
                    float t = i / (labelsY.Length - 1f);
                    float x = parentWidget.Left - GlobalTheme.ActiveUITheme.textSize / 2 * padFactor;
                    float y = parentWidget.Bottom + parentWidget.Size.y * t;
                    Vector2 pos = new(x, y);

                    Vis.Text(GlobalTheme.ActiveUITheme.font, labelsY[i], GlobalTheme.ActiveUITheme.textSize, pos, Anchor.CentreRight, labelCol);
                }
            }
        }

        void DrawInputField(Vector2 pos)
        {
            if (!drawInputField) return;
            
            using (UI.CreateWorldSpaceUIScope(parentWidget.Centre))
            {
                InputFieldState inputFieldState = UI.GetInputFieldState(inputFieldHandle);
                if (inputFieldState.focused)
                {
                    inputFieldState.HandleInput();
                }


                const string defaultText = "0";
                int textLength = UI.GetInputFieldState(inputFieldHandle).text.Length;
                if (textLength == 0) textLength = defaultText.Length;
                float width = textLength * Vis.GetMonospaceFontAdvance(GlobalTheme.ActiveUITheme.inputFieldTheme.font, GlobalTheme.ActiveUITheme.inputFieldTheme.fontSize) + GlobalTheme.textPadding * 2;


                Vector2 size = new(width, GlobalTheme.ActiveUITheme.textSize + GlobalTheme.textPadding / 2);

                InputFieldTheme inputFieldTheme = GlobalTheme.ActiveUITheme.inputFieldTheme;
                bool mouseOver = InputHelper.MouseInsideBounds_World(pos - Vector2.right * size.x / 2 + parentWidget.Centre, size);
                inputFieldTheme.bgCol = inputFieldState.focused ? theme.panelCol : mouseOver || isDraggingValue ? theme.panelBorderCol : Color.black;
                inputFieldTheme.focusBorderCol = theme.panelBorderCol;
                inputFieldTheme.textCol = theme.titleCol;
                inputFieldTheme.defaultTextCol = theme.titleCol.WithAlpha(0.5f);

                if (mouseOver && InputHelper.IsMouseDownThisFrame(MouseButton.Right))
                {
                    isDraggingValue = true;
                    dragPrevX = parentWidget.MousePos.x;
                }

                if (isDraggingValue)
                {
                    float dragDeltaX = parentWidget.MousePos.x - dragPrevX;
                    float dragDir = Mathf.Sign(dragDeltaX);
                    dragPrevX = parentWidget.MousePos.x;
                    float valueXNew = maxX + dragDir * Mathf.CeilToInt(Mathf.Abs(dragDeltaX) * valueDragSensitivity * (InputHelper.ShiftIsHeld ? 5 : 1));
                    valueXNew = Mathf.Max(1, valueXNew);
                    UpdateValueXFromUI(valueXNew);
                    inputFieldState.SetText($"{maxX:0}", false);
                    if (InputHelper.IsMouseUpThisFrame(MouseButton.Right)) isDraggingValue = false;
                }


                UI.InputField(inputFieldHandle, inputFieldTheme, pos, size, defaultText, Anchor.CentreRight, +GlobalTheme.textPadding, handleInput: false);


                if (Application.isPlaying)
                {
                    if (int.TryParse(inputFieldState.text, out int result)) UpdateValueXFromUI(result);
                    else minMaxX.y = 0;
                }
            }
        }

        void UpdateValueXFromUI(float valNew)
        {
            if (maxX != valNew)
            {
                minMaxX.y = valNew;
                labelsUpToDate = false;
            }
        }

        void UpdateLabelStrings()
        {
            UpdateLabelStrings(ref labelsX, xAxisLabelTargetSpacing, parentWidget.Size.x, minMaxX);
            UpdateLabelStrings(ref labelsY, yAxisLabelTargetSpacing, parentWidget.Size.y, minMaxY);

            return;

            static void UpdateLabelStrings(ref string[] labels, float targetSpacing, float worldSize, Vector2 minMax)
            {
                int count = Mathf.CeilToInt(worldSize / Mathf.Max(0.1f, targetSpacing));
                ArrayHelper.Resize(ref labels, count);
                float valueRange = minMax.y - minMax.x;

                for (int i = 0; i < count; i++)
                {
                    float t = i / (count - 1f);
                    float val = Mathf.Lerp(minMax.x, minMax.y, t);

                    string text = string.Empty;
                    if (valueRange >= 100) text = $"{val:0}";
                    else text = $"{val:0.0}";

                    labels[i] = text;
                }
            }
        }

        void OnValidate()
        {
            labelsUpToDate = false;
        }

        DisplaySettings GlobalTheme => parentWidget.GlobalTheme;
        WidgetTheme theme => parentWidget.widgetTheme;

        public float minX => minMaxX.x;
        public float maxX => minMaxX.y;
        public float minY => minMaxY.x;
        public float maxY => minMaxY.y;
    }
}