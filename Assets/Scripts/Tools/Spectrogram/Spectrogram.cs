using System;
using System.Collections.Generic;
using System.Linq;
using Audio.Analysis;
using Audio.Core;
using Audio.Helpers;
using Seb.Helpers;
using Seb.Stuff;
using Seb.Types;
using Seb.Visualization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static Seb.Helpers.ComputeHelper;

namespace Audio.Tools
{
    public class Spectrogram : MonoBehaviour
    {
        public bool showHzText = true;
        public AudioClip clip;
        public AudioClip[] clipPool;
        public string saveName;

        [Header("STFT Settings")]
        public int windowSize;
        [Range(0.25f, 1)] public float hopSize = 1;
        public bool useHannWindow;
        public bool autoRegenerate;

        [Header("Display Settings")]
        public bool normalizeAmplitude;
        public PaintDisplayMode paintDisplayMode;
        public float frequencyDisplayMin;
        public float frequencyDisplayMax;
        public float loudnessFloorDb;
        public Gradient gradient;
        public bool useGradient;

        [Header("Edit Settings")]
        public bool useDecibelThreshold;
        public float decibelThreshold;

        [Header("Overtone Reference Lines")]
        public NoteInfo referenceNote;
        public bool showOvertoneLines;
        public float overtoneDeltaTest;


        [Header("Frequency Selection")]
        public bool selectedFrequencyDisplayUseDb;
        public bool selectedFreqShowNormalized;
        public Color selectedFrequencyLineCol;
        public Color selectedFrequencyLineFadeCol;
        public Color selectedFrequencyNormalizedLineCol;
        public Color selectedFreqMarkerCol;


        [Header("Display Settings Extra")]
        public FontType font;
        public float fontSize;
        public Vector2 textOffset;
        public Vector2 labelSpacingTarget;
        public Color textCol;
        public Color outlineCol;
        public float outlineThick;
        [Header("Selection Box Settings")]
        public Color selectionBoxCol;
        public Color selectionBoxInvertedCol;
        public float selectionBoxAlpha;
        public float selectionBoxOutlineThickness;
        [Header("Info box settings")]
        public Vector2 mouseInfoOffset;
        public float mouseInfoSizePad;
        public float mouseInfoFontSize;
        public float infoBoxOutlineThickness;
        public Color infoBoxCol;
        public Color infoBoxOutlineCol;
        public Color infoBoxTextCol;

        [Header("Playback Settings")]
        public float playbackAmplitudeFactor = 1;
        
        [Header("References")]
        public MeshRenderer meshRenderer;
        public ComputeShader spectrogramCompute;
        public Paint paint;

        [Header("Info")]
        public float info_frequencySpacing;
        public float info_segmentDurationMs;
        public InputState debugInfo_inputState;
        public float[] info_amplitudesNotepad;

        // State
        InputState inputState;
        Signal signal;
        STFT.StftResult stft;
        bool hasUpdatedSinceSettingsChange;
        float maxAmplitudeInSignal;
        float maxFrequencyInSpectrum;
        Vector2 selectionBoxWorldStart;

        // Textures
        Texture2D spectrogramMap;
        RenderTexture paintMap;
        RenderTexture editedSpectrogram;
        Texture2D gradientTex;

        ComputeKernel paintMapKernel;
        ComputeKernel editMapKernel;
        ComputeKernel readbackKernel;

        static readonly int id_paintInput = Shader.PropertyToID("PaintInput");
        static readonly int id_spectrogramInput = Shader.PropertyToID("SpectrogramMap");
        static readonly int id_editedSpectrogram = Shader.PropertyToID("EditedSpectrogram");
        static readonly int id_paintMap = Shader.PropertyToID("PaintMap");

        readonly List<SelectionBox> selectionBoxes = new();
        ComputeBuffer selectionBoxBuffer;

        const float lineThickness = 0.015f;
        // Selected frequency state
        int selectedFrequencyIndex = -1;
        float[] selectedFrequencyAmplitudes;
        float[] selectedFrequencyDataLineValues;
        float[] selectedFrequencyNormalizedAmplitudeFracs;
        float maxAmplitudeInSelectedFrequency;
        //
        Vector2 measureStartTimeFreq;
        bool multipleSpectrogramsActive;
        bool hasFocus = true;

        public enum InputState
        {
            None,
            CreatingSelectionBox,
            Measuring,
        }

