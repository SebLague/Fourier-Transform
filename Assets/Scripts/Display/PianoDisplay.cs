using System;
using System.Collections.Generic;
using System.Linq;
using Audio.Helpers;
using Audio.Tools;
using Seb.Helpers;
using UnityEngine;
using Seb.Visualization;

namespace Audio.Display
{
    [ExecuteAlways]
    public class PianoDisplay : MonoBehaviour
    {
        public event Action<int, float> OnKeyPressed;
        public event Action<int> OnKeyReleased;

        [Header("Settings")]
        [SerializeField] public NoteLetter startNoteLetter = NoteLetter.C;
        [SerializeField] public int startNoteOctave = 2;
        [SerializeField] public int keyCount = 61;

        [Header("Input")]
        [SerializeField] NoteInfo inputStartNote;

        [Header("Display")]
        [SerializeField] Vector2 keyboardSize;
        [SerializeField] Vector2 blackKeyScale = new(0.65f, 0.55f);

        [SerializeField] float noteSpacing;
        [SerializeField] float sharpOffsetX;

        [SerializeField] KeyCol keyColsWhite;
        [SerializeField] KeyCol keyColsBlack;

        [Header("Display Extra")]
        [SerializeField] float outlineThickness;
        [SerializeField] float keyEndBandHeight;
        [SerializeField] float keyEndWidthFrac = 1;
        [SerializeField] Color colBlackTip;
        [SerializeField] Color colWhiteTip;
        [SerializeField] Color colOutline;
        [SerializeField] Color colBg;

        // State
        NoteInfo startNote;
        PianoKey[] pianoKeys;
        PianoKey keyUnderMouse;
        PianoKey keyHeldByMouse;
        string synthDisplayName;
        Dictionary<int, PianoKey> keyMap;

        KeyCode[] keysQwerty =
        {
            KeyCode.Q,
            KeyCode.W,
            KeyCode.E,
            KeyCode.R,
            KeyCode.T,
            KeyCode.Y,
            KeyCode.U,
            KeyCode.I,
            KeyCode.O,
            KeyCode.P,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
        };


        void Start()
        {
            if (Application.isPlaying)
            {
                MidiListener.Subscribe(OnMidiNotePlayed, OnMidiNoteReleased);
            }
        }

        void OnMidiNotePlayed(NoteInfo noteInfo, float velocity)
        {
            SetKeyDown(noteInfo.MidiIndex, velocity);
        }

        void OnMidiNoteReleased(NoteInfo noteInfo)
        {
            SetKeyUp(noteInfo.MidiIndex);
        }


        void Update()
        {
            BuildKeyboardIfNeeded();
            LayoutKeyboard();

            Vector2 keyboardPos = (Vector2)transform.position;

            HandleKeyboardInput();
            HandleMouseInput(keyboardPos);
            DrawPiano(keyboardPos);
        }

        void BuildKeyboardIfNeeded()
        {
            if (pianoKeys == null || pianoKeys.Length != keyCount || startNote.noteLetter != startNoteLetter || startNote.octaveNumber != startNoteOctave)
            {
                startNote = new NoteInfo(startNoteLetter, startNoteOctave, false);
                pianoKeys = BuildKeyboard(startNote, keyCount);
                keyMap = pianoKeys.ToDictionary(k => k.midiID, k => k);
            }
        }

        static PianoKey[] BuildKeyboard(NoteInfo startNote, int keyCount)
        {
            PianoKey[] pianoKeys = new PianoKey[keyCount];
            PianoKey keyPrev = null;
            NoteInfo note = startNote;

            for (int i = 0; i < pianoKeys.Length; i++)
            {
                PianoKey key = new()
                {
                    noteInfo = note,
                    midiID = MidiHelper.GetMidiNoteIndex(note),
                    semitoneDown = keyPrev,
                    name = note.NameShort
                };
                pianoKeys[i] = key;
                keyPrev = key;
                note = NoteHelper.SemitoneUp(note);
            }

            // Sort white keys first (since black keys drawn on top)
            ArrayHelper.SortArray(pianoKeys, (a, b) => a.noteInfo.isSharp.CompareTo(b.noteInfo.isSharp));
            return pianoKeys;
        }

        void LayoutKeyboard()
        {
            int naturalCount = 0;
            foreach (PianoKey key in pianoKeys)
            {
                if (!key.noteInfo.isSharp) naturalCount++;
            }

            // Layout
            float whiteKeyWidth = Maths.GetElementSizeForSpacedLayout(naturalCount, keyboardSize.x, noteSpacing);
            Vector2 whiteKeySize = new(whiteKeyWidth, keyboardSize.y);
            Vector2 whiteKeyPos = new(-keyboardSize.x / 2 + whiteKeyWidth / 2, 0);
            Vector2 blackKeySize = new Vector2(whiteKeyWidth * blackKeyScale.x, keyboardSize.y * blackKeyScale.y);

            foreach (PianoKey key in pianoKeys)
            {
                if (key.noteInfo.isSharp)
                {
                    float posX = key.semitoneDown.pos.x + (whiteKeyWidth + noteSpacing) / 2;
                    // Horizontal offset of black key
                    if (key.noteInfo.noteLetter is NoteLetter.C or NoteLetter.F) posX -= sharpOffsetX;
                    if (key.noteInfo.noteLetter is NoteLetter.D or NoteLetter.A) posX += sharpOffsetX;

                    key.pos = new Vector2(posX, keyboardSize.y / 2 - blackKeySize.y / 2 + 0.01f);
                    key.size = blackKeySize;
                }
                else
                {
                    key.pos = whiteKeyPos;
                    key.size = whiteKeySize;
                    whiteKeyPos.x += whiteKeyWidth + noteSpacing;
                }
            }
        }

