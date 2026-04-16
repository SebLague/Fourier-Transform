using Audio.Display;
using Audio.Helpers;
using Audio.Tools;
using Seb.Helpers;
using UnityEngine;
using static UnityEngine.Mathf;

namespace Audio.Synth
{
    public class Synth : SynthBase
    {
        [Header("Settings")]

        public SynthKeyFrame[] synthKeyFrames;
        public CompressorTest compressor;
        public EnvelopeCreatorADR envelopeCreator;
        public DiscreteCurveWidgetBase decayRateCurve;
        public bool autoReleaseNotes;

        [Header("Info")]
        public int info_numActiveNotes;

        const float MaxAmplitude = 0.3f;
        LinearEnvelopeADR envelope;

        void Start()
        {
            UpdateFromGUI();
        }

        void Update()
        {
            info_numActiveNotes = activeNotes.Count;
            UpdateFromGUI();
        }

        void Synthesize(float timeStep, float[] audioData)
        {
            for (int i = 0; i < audioData.Length; i++)
            {
                float value = 0;
                foreach (Note note in activeNotes)
                {
                    if (note.IsPressed)
                    {
                        note.PressedTimer += timeStep;
                        if (autoReleaseNotes && note.PressedTimer > envelope.Attack) note.Release();
                    }
                    else note.ReleasedTimer += timeStep;


                    for (int harmonicIndex = 0; harmonicIndex < note.HarmonicAmplitudes.Length; harmonicIndex++)
                    {
                        // Calculate frequency and amplitude of current harmonic
                        int harmonicFrequency = note.Frequency * (harmonicIndex + 1);
                        float decayFactor = note.DecayDurationFactors[harmonicIndex];
                        float harmonicAmplitude = note.HarmonicAmplitudes[harmonicIndex];
                        harmonicAmplitude *= envelope.Evaluate(note, decayFactor) * MaxAmplitude * note.Velocity;

                        // Increment phase and wrap within [0, 1] for precision
                        float phase = note.Phases[harmonicIndex] + timeStep * harmonicFrequency;
                        if (phase > 1) phase -= 1;
                        note.Phases[harmonicIndex] = phase;

                        value += Sin(phase * TAU) * harmonicAmplitude;
                    }
                }


                audioData[i] = value;
            }

            compressor.UpdateCompressor(audioData, timeStep);
        }

        protected override void UpdateNoteSettings(Note note)
        {
            for (int i = 0; i < note.DecayDurationFactors.Length; i++)
            {
                float freq = note.Frequency * (i + 1);

                if (decayRateCurve == null)
                {
                    note.DecayDurationFactors[i] = 1;
                }
                else
                {
                    float freqT_decay = Mathf.InverseLerp(decayRateCurve.GetXAxisMinMax().x, decayRateCurve.GetXAxisMinMax().y, freq);
                    float decayRateT = decayRateCurve.GetDiscreteCurve().Evaluate(freqT_decay);
                    note.DecayDurationFactors[i] = Mathf.Lerp(decayRateCurve.GetYAxisMinMax().x, decayRateCurve.GetYAxisMinMax().y, decayRateT);
                }
            }

            InterpolateNoteProperties(note);
        }


        void InterpolateNoteProperties(Note note)
        {
            SynthKeyFrame low = synthKeyFrames[0];
            SynthKeyFrame high = synthKeyFrames[^1];

            // Find which key frames the current note lies between
            foreach (SynthKeyFrame frame in synthKeyFrames)
            {
                if (frame.MidiIndex <= note.MidiIndex && frame.MidiIndex >= low.MidiIndex) low = frame;
                if (frame.MidiIndex >= note.MidiIndex && frame.MidiIndex <= high.MidiIndex) high = frame;
            }

            low.Init();
            high.Init();

            float t = InverseLerp(low.note.MidiIndex, high.note.MidiIndex, note.MidiIndex);
            t = Maths.EaseQuadInOut(t);

            for (int i = 0; i < note.HarmonicAmplitudes.Length; i++)
            {
                note.HarmonicAmplitudes[i] = Lerp(low.harmonicAmplitudes[i], high.harmonicAmplitudes[i], t);

                float decayFac = Lerp(low.decayDurationFactor, high.decayDurationFactor, t);
                note.DecayDurationFactors[i] /= decayFac;
            }
        }

        void UpdateFromGUI()
        {
            envelope = envelopeCreator.CreateEnvelope();
        }


        protected override void ActiveNotesFill(float startTime, float timeStep, float[] data)
        {
            Synthesize(timeStep, data);
        }

        public override string SynthName => "Synth";

        protected override Note CreateNote(int midiIndex)
        {
            Note note = new Note();
            note.Frequency = Mathf.RoundToInt(NoteHelper.CalculateFrequencyFromMidiIndex(midiIndex));
            note.MidiIndex = midiIndex;

            int overtoneCount = synthKeyFrames[0].overtoneCreator.overtoneCount;
            note.HarmonicAmplitudes = new float[overtoneCount];
            note.DecayDurationFactors = new float[overtoneCount];
            note.Phases = new float[overtoneCount];
            return note;
        }

        protected override void FlagFinishedNotes(float time)
        {
            foreach (Note note in activeNotes)
            {
                if (!note.IsPressed && note.ReleasedTimer > envelope.Release)
                {
                    note.IsActive = false;
                }
            }
        }

        void OnValidate()
        {
            if (synthKeyFrames != null)
            {
                foreach (SynthKeyFrame frame in synthKeyFrames)
                {
                    frame.name = frame.note.ToString();
                }
            }
        }

        [System.Serializable]
        public class SynthKeyFrame
        {
            public string name;
            public NoteInfo note;
            public OvertoneCreator overtoneCreator;
            public float decayDurationFactor = 1;

            [HideInInspector] public float[] harmonicAmplitudes;
            [HideInInspector] public float[] harmonicAmplitudes_Display;

            public int MidiIndex => note.MidiIndex;

            public void Init()
            {
                ArrayHelper.ResizeAndCopy(ref harmonicAmplitudes, overtoneCreator.valuesScaled);
                ArrayHelper.ResizeAndCopy(ref harmonicAmplitudes_Display, overtoneCreator.values);
            }
        }
    }
}