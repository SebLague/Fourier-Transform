using Seb.Helpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Experimental.Rendering;

namespace Audio.Tools
{
    public class Paint : MonoBehaviour
    {
        [Header("Init Settings")]
        public int resolutionX = 1920;

        [Header("Runtime Settings")]
        public float brushRadius = 6;
        [Range(0, 1)] public float brushSmoothT = 0.5f;

        public Color backgroundColour = Color.black;
        [Range(0, 0.1f)] public float temporalSmoothTime = 0.01f;
        [Range(0, 0.1f)] public float pressureSmoothTime;
        [Range(0, 1f)] public float alphaMultiplier = 1f;
        public Color brushColour = Color.white;
        public Color brushColourAlt = Color.white;
        public AnimationCurve pressureCurve;

        int count;

        [Header("References")]
        public ComputeShader paintCompute;
        public Transform brushDisplay;

        [Header("Info")]
        public Vector2Int info_resolution;

        Camera cam;
        RenderTexture canvas;
        RenderTexture activeStrokeCanvas;
        [HideInInspector] public RenderTexture combined;

        Vector2 lastDrawPos;
        float pressurePrev;

        Vector2 smoothV;
        float pressureSmoothV;
        bool isResizingBrush;
        Vector2 brushResizeStartCoord;
        Vector2 brushResizeStartWorld;

        Vector2 worldSize;
        Vector2 worldCentre;
        bool isInit;
        bool isActive = true;

        const int drawStrokeKernel = 0;
        const int strokeCompletedKernel = 1;
        const int fillBackgroundKernel = 2;
        const int combineKernel = 3;


        public void Init(Vector2 worldSize, Vector2 worldCentre)
        {
            isInit = true;
            this.worldSize = worldSize;
            this.worldCentre = worldCentre;
            cam = Camera.main;

            const GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat;
            const FilterMode filter = FilterMode.Trilinear;

            int resolutionY = Mathf.RoundToInt(resolutionX * (worldSize.y / worldSize.x));

            info_resolution = new Vector2Int(resolutionX, resolutionY);
            ComputeHelper.CreateRenderTexture(ref canvas, resolutionX, resolutionY, filter, format, "Canvas");
            ComputeHelper.CreateRenderTexture(ref activeStrokeCanvas, resolutionX, resolutionY, filter, format, "Stroke Canvas");
            ComputeHelper.CreateRenderTexture(ref combined, resolutionX, resolutionY, filter, format, "Combined Draw");

            BindCanvas();


            FillBackground(backgroundColour);
        }

        void Update()
        {
            brushDisplay.gameObject.SetActive(isActive);

            if (!isInit || !isActive) return;

            EditorOnlyUpdate();
            HandleDrawing();
            HandleKeyboardShortcuts();
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        void HandleDrawing()
        {
            float pixelWorldSize = worldSize.x / resolutionX;

            if (Keyboard.current.ctrlKey.isPressed && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isResizingBrush = true;
                brushResizeStartCoord = CalculateBrushCoord();
                brushResizeStartWorld = BrushPosWorld();
            }

            brushRadius += Mouse.current.scroll.y.ReadValue() / 256 * 10f;
            brushRadius = Mathf.Max(1, brushRadius);

            if (isResizingBrush)
            {
                brushRadius = (brushResizeStartCoord - CalculateBrushCoord()).magnitude;
            }

            Vector2 targetPenPos = CalculateBrushCoord();
            bool isDrawing = Mouse.current.leftButton.isPressed && !isResizingBrush;
            bool startedDrawingThisFrame = Mouse.current.leftButton.wasPressedThisFrame;

            if (startedDrawingThisFrame)
            {
                lastDrawPos = targetPenPos;
            }

            Vector2 brushCoord = Vector2.SmoothDamp(lastDrawPos, targetPenPos, ref smoothV, temporalSmoothTime);
            Vector2 brushPosWord = BrushPosWorld();
            brushDisplay.position = isResizingBrush ? new Vector3(brushResizeStartWorld.x, brushResizeStartWorld.y, -1) : new Vector3(brushPosWord.x, brushPosWord.y, -1);

            brushDisplay.transform.localScale = new Vector3(pixelWorldSize, pixelWorldSize, 1) * brushRadius * 2;

            if (isDrawing)
            {
                if ((lastDrawPos - brushCoord).magnitude > 0 || startedDrawingThisFrame)
                {
                    DrawLine(lastDrawPos, brushCoord);
                    lastDrawPos = brushCoord;
                }
            }

            lastDrawPos = brushCoord;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isResizingBrush = false;
                if (isDrawing) OnStrokeCompleted();
            }


            ComputeHelper.Dispatch(paintCompute, canvas.width, canvas.height, kernelIndex: combineKernel);
        }

        void HandleKeyboardShortcuts()
        {
            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                FillBackground(backgroundColour);
            }
        }

        Vector2 BrushPosScreenSpace()
        {
            Vector2 pos = (TabletPenInRange()) ? Pen.current.position.ReadValue() : Mouse.current.position.ReadValue();
            return pos;
        }