        public void SetKeyDown(int midiID, float velocity = 0.5f)
        {
            if (keyMap.TryGetValue(midiID, out PianoKey pianoKey))
            {
                pianoKey.isHeld = true;
            }

            OnKeyPressed?.Invoke(midiID, velocity);
        }

        public void SetKeyUp(int midiID)
        {
            if (keyMap.TryGetValue(midiID, out PianoKey pianoKey))
            {
                pianoKey.isHeld = false;
            }

            OnKeyReleased?.Invoke(midiID);
        }

        void HandleKeyboardInput()
        {
            for (int i = 0; i < keysQwerty.Length; i++)
            {
                if (Input.GetKeyDown(keysQwerty[i]))
                {
                    OnMidiNotePlayed(MidiHelper.GetNoteInfoFromMidiNumber(inputStartNote.MidiIndex + i), 1);
                }
                else if (Input.GetKeyUp(keysQwerty[i]))
                {
                    OnMidiNoteReleased(MidiHelper.GetNoteInfoFromMidiNumber(inputStartNote.MidiIndex + i));
                }
            }
        }

        void HandleMouseInput(Vector2 offset)
        {
            keyUnderMouse = null;

            foreach (PianoKey key in pianoKeys)
            {
                if (InputHelper.MouseInsideBounds_World(key.pos + offset, key.size))
                {
                    keyUnderMouse = key;
                }
            }

            // Press note (left mouse down)
            if (keyUnderMouse != null && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
            {
                keyHeldByMouse = keyUnderMouse;
                SetKeyDown(keyUnderMouse.midiID);
            }

            // Release note (left mouse up)
            if (keyHeldByMouse != null && InputHelper.IsMouseUpThisFrame(MouseButton.Left))
            {
                SetKeyUp(keyHeldByMouse.midiID);
                keyHeldByMouse = null;
            }

            // Print note debug info (middle mouse down)
            if (keyUnderMouse != null && InputHelper.IsMouseDownThisFrame(MouseButton.Middle))
            {
                string debugString = $"Note: {keyUnderMouse.noteInfo} Midi = {keyUnderMouse.noteInfo.MidiIndex} Freq = {NoteHelper.CalculateFrequency(keyUnderMouse.noteInfo)}";
                Debug.Log(debugString);
            }
        }

        public void DrawNotePlayed(int noteID)
        {
            foreach (PianoKey key in pianoKeys)
            {
                if (key.midiID == noteID)
                {
                    key.isHeld = true;
                    break;
                }
            }
        }

        public void DrawNoteReleased(int noteID)
        {
            foreach (PianoKey key in pianoKeys)
            {
                if (key.midiID == noteID)
                {
                    key.isHeld = false;
                    break;
                }
            }
        }


        void DrawPiano(Vector2 offset)
        {
            Vis.StartLayer(offset, 1, false);
            Vis.Quad(Vector2.zero, keyboardSize, colBg);

            foreach (PianoKey key in pianoKeys)
            {
                KeyCol keyCols = key.noteInfo.isSharp ? keyColsBlack : keyColsWhite;
                Color col = keyCols.normal;

                if (key == keyUnderMouse) col = keyCols.hover;
                if (key.isHeld) col = keyCols.hold;

                Vis.Quad(key.pos, key.size, col);

                if (!key.isHeld)
                {
                    Color c = key.noteInfo.isSharp ? colBlackTip : colWhiteTip;
                    Vector2 s = new Vector2(key.size.x * keyEndWidthFrac, key.size.y * keyEndBandHeight);
                    Vis.Quad(key.pos + Vector2.up * (s.y + -key.size.y) / 2, s, c);
                }
            }

            if (!string.IsNullOrEmpty(synthDisplayName))
            {
                Vis.Text(FontType.MapleMonoBold, synthDisplayName, 0.15f, new Vector2(-keyboardSize.x / 2, keyboardSize.y / 2 + 0.15f), Anchor.TextCentreLeft, Color.green);
            }

            Vis.QuadOutline(Vector2.zero, keyboardSize, outlineThickness, colOutline);
        }

        public void SetSynthDisplayName(string synthName) => synthDisplayName = synthName;

        public class PianoKey
        {
            public NoteInfo noteInfo;

            // Input info
            public bool isHeld;
            public int midiID;

            // Layout info
            public Vector2 pos;
            public Vector2 size;
            public PianoKey semitoneDown;
            public string name;
        }

        [Serializable]
        public struct NoteLabel
        {
            public bool show;
            public Color col;
            public Color colOutline;
            public Color colText;

            public Color highlightCol;
            public Color highlightColOutline;
            public Color highlightColText;

            public float fontSize;
            public Vector2 offsetWhite;
            public Vector2 offsetBlack;
            public Vector2 size;
            public float outlineThickness;
        }

        [Serializable]
        public struct KeyCol
        {
            public Color normal;
            public Color hover;
            public Color hold;
        }
    }
}