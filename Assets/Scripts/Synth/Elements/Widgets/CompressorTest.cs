using System.Collections.Generic;
using Seb.Helpers;
using Seb.Visualization;
using UnityEngine;
using static Audio.Helpers.AudioHelper;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class CompressorTest : Widget
{
    [Space] public Color thresholdLineCol;
    public Color smoothLineCol;
    public Color maxInputCol;
    public Color labelCol;
    public Color reduceCol;

    [Header("Behaviour")]
    public float dbMin = -80;
    public float dbMax = 0;
    public float threshold;
    public float smoothWidth;
    public float ratio;

    public float attackSpeed;
    public float decaySpeed;

    public int displayResolution = 100;

    [Header("Info")]
    public float info_compressThresholdAmp;

    public float targetReductionDB;
    public float smoothedReductionDB;

    Vector2 clickPos;
    float[] vals;
    bool hasClickFocus;

    int maxDbFrameCounter;
    float maxInputDb_state;
    float maxInputDb_smoothTarget;
    float maxInputSmoothedDb;
    float smoothV;

    List<Vector2> pointsDrawActive = new();
    int pointDrawStartIndex;
    Vector2 size;
    

    void OnEnable()
    {
        maxInputDb_smoothTarget = dbMin;
        maxInputDb_state = dbMin;
        maxInputSmoothedDb = dbMin;
    }

    void Update()
    {
        size = Size;
        DrawWidget();
    }

    void DrawWidget()
    {
        info_compressThresholdAmp = DecibelsToAmplitude(threshold);

        ArrayHelper.Resize(ref vals, displayResolution);

        for (int i = 0; i < displayResolution; i++)
        {
            float t = i / (displayResolution - 1f);

            float dbIn = Lerp(dbMin, dbMax, t);
            float dbOut = ApplyVolumeCurve(dbIn, false);

            float x = t;
            float y = InverseLerp(dbMin, dbMax, dbOut);
            vals[i] = y;
        }

        StartLayer();
        DrawPanel(false);

        float thresholdLeft = threshold - smoothWidth / 2;
        float thresholdRight = threshold + smoothWidth / 2;
        float thresholdX = (InverseLerp(dbMin, dbMax, threshold) - 0.5f) * Size.x;
        float thresholdX_min = (InverseLerp(dbMin, dbMax, thresholdLeft) - 0.5f) * Size.x;
        float thresholdX_max = (InverseLerp(dbMin, dbMax, thresholdRight) - 0.5f) * Size.x;
        Vis.Line(new Vector2(thresholdX_min, -Size.y / 2), new Vector2(thresholdX_min, Size.y / 2), GlobalTheme.lineThickness, smoothLineCol);
        Vis.Line(new Vector2(thresholdX_max, -Size.y / 2), new Vector2(thresholdX_max, Size.y / 2), GlobalTheme.lineThickness, smoothLineCol);
        Vis.Line(new Vector2(thresholdX, -Size.y / 2), new Vector2(thresholdX, Size.y / 2), GlobalTheme.lineThickness, thresholdLineCol);

        Vis.DataLine(vals, -Size.x / 2, Size.x / 2, Size.y, -Size.y / 2, GlobalTheme.lineThickness, widgetTheme.colMain);
        DrawPanelOutline();
        DrawLabels();

        maxInputSmoothedDb = SmoothDamp(maxInputSmoothedDb, maxInputDb_smoothTarget, ref smoothV, Time.deltaTime * 10);
        
        Vector2 maxDbPos = GetPosFromDb(maxInputSmoothedDb, ApplyVolumeCurve(maxInputSmoothedDb, false));
        float rad = GlobalTheme.pointRadius * 1.5f;
        Vis.Point(maxDbPos, rad + GlobalTheme.pointOutlineSize, Color.black);
        Vis.Point(maxDbPos, rad, maxInputCol);

        int numToDraw = pointsDrawActive.Count - pointDrawStartIndex;
        if (Time.frameCount % 10 == 0) pointDrawStartIndex += numToDraw;

        DrawReductionPanel();
        HandleInput();

     
    }

    void DrawReductionPanel()
    {
        float width = Size.x * 0.05f;
        Vector2 centre = Vector2.right * (Size.x / 2 + width / 2 * 2.5f);
        Vector2 size = new Vector2(width, Size.y);
        Vis.Quad(centre, size, widgetTheme.panelCol);

        float reduceT = Mathf.InverseLerp(dbMax, dbMin, -smoothedReductionDB);
        reduceT *= 4; // Exaggerate for visibility (temp)
        Vector2 reduceSize = new Vector2(width, size.y * reduceT);
        Vis.Quad(centre + Vector2.up * (-reduceSize.y / 2 + size.y / 2), reduceSize, reduceCol);

        Color outlineCol = widgetTheme.panelBorderCol;
        Vis.QuadOutline(centre, size, uiTheme.panelOutlineThickness, outlineCol);
    }


    Vector2 GetPosFromDb(float dbIn, float dbOut)
    {
        if (dbIn > dbMax) dbOut = ApplyVolumeCurve(dbMax, false);
        float remapX = (InverseLerp(dbMin, dbMax, dbIn) - 0.5f) * size.x;
        float remapY = (InverseLerp(dbMin, dbMax, dbOut) - 0.5f) * size.y;

        return new Vector2(remapX, remapY);
    }

    void HandleInput()
    {
        if (MouseInBounds && (InputHelper.IsMouseDownThisFrame(MouseButton.Left) || InputHelper.IsMouseDownThisFrame(MouseButton.Right)))
        {
            clickPos = MousePos;
            hasClickFocus = true;
        }

        if (InputHelper.IsMouseUpThisFrame(MouseButton.Left) || InputHelper.IsMouseUpThisFrame(MouseButton.Right)) hasClickFocus = false;


        if (hasClickFocus)
        {
            if (InputHelper.IsMouseHeld(MouseButton.Left))
            {
                // Ratio control
                if (InputHelper.CtrlIsHeld)
                {
                    ratio = Pow(1 + Max(0, clickPos.y - MousePos.y) * 0.5f, 2);
                }
                // Threshold control
                else
                {
                    float db = Lerp(dbMin, dbMax, GetMouseUV(true).x);
                    threshold = db;
                }
            }
            else if (InputHelper.IsMouseHeld(MouseButton.Right))
            {
                // Smooth width control
                float db = Lerp(dbMin, dbMax, GetMouseUV(true).x);
                smoothWidth = Abs(db - threshold) * 2;
            }
        }
    }


    void DrawLabels()
    {
        int num = 7;
        Color col = labelCol;
        float markerWidth = Size.x * 0.01f;

        for (int i = 0; i < num; i++)
        {
            float t = i / (num - 1f);
            float db = Lerp(dbMin, dbMax, t);


            // Horizontal
            float textOffsetX = Size.x * 0.035f;
            float y = Lerp(Bottom, Top, t);
            Vis.LineCentered(new Vector2(Left, y), Vector2.right * markerWidth, uiTheme.panelOutlineThickness, col);
            Vis.Text(uiTheme.font, $"{db:0}", uiTheme.textSize * 0.9f, new Vector2(Left - textOffsetX, y), Anchor.TextCentreRight, col);

            // Vertical
            // Horizontal
            float textOffsetY = Size.y * 0.05f;
            float x = Lerp(Left, Right, t);
            Vis.LineCentered(new Vector2(x, Bottom), Vector2.down * markerWidth, uiTheme.panelOutlineThickness, col);
            Vis.Text(uiTheme.font, $"{db:0}", uiTheme.textSize * 0.9f, new Vector2(x, Bottom - textOffsetY), Anchor.TextCentre, col);
        }
    }


    // 'Compress' the volume of the audio (make loud parts softer)
    public void UpdateCompressor(float[] audioData, float timeStep)
    {
        float maxDbIn = dbMin;

        for (int i = 0; i < audioData.Length; i++)
        {
            float value = audioData[i];
            float absValue = Abs(value);
            float dbIn = AmplitudeToDecibels(absValue);
            maxDbIn = Max(maxDbIn, dbIn);
        }

        
        for (int i = 0; i < audioData.Length; i++)
        {
            if (audioData[i] == 0) continue;

            // Calculate number of decibels to reduce by, based on loudest part of sound
            float inputDB = SignedAmplitudeToDecibels(audioData[i]);
            float reductionDB = inputDB - ApplyVolumeCurve(inputDB, threshold, smoothWidth, ratio);
            targetReductionDB = Max(targetReductionDB, Max(0, reductionDB));

            // Move target gradually back to zero (so that volume returns to full, absent any new loud inputs)
            targetReductionDB -= targetReductionDB * timeStep * decaySpeed;
            // Move smoothly towards the target reduction amount (so that volume fluctuations happen smoothly)
            smoothedReductionDB += (targetReductionDB - smoothedReductionDB) * timeStep * attackSpeed;

            // Reduce volume of current sample using the smoothed reduction amount
            float outputDB = inputDB - smoothedReductionDB;
            audioData[i] = DecibelsToAmplitude(outputDB) * Sign(audioData[i]);
        }

        // Draw data ---


        maxInputDb_state = Max(maxDbIn, maxInputDb_state);
        maxDbFrameCounter++;
        if (maxDbFrameCounter > 10)
        {
            maxDbFrameCounter = 0;
            maxInputDb_smoothTarget = maxInputDb_state;
            maxInputDb_state = dbMin;
        }
    }


    public void UpdateCompressor_Old(float[] values, float dt)
    {
        float maxDbIn = dbMin;

        for (int i = 0; i < values.Length; i++)
        {
            float value = values[i];
            float absValue = Abs(value);
            float dbIn = AmplitudeToDecibels(absValue);
            maxDbIn = Max(maxDbIn, dbIn);

            // Update reduction target
            float dbOut = ApplyVolumeCurve(dbIn);

            if (dbOut < dbIn)
            {
                float dbReduce = dbIn - dbOut;
                targetReductionDB = Max(targetReductionDB, dbReduce);
            }

            float dbNew = dbIn - smoothedReductionDB;
            smoothedReductionDB += (targetReductionDB - smoothedReductionDB) * Clamp01(dt * attackSpeed);
            targetReductionDB += (0 - targetReductionDB) * Clamp01(dt * decaySpeed);

            values[i] = DecibelsToAmplitude(dbNew) * Sign(value);
        }

        // Draw data ---
        maxInputDb_state = Max(maxDbIn, maxInputDb_state);
        maxDbFrameCounter++;
        if (maxDbFrameCounter > 10)
        {
            maxDbFrameCounter = 0;
            maxInputDb_smoothTarget = maxInputDb_state;
            maxInputDb_state = dbMin;
        }
    }


    public float ApplyVolumeCurve(float dbIn, bool isAudio = true)
    {
        float outputDB = ApplyVolumeCurve(dbIn, threshold, smoothWidth, ratio);
        if (isAudio)
        {
            Vector2 p = GetPosFromDb(dbIn, outputDB);
            pointsDrawActive.Add(p);
        }

        return outputDB;

    }


    public static float ApplyVolumeCurve(float inputDB, float thresholdDB, float smoothWidthDB, float ratio)
    {
        float curveStartDB = thresholdDB - smoothWidthDB / 2;
        float excess = inputDB - curveStartDB;

        // Input dB is below threshold, so output is unaltered
        if (excess <= 0) return inputDB;

        // Input dB is above threshold, so reduce the amount beyond the threshold by given ratio
        if (excess >= smoothWidthDB) return thresholdDB + (inputDB - thresholdDB) / ratio;

        // Within smoothing window, interpolate between original and reduced volume
        float outputAtEndOfWindow = thresholdDB + smoothWidthDB / (2 * ratio);
        float smoothT = excess / smoothWidthDB; // [0, 1]
        return QuadraticBezier1D(curveStartDB, thresholdDB, outputAtEndOfWindow, smoothT);
    }

    static float QuadraticBezier1D(float a, float b, float c, float t)
    {
        return t * t * (a - 2 * b + c) + t * 2 * (b - a) + a;
    }
}