        enum Kernels
        {
            UpdateEditedSpectrogram,
            UpdatePaintMap,
            Readback,
        }

        public enum PaintDisplayMode
        {
            Off,
            PaintOverlay,
            PaintOnly
        }

        void OnEnable()
        {
            Debug.Log("LMB to select | Esc to clear | Tab to switch mode | Space to play sound | S to save | Middle mouse to select frequency | Ctrl to show info | Ctrl + LMB to measure");

            paintMapKernel = ComputeKernel.Create(spectrogramCompute, Kernels.UpdatePaintMap);
            editMapKernel = ComputeKernel.Create(spectrogramCompute, Kernels.UpdateEditedSpectrogram);
            readbackKernel = ComputeKernel.Create(spectrogramCompute, Kernels.Readback);

            meshRenderer.sharedMaterial = new(meshRenderer.sharedMaterial);
            multipleSpectrogramsActive = FindObjectsByType<Spectrogram>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1;

            Regenerate();

        }


        void Update()
        {
            if (!hasUpdatedSinceSettingsChange)
            {
                if (autoRegenerate) Regenerate();
            }

            HandleCompute();
            ApplyShaderParams();
            HandleInput();
            DrawUI();
            paint.SetActive(paintDisplayMode != PaintDisplayMode.Off);
        }

        void HandleCompute()
        {
            RenderTexture paintInputMap = paint.combined;

            spectrogramCompute.SetFloat("maxFrequencyInSpectrum", maxFrequencyInSpectrum);
            spectrogramCompute.SetFloat("frequencyDisplayMin", frequencyDisplayMin);
            spectrogramCompute.SetFloat("frequencyDisplayMax", frequencyDisplayMax);
            spectrogramCompute.SetFloat("maxAmplitudeInSignal", maxAmplitudeInSignal);
            spectrogramCompute.SetBool("normalizeAmplitude", normalizeAmplitude);
            spectrogramCompute.SetFloat("duration", signal.Duration);
            spectrogramCompute.SetInts("resolution", spectrogramMap.width, spectrogramMap.height);

            // ---- Edited spectrogram ----
            {
                spectrogramCompute.SetBool("useDecibelThreshold", useDecibelThreshold);
                spectrogramCompute.SetFloat("decibelThreshold", decibelThreshold);
                spectrogramCompute.SetInt("selectionBoxCount", selectionBoxes.Count);

                ComputeHelper.CreateStructuredBuffer_DontShrink(ref selectionBoxBuffer, selectionBoxes);
                editMapKernel.SetBuffer("SelectionBoxBuffer", selectionBoxBuffer);

                editMapKernel.SetTexture(id_paintInput, paintInputMap);
                editMapKernel.SetTexture(id_spectrogramInput, spectrogramMap);
                editMapKernel.SetTexture(id_editedSpectrogram, editedSpectrogram);
                // Run
                editMapKernel.Dispatch(spectrogramMap.width, spectrogramMap.height);
            }

            // ---- Paint map ----
            if (paintDisplayMode != PaintDisplayMode.Off)
            {
                paintMapKernel.SetTexture(id_paintInput, paintInputMap);
                paintMapKernel.SetTexture(id_spectrogramInput, spectrogramMap);
                paintMapKernel.SetTexture("PaintMap", paintMap);
                // Run
                paintMapKernel.Dispatch(spectrogramMap.width, spectrogramMap.height);
            }
        }

        static bool IsInputActive_SelectingFrequency => InputHelper.IsMouseHeld(MouseButton.Middle);

        void HandleInput()
        {
            bool measureKeyHeld = InputHelper.CtrlIsHeld;
            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) || InputHelper.IsMouseDownThisFrame(MouseButton.Right) || InputHelper.IsMouseDownThisFrame(MouseButton.Middle))
            {
                hasFocus = IsMouseInBounds();
            }

            SelectionBoxInput();
            MiscInput();

            return;