        Vector2 BrushPosWorld()
        {
            Vector2 posScreenSpace = BrushPosScreenSpace();
            Vector2 posWorld = cam.ScreenToWorldPoint(posScreenSpace);
            return posWorld;
        }

        // Calculates pen position relative to canvas (0,0) = bottom left; (width-1, height-1) = top right
        Vector2 CalculateBrushCoord()
        {
            Vector2 posWorld = BrushPosWorld();
            Vector2 canvasCentre = worldCentre;
            Vector2 canvasSize = worldSize;

            Vector2 canvasMin = canvasCentre - canvasSize / 2;
            Vector2 canvasMax = canvasCentre + canvasSize / 2;

            double tx = InverseLerp(canvasMin.x, canvasMax.x, posWorld.x);
            double ty = InverseLerp(canvasMin.y, canvasMax.y, posWorld.y);
            double posX = tx * (canvas.width - 1);
            double posY = ty * (canvas.height - 1);

            return new Vector2((float)posX, (float)posY);

            double InverseLerp(double a, double b, double value)
            {
                return System.Math.Clamp((value - a) / (b - a), 0, 1);
            }
        }

        void DrawLine(Vector2 start, Vector2 end)
        {
            // Get bounding box of line (+ brush radius)
            int boundsMinX = (int)Mathf.Clamp(Mathf.Min(start.x, end.x) - brushRadius, 0, canvas.width - 1);
            int boundsMinY = (int)Mathf.Clamp(Mathf.Min(start.y, end.y) - brushRadius, 0, canvas.height - 1);
            int boundsMaxX = Mathf.CeilToInt(Mathf.Clamp(Mathf.Max(start.x, end.x) + brushRadius, 0, canvas.width - 1));
            int boundsMaxY = Mathf.CeilToInt(Mathf.Clamp(Mathf.Max(start.y, end.y) + brushRadius, 0, canvas.height - 1));
            int boundsWidth = boundsMaxX - boundsMinX;
            int boundsHeight = boundsMaxY - boundsMinY;

            // Dispatch compute shader to draw line inside bounding box
            if (boundsWidth > 0 && boundsHeight > 0)
            {
                paintCompute.SetVector("lineStart", start);
                paintCompute.SetVector("lineEnd", end);
                paintCompute.SetInts("boundsBottomLeft", boundsMinX, boundsMinY);

                // Brush settings
                float pressure = TabletPenInRange() ? Mathf.Clamp01(pressureCurve.Evaluate(Pen.current.pressure.ReadValue())) : 1;
                pressure = Mathf.SmoothDamp(pressurePrev, pressure, ref pressureSmoothV, pressureSmoothTime);
                paintCompute.SetFloat("pressure", pressure);
                paintCompute.SetFloat("pressurePrevious", pressurePrev);
                pressurePrev = pressure;
                //float pressureAlpha = Mathf.InverseLerp(0.05f, 0.5f, pressure);
                paintCompute.SetFloat("brushRadius", brushRadius);
                paintCompute.SetFloat("brushSmoothT", brushSmoothT);
                paintCompute.SetFloat("alphaMultiplier", alphaMultiplier);

                Color c = brushColour;
                paintCompute.SetVector("brushColour", InputHelper.AltIsHeld ? brushColourAlt : c);

                ComputeHelper.Dispatch(paintCompute, boundsWidth, boundsHeight, kernelIndex: drawStrokeKernel);
            }
        }

        void OnStrokeCompleted()
        {
            ComputeHelper.Dispatch(paintCompute, canvas.width, canvas.height, kernelIndex: strokeCompletedKernel);
        }

        void FillBackground(Color colour)
        {
            paintCompute.SetVector("fillColour", colour);
            ComputeHelper.Dispatch(paintCompute, canvas.width, canvas.height, kernelIndex: fillBackgroundKernel);
        }

        void BindCanvas()
        {
            paintCompute.SetTexture(strokeCompletedKernel, "Canvas", canvas);
            paintCompute.SetTexture(fillBackgroundKernel, "Canvas", canvas);
            paintCompute.SetTexture(combineKernel, "Canvas", canvas);

            paintCompute.SetTexture(drawStrokeKernel, "StrokeCanvas", activeStrokeCanvas);
            paintCompute.SetTexture(strokeCompletedKernel, "StrokeCanvas", activeStrokeCanvas);
            paintCompute.SetTexture(fillBackgroundKernel, "StrokeCanvas", activeStrokeCanvas);
            paintCompute.SetTexture(combineKernel, "StrokeCanvas", activeStrokeCanvas);

            paintCompute.SetTexture(combineKernel, "Result", combined);

            paintCompute.SetInts("size", canvas.width, canvas.height);
        }

        bool TabletPenInRange()
        {
            return Pen.current.inRange.isPressed;
        }

        void EditorOnlyUpdate()
        {
            // Rebind in editor so continues working if script/shader is recompiled
            if (Application.isEditor)
            {
                BindCanvas();
            }
        }

        void OnDestroy()
        {
            ComputeHelper.Release(canvas, activeStrokeCanvas, combined);
        }

        public void SetBrushColour(Color col)
        {
            brushColour = col;
        }
    }
}