            void MiscInput()
            {
                // Select frequency
                if (IsInputActive_SelectingFrequency)
                {
                    float targetSelectionFrequency = DisplayUVToTimeFreq(WorldToDisplayUV(InputHelper.MousePosWorld)).y;

                    FrequencyData[] spectrum = stft.Segments[0].Spectrum;
                    float minDstFromTargetFrequency = float.MaxValue;

                    for (int i = 0; i < spectrum.Length; i++)
                    {
                        float dst = Mathf.Abs(spectrum[i].Frequency - targetSelectionFrequency);
                        if (dst < minDstFromTargetFrequency)
                        {
                            minDstFromTargetFrequency = dst;
                            selectedFrequencyIndex = i;
                        }
                        else break;
                    }

                    UpdateSelectedFrequencyAmplitudeData();
                }

                if (InputHelper.IsKeyDownThisFrame(KeyCode.Escape))
                {
                    selectedFrequencyIndex = -1;
                    if (inputState is InputState.Measuring) SetInputState(InputState.None);
                }

                // 
                if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && measureKeyHeld)
                {
                    SetInputState(InputState.Measuring);
                    measureStartTimeFreq = WorldToTimeFreq(InputHelper.MousePosWorld);
                }

                if (InputHelper.IsMouseDownThisFrame(MouseButton.Right))
                {
                    if (inputState is InputState.Measuring) SetInputState(InputState.None);
                }

                // Toggle paint modes
                if (InputHelper.IsKeyDownThisFrame(KeyCode.Tab))
                {
                    int numModes = Enum.GetNames(typeof(PaintDisplayMode)).Length;
                    int nextModeIndex = ((int)paintDisplayMode + 1) % numModes;
                    paintDisplayMode = (PaintDisplayMode)nextModeIndex;
                }

                // Save
                if (InputHelper.IsKeyDownThisFrame(KeyCode.S))
                {
                    Signal synthSignal = SpectrogramToSignal();
                    AudioFileHelper.SaveToWav(synthSignal, saveName, true);
                }

                // Play
                if (InputHelper.IsKeyDownThisFrame(KeyCode.Space))
                {
                    if (hasFocus || !multipleSpectrogramsActive)
                    {
                        Signal synthSignal = SpectrogramToSignal(playbackAmplitudeFactor);
                        FindFirstObjectByType<SpectrogramAudioPreview>().Play(synthSignal);
                    }
                }
            }

            void SelectionBoxInput()
            {
                if (paintDisplayMode == PaintDisplayMode.Off)
                {
                    if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && !measureKeyHeld && hasFocus)
                    {
                        SetInputState(InputState.CreatingSelectionBox);
                        selectionBoxWorldStart = InputHelper.MousePosWorld;
                    }

                    if (InputHelper.IsMouseUpThisFrame(MouseButton.Left) && inputState is InputState.CreatingSelectionBox)
                    {
                        Bounds2D selectionBounds = Bounds2D.CreateFromPoints(selectionBoxWorldStart, InputHelper.MousePosWorld);
                        SelectionBox box;
                        box.timeFreqMin = DisplayUVToTimeFreq(WorldToDisplayUV(selectionBounds.Min));
                        box.timeFreqMax = DisplayUVToTimeFreq(WorldToDisplayUV(selectionBounds.Max));
                        box.showInside = InputHelper.AltIsHeld ? 0 : 1;
                        selectionBoxes.Add(box);
                        SetInputState(InputState.None);
                    }

                    // Delete selection box under mouse
                    if (InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete))
                    {
                        Vector2 mouseTimeFreq = DisplayUVToTimeFreq(WorldToDisplayUV(InputHelper.MousePosWorld));
                        int deletionIndex = -1;

                        for (int i = 0; i < selectionBoxes.Count; i++)
                        {
                            if (selectionBoxes[i].Contains(mouseTimeFreq))
                            {
                                deletionIndex = i;
                            }
                        }

                        if (deletionIndex != -1) selectionBoxes.RemoveAt(deletionIndex);
                    }

                    if (InputHelper.IsMouseDownThisFrame(MouseButton.Right))
                    {
                        if (inputState is InputState.CreatingSelectionBox) SetInputState(InputState.None);
                    }

                    if (InputHelper.IsKeyDownThisFrame(KeyCode.Escape))
                    {
                        selectionBoxes.Clear();
                        if (inputState is InputState.CreatingSelectionBox) SetInputState(InputState.None);
                    }
                }
            }

            bool IsMouseInBounds()
            {
                return InputHelper.MouseInsideBounds_World(displayCentre, displaySize);
            }
        }

        void UpdateSelectedFrequencyAmplitudeData()
        {
            maxAmplitudeInSelectedFrequency = 0;
            for (int i = 0; i < stft.Segments.Length; i++)
            {
                if (stft.Segments[i].Spectrum.Length <= selectedFrequencyIndex) continue;
                float amplitude = stft.Segments[i].Spectrum[selectedFrequencyIndex].Amplitude;
                maxAmplitudeInSelectedFrequency = Mathf.Max(amplitude, maxAmplitudeInSelectedFrequency);
            }

            for (int i = 0; i < stft.Segments.Length; i++)
            {
                float amplitude = 0;
                if (stft.Segments[i].Spectrum.Length > selectedFrequencyIndex) amplitude = stft.Segments[i].Spectrum[selectedFrequencyIndex].Amplitude;
                //if (i == 0) amplitude = 0;
                if (i == stft.Segments.Length - 1) amplitude = selectedFrequencyAmplitudes[i - 1];
                selectedFrequencyAmplitudes[i] = amplitude;
                selectedFrequencyNormalizedAmplitudeFracs[i] = amplitude / maxAmplitudeInSelectedFrequency;
            }
        }


        void Regenerate()
        {
            hasUpdatedSinceSettingsChange = true;

            signal = AudioFileHelper.SignalFromAudioClip(clip);
            stft = STFT.Compute(signal, windowSize, hopSize, useHannWindow, true);
            info_frequencySpacing = stft.FrequencySpacing;
            info_segmentDurationMs = stft.SegmentDuration * 1000;

            TextureHelper.TextureFromGradient(ref gradientTex, 256, gradient);

            RegenerateTexture(stft);
            PaintInit();
            ApplyShaderParams();
        }

        void PaintInit()
        {
            paint.Init(meshRenderer.transform.localScale, meshRenderer.transform.position);
        }

        void RegenerateTexture(STFT.StftResult stft)
        {
            int width = stft.Segments.Length;
            int height = stft.Segments[0].Spectrum.Length;

            selectedFrequencyAmplitudes = new float[width];
            selectedFrequencyNormalizedAmplitudeFracs = new float[width];
            selectedFrequencyDataLineValues = new float[width];

            // Raw spectrogram map
            spectrogramMap = new Texture2D(width, height, GraphicsFormat.R32_SFloat, TextureCreationFlags.None)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            // Edited spectrogram map
            CreateRenderTexture(ref editedSpectrogram, width, height, FilterMode.Point, R_SFloat);

            // Paint map
            CreateRenderTexture(ref paintMap, width, height, FilterMode.Point, R_SFloat);

            //
            Color[] cols = new Color[width * height];
            maxFrequencyInSpectrum = stft.Segments[0].Spectrum[^1].Frequency;
            maxAmplitudeInSignal = 0;

            // Store amplitude in red component of time-frequency color map
            for (int timeIndex = 0; timeIndex < width; timeIndex++)
            {
                FrequencyData[] spectrum = stft.Segments[timeIndex].Spectrum;

                for (int freqIndex = 0; freqIndex < spectrum.Length; freqIndex++)
                {
                    float amplitude = spectrum[freqIndex].Amplitude;
                    maxAmplitudeInSignal = Mathf.Max(maxAmplitudeInSignal, amplitude);

                    int pixelIndex = freqIndex * width + timeIndex;
                    cols[pixelIndex].r = amplitude;
                }
            }

            // Create texture
            CreateTexture2D(ref spectrogramMap, width, height, GraphicsFormat.R32_SFloat, FilterMode.Point);
            spectrogramMap.SetPixels(cols);
            spectrogramMap.Apply();
        }


        void ApplyShaderParams()
        {
            Material mat = meshRenderer.sharedMaterial;
            mat.SetFloat("maxFrequencyInSpectrum", maxFrequencyInSpectrum);
            mat.SetFloat("frequencyDisplayMin", frequencyDisplayMin);
            mat.SetFloat("frequencyDisplayMax", frequencyDisplayMax);
            mat.SetFloat("amplitudeMax", maxAmplitudeInSignal);
            mat.SetInt("normalizeAmplitude", normalizeAmplitude ? 1 : 0);
            mat.SetFloat("decibelsDisplayMin", loudnessFloorDb);
            mat.SetFloat("decibelsDisplayMax", AudioHelper.AmplitudeToDecibels(normalizeAmplitude ? 1 : maxAmplitudeInSignal));
            mat.SetInt("useGradient", useGradient ? 1 : 0);
            mat.SetInt("paintMode", (int)paintDisplayMode);
            mat.SetInt("useDimView", selectedFrequencyIndex == -1 ? 0 : 1);

            // ---- Textures ----
            mat.SetTexture(id_editedSpectrogram, editedSpectrogram);
            mat.SetTexture(id_paintMap, paintMap);
            mat.SetTexture("GradientTex", gradientTex);
        }

        public Signal SpectrogramToSignal(float amplitudeFactor = 1)
        {
            bool paintToSound = paintDisplayMode != PaintDisplayMode.Off;
            bool applyWindow = paintToSound;
            bool usePhase = !paintToSound;

            // Get data from texture
            ComputeBuffer buffer = CreateStructuredBuffer<float>(spectrogramMap.width * spectrogramMap.height);
            spectrogramCompute.SetBool("paintToSound", paintToSound);
            readbackKernel.SetTexture(id_paintMap, paintMap);
            readbackKernel.SetTexture("EditedSpectrogram", editedSpectrogram);
            readbackKernel.SetBuffer("ReadbackBuffer", buffer);
            readbackKernel.Dispatch(spectrogramMap.width, spectrogramMap.height);

            float[] data = new float[buffer.count];
            buffer.GetData(data);
            Release(buffer);

            // Create stft from data
            STFT.StftSegment[] synthSegs = new STFT.StftSegment[stft.Segments.Length];
            int index = 0;

            for (int i = 0; i < stft.Segments.Length; i++)
            {
                STFT.StftSegment source = stft.Segments[i];
                FrequencyData[] spectrum = new FrequencyData[source.Spectrum.Length];
                for (int j = 0; j < spectrum.Length; j++)
                {
                    spectrum[j].Frequency = source.Spectrum[j].Frequency;
                    spectrum[j].Phase = usePhase ? source.Spectrum[j].Phase : 0;
                    spectrum[j].Amplitude = data[index];
                    index++;
                }

                STFT.StftSegment seg = new(spectrum, source.SampleOffset, source.SampleCount);
                synthSegs[i] = seg;
            }


            STFT.StftResult synth = new(synthSegs, signal.SampleRate, signal.NumSamples, stft.useWindow);
            Signal reconstructed = STFT.ReconstructSignal(synth, applyWindow);
            
            if (amplitudeFactor != 1)
            {
                for (int i = 0; i < reconstructed.Samples.Length; i++)
                {
                    reconstructed.Samples[i] *= amplitudeFactor;
                }
                Debug.Log(amplitudeFactor);
            }

            Debug.Log($"Generated signal from spectrogram: usePhase = {usePhase} applyWindow = {applyWindow}");

            return reconstructed;
        }

        void DrawUI()
        {
            Vis.StartLayer(Vector2.zero, 1, false);
            Vector2 centre = meshRenderer.transform.position;
            Vector2 size = meshRenderer.transform.localScale;
            Vector2 bottomLeft = centre - size / 2;

            DrawLabels();
            Vis.StartLayer(Vector2.zero, 1, false);
            DrawOutline();
            DrawSelectionBoxes();
            DrawSelectedFrequencyInfo();
            DrawSpectrumInfoAtMouse();
            DrawMeasure();
            DrawOvertoneReferenceLines();
            return;

            void DrawOvertoneReferenceLines()
            {
                if (!showOvertoneLines) return;
                float fundamentalFrequency = NoteHelper.CalculateFrequency(referenceNote) + overtoneDeltaTest;
                const int MaxOvertoneDisplayCount = 100;

                for (int i = 0; i < MaxOvertoneDisplayCount; i++)
                {
                    float freq = fundamentalFrequency * (i + 1);

                    if (freq < frequencyDisplayMin) continue;
                    if (freq > frequencyDisplayMax) break;

                    Vector2 uv = TimeFreqToDisplayUV(new Vector2(timeDisplayMax, freq));
                    Vector2 pos = DisplayUVToWorld(uv);

                    Vis.LineCentered(pos, Vector2.right * 0.1f, lineThickness, Color.red);
                }
            }

            void DrawMeasure()
            {
                if (inputState is not InputState.Measuring) return;

                Vector2 measureEndTimeFreq = WorldToTimeFreq(InputHelper.MousePosWorld);
                float yFrac = TimeFreqToDisplayUV(measureEndTimeFreq).y;


                float timeDelta = Mathf.Abs(measureStartTimeFreq.x - measureEndTimeFreq.x);
                string text = $"t0: {measureStartTimeFreq.x * 1000:0} ms\nt1: {measureEndTimeFreq.x * 1000:0} ms\nΔt: {timeDelta * 1000:0} ms";

                Vector2 measureStartWorldPos = DisplayUVToWorld(TimeFreqToDisplayUV(measureStartTimeFreq));
                Vis.Line(measureStartWorldPos, InputHelper.MousePosWorld, lineThickness, Color.white);
                DrawMarkerDot(measureStartWorldPos);

                DrawInfoBoxAtMouse(text);
            }

            void DrawSelectedFrequencyInfo()
            {
                if (selectedFrequencyIndex == -1) return;

                float selectedFrequency = stft.Segments[0].Spectrum[selectedFrequencyIndex].Frequency;

                // Draw selected frequency marker line
                Vector2 frequencyDisplayPosLeft = DisplayUVToWorld(TimeFreqToDisplayUV(new Vector2(timeDisplayMin, selectedFrequency)));
                Vector2 frequencyDisplayPosRight = DisplayUVToWorld(TimeFreqToDisplayUV(new Vector2(timeDisplayMax, selectedFrequency)));
                Vector2 markerOffset = Vector2.right * 0.15f;
                if (IsInputActive_SelectingFrequency)
                {
                    Vis.LineCentered(new Vector2(displayCentre.x, frequencyDisplayPosRight.y), Vector2.right * displaySize.x / 2, lineThickness, selectedFreqMarkerCol.WithAlpha(0.5f));
                }

                Vis.LineCentered(frequencyDisplayPosRight, markerOffset, lineThickness, selectedFreqMarkerCol);
                Vis.LineCentered(frequencyDisplayPosLeft, markerOffset, lineThickness, selectedFreqMarkerCol);
                if (showHzText) Vis.Text(font, $"{Mathf.CeilToInt(selectedFrequency)} Hz", mouseInfoFontSize, frequencyDisplayPosRight + markerOffset + Vector2.right * 0.1f, Anchor.CentreLeft, selectedFreqMarkerCol);

                Vis.Text(font, $"Peak: {maxAmplitudeInSelectedFrequency / maxAmplitudeInSignal:0.000}", fontSize, centre + new Vector2(-size.x / 2, size.y / 2 + 0.1f), Anchor.BottomLeft, Color.white);

                if (selectedFrequencyDisplayUseDb)
                {
                    float dbMax = AudioHelper.AmplitudeToDecibels(normalizeAmplitude ? 1 : maxAmplitudeInSignal);
                    for (int i = 0; i < selectedFrequencyAmplitudes.Length; i++)
                    {
                        float amp = selectedFrequencyAmplitudes[i];
                        if (normalizeAmplitude) amp /= maxAmplitudeInSignal;
                        float db = AudioHelper.AmplitudeToDecibels(amp);
                        selectedFrequencyDataLineValues[i] = Mathf.InverseLerp(loudnessFloorDb, dbMax, db);
                    }
                }
                else
                {
                    for (int i = 0; i < selectedFrequencyAmplitudes.Length; i++)
                    {
                        if (normalizeAmplitude) selectedFrequencyDataLineValues[i] = selectedFrequencyAmplitudes[i] / maxAmplitudeInSignal;
                        else selectedFrequencyDataLineValues[i] = selectedFrequencyAmplitudes[i];
                    }
                }

                // Draw selected frequency amplitudes
                if (selectedFreqShowNormalized) Vis.DataLine(selectedFrequencyNormalizedAmplitudeFracs, displayMinX, displayMaxX, displaySize.y, -displaySize.y / 2, lineThickness, selectedFrequencyNormalizedLineCol);

                Color lineCol = (inputState is InputState.Measuring) ? selectedFrequencyLineFadeCol : selectedFrequencyLineCol;
                Vis.DataLine(selectedFrequencyDataLineValues, displayMinX, displayMaxX, displaySize.y, -displaySize.y / 2 + centre.y, lineThickness + 0.001f, lineCol);
            }

            void DrawSpectrumInfoAtMouse()
            {
                if (!InputHelper.CtrlIsHeld) return;
                if (inputState is InputState.Measuring) return;

                // Get info
                Vector2 mouseWorld = InputHelper.MousePosWorld;
                Vector2 mouseDisplayUV = WorldToDisplayUV(mouseWorld);
                Vector2 mouseTimeFreq = DisplayUVToTimeFreq(mouseDisplayUV);

                Vector2 mouseFullUV = RemapDisplayUVToFullUV(mouseDisplayUV);
                float amplitude = spectrogramMap.GetPixelBilinear(mouseFullUV.x, mouseFullUV.y).r;
                float amplitudePercent = amplitude / maxAmplitudeInSignal * 100f;

                string text = $"Time: {mouseTimeFreq.x:0.00}\nFreq: {mouseTimeFreq.y:0}\nAmp%: {amplitudePercent:0.00}\n";
                DrawInfoBoxAtMouse(text);
            }

            void DrawInfoBoxAtMouse(string text)
            {
                // Get info
                Vector2 mouseWorld = InputHelper.MousePosWorld;
                Vector2 mouseDisplayUV = WorldToDisplayUV(mouseWorld);

                // Create text
                Vector2 textSize = Vis.CalculateTextBoundsSize(text, mouseInfoFontSize, font);

                // Draw dot under mouse to show source
                DrawMarkerDot(mouseWorld);

                // Draw box
                Vector2 infoBoxSize = textSize + Vector2.one * mouseInfoSizePad;
                float boxDirY = mouseDisplayUV.y > 0.8 ? -1 : 1;
                Vector2 infoBoxCentre = mouseWorld + (Vector2.up * infoBoxSize.y / 2 + mouseInfoOffset) * boxDirY;

                Vis.Quad(infoBoxCentre, infoBoxSize, infoBoxCol);
                Vis.QuadOutline(infoBoxCentre, infoBoxSize, infoBoxOutlineThickness, infoBoxOutlineCol);

                // Draw text
                Vector2 textPos = infoBoxCentre + new Vector2(-textSize.x, textSize.y) / 2;
                Vis.Text(font, text, mouseInfoFontSize, textPos, Anchor.TopLeft, infoBoxTextCol);
            }

            void DrawMarkerDot(Vector2 posWorld)
            {
                const float dotRadius = 0.065f;
                Vis.Point(posWorld, dotRadius, infoBoxCol);
                Vis.PointOutline(posWorld, dotRadius, infoBoxOutlineThickness * 2, infoBoxOutlineCol);
            }

            void DrawSelectionBoxes()
            {
                foreach (SelectionBox box in selectionBoxes)
                {
                    Vector2 worldMin = DisplayUVToWorld(TimeFreqToDisplayUV(box.timeFreqMin));
                    Vector2 worldMax = DisplayUVToWorld(TimeFreqToDisplayUV(box.timeFreqMax));
                    DrawBox(worldMin, worldMax, box.showInside == 1);
                }

                if (inputState is InputState.CreatingSelectionBox)
                {
                    Bounds2D selectionBounds = Bounds2D.CreateFromPoints(selectionBoxWorldStart, InputHelper.MousePosWorld);
                    DrawBox(selectionBounds.Min, selectionBounds.Max, !InputHelper.AltIsHeld);
                }

                void DrawBox(Vector2 worldMin, Vector2 worldMax, bool showInside)
                {
                    Color col = (showInside) ? selectionBoxCol : selectionBoxInvertedCol;
                    Vis.QuadMinMax(worldMin, worldMax, col.WithAlpha(selectionBoxAlpha));
                    Vis.QuadOutlineMinMax(worldMin, worldMax, selectionBoxOutlineThickness, col);
                }
            }

            void DrawOutline()
            {
                Vis.QuadOutline(centre, size, outlineThick, outlineCol);
            }

            void DrawLabels()
            {
                labelSpacingTarget = Vector2.Max(Vector2.one * 0.25f, labelSpacingTarget);
                // X labels
                {
                    int numX = (int)(size.x / labelSpacingTarget.x);
                    numX = Mathf.Max(2, numX);

                    for (int i = 0; i < numX; i++)
                    {
                        float t = i / (numX - 1f);
                        float duration = stft.SampleCount / (float)stft.SampleRate;
                        float s = duration * t;
                        Vector2 pos = new Vector2(bottomLeft.x + size.x * t, bottomLeft.y + textOffset.y);
                        string text = $"{s:0.##}";
                        Vis.Text(font, text, fontSize, pos, Anchor.CentreTop, textCol);
                    }
                }
                // Y labels
                {
                    int numY = (int)(size.y / labelSpacingTarget.y);
                    numY = Mathf.Max(2, numY);
                    float alpha = selectedFrequencyIndex != -1 ? 0.35f : 1;
                    Color col = textCol;
                    col.a *= alpha;

                    //float fracVisibleY = (frequencyDisplayMax - frequencyDisplayMin) / maxFrequencyInSpectrum;
                    //float pixelsVisibleY = spectrogramMap.height * fracVisibleY;
                    //float pixelSizeY = size.y / pixelsVisibleY;
                    //float startY = bottomLeft.y + pixelSizeY / 2;
                    //float endY = bottomLeft.y + size.y - pixelSizeY / 2;

                    for (int i = 0; i < numY; i++)
                    {
                        float t = i / (numY - 1f);
                        float freq = Mathf.Lerp(frequencyDisplayMin, frequencyDisplayMax, t);
                        string text = $"{freq:0.##}";
                        if (freq >= 1000) text = $"{freq / 1000:0.#}k";
                        //float y = Mathf.Lerp(startY, endY, t);
                        Vector2 pos = new Vector2(bottomLeft.x + textOffset.x, bottomLeft.y + size.y * t);

                        Vis.Text(font, text, fontSize, pos, Anchor.TextCentreRight, col);
                    }
                }
            }
        }

        void SetInputState(InputState state)
        {
            //Debug.Log(inputState + " -> " + state);
            inputState = state;
            debugInfo_inputState = state;
        }

        // ---- Space conversion functions ----

        Vector2 WorldToTimeFreq(Vector2 worldPos)
        {
            return DisplayUVToTimeFreq(WorldToDisplayUV(worldPos));
        }

        Vector2 DisplayUVToTimeFreq(Vector2 uv)
        {
            float time = uv.x * signal.Duration;
            float freq = Mathf.Lerp(frequencyDisplayMin, frequencyDisplayMax, uv.y);
            return new Vector2(time, freq);
        }

        Vector2 WorldToDisplayUV(Vector2 worldPos)
        {
            Vector2 bottomLeft = displayCentre - displaySize / 2;
            Vector2 topRight = bottomLeft + displaySize;

            float u = Mathf.InverseLerp(bottomLeft.x, topRight.x, worldPos.x);
            float v = Mathf.InverseLerp(bottomLeft.y, topRight.y, worldPos.y);
            return new Vector2(u, v);
        }

        Vector2 TimeFreqToDisplayUV(Vector2 timeFreq)
        {
            float timeT = Mathf.InverseLerp(timeDisplayMin, timeDisplayMax, timeFreq.x);
            float freqT = Mathf.InverseLerp(frequencyDisplayMin, frequencyDisplayMax, timeFreq.y);
            return new Vector2(timeT, freqT);
        }

        Vector2 DisplayUVToWorld(Vector2 displayUV)
        {
            Vector2 bottomLeft = displayCentre - displaySize / 2;

            float worldX = bottomLeft.x + displaySize.x * displayUV.x;
            float worldY = bottomLeft.y + displaySize.y * displayUV.y;
            return new Vector2(worldX, worldY);
        }

        Vector2 RemapDisplayUVToFullUV(Vector2 displayUV)
        {
            float freq = Mathf.Lerp(frequencyDisplayMin, frequencyDisplayMax, displayUV.y);
            float freqT = freq / maxFrequencyInSpectrum;
            float timeT = displayUV.x;

            return new Vector2(timeT, freqT);
        }

        Vector2 RemapUVToDisplayUV(Vector2 uv)
        {
            float timeT = uv.x;
            float freqT = uv.y;
            // Remap frequency to display range
            float frequency = freqT * maxFrequencyInSpectrum;
            freqT = frequency / maxFrequencyInSpectrum;

            return new Vector2(timeT, freqT); //
        }

        float timeDisplayMin => 0;
        float timeDisplayMax => signal.Duration;

        float displayMinX => displayCentre.x - displaySize.x / 2;
        float displayMaxX => displayCentre.x + displaySize.x / 2;
        Vector2 displayCentre => meshRenderer.transform.position;
        Vector2 displaySize => meshRenderer.transform.localScale;

        void OnDestroy()
        {
            Release(paintMap, editedSpectrogram);
            Release(selectionBoxBuffer);
        }

        void OnValidate()
        {
            hasUpdatedSinceSettingsChange = false;
        }
        

        struct SelectionBox
        {
            public Vector2 timeFreqMin;
            public Vector2 timeFreqMax;
            public int showInside;

            public bool Contains(Vector2 timeFreq)
            {
                return timeFreq.x >= timeFreqMin.x && timeFreq.x <= timeFreqMax.x && timeFreq.y >= timeFreqMin.y && timeFreq.y <= timeFreqMax.y;
            }
        }
    }
}