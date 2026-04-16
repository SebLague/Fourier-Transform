using Audio.Display;
using Audio.MidiParser;
using Audio.Synth;
using UnityEngine;

namespace Audio
{
    public class MidiPlayer : MonoBehaviour
    {
        public TextAsset midiFile;
        public bool playing;
        public float bpm = 92;
        public bool bpmFromMidi;
        public int tickOffset;
        public bool overrideVelocityWilds;
        public bool overrideVelocityAllMax;
        public int transposeSemitoneCount;
        float currentPlaybackTime;
        MidiPlaybackState playbackState;

        public SynthBase synth;
        public PianoDisplay pianoDisplay;

        public int info_tick;
        public float info_quarterNoteDuration;
        public float info_ticksPerSecond;
        readonly System.Random rng = new(42);

        void Start()
        {
            playbackState = Load(midiFile.bytes);
            if (bpmFromMidi) bpm = playbackState.tempo;
        }

        void Update()
        {
            float crotchetDuration = 60f / bpm; // Quarter note
            float ticksPerSecond = playbackState.ticksPerQuarterNote / crotchetDuration;
            if (playbackState.tempo != 0) ticksPerSecond *= (float)playbackState.tempo / bpm;
            info_ticksPerSecond = ticksPerSecond;

            if (playing)
            {
                currentPlaybackTime += Time.deltaTime;
                playbackState.tick = (int)(currentPlaybackTime * ticksPerSecond + tickOffset);
                info_tick = playbackState.tick;

                foreach (MidiPlaybackTrack track in playbackState.tracks)
                {
                    ProcessPendingEvents(track, playbackState.tick);
                }
            }
        }

        void ProcessPendingEvents(MidiPlaybackTrack trackState, int tick)
        {
            for (int i = trackState.nextEventIndex; i < trackState.track.MidiEvents.Count; i++)
            {
                MidiEvent midiEvent = trackState.track.MidiEvents[i];
                if (midiEvent.Time <= tick)
                {
                    trackState.nextEventIndex++;
                    ProcessEvent(midiEvent);
                }
                else break;
            }
        }


        void ProcessEvent(MidiEvent midiEvent)
        {
            int noteID = midiEvent.Note + transposeSemitoneCount;
            
            if (midiEvent.MidiEventType == MidiEventType.NoteOn)
            {
                synth.NotifyNotePressed(noteID);
                // pianoDisplay.DrawNotePlayed(midiEvent.Note);
                
                float velocity = midiEvent.Velocity / 100f;
                if (overrideVelocityWilds)
                {
                    velocity = 0.4f;
                    if (noteID is 62 || (noteID + 12) is 62) velocity = 1f;
                    if (noteID is 64 || (noteID + 12) is 64) velocity = 0.9f;
                    if (noteID is 74 || (noteID + 12) is 74) velocity = 0.7f;
                    velocity += ((float)rng.NextDouble() - 0.5f) * 0.05f;
                    velocity = Mathf.Clamp01(velocity);
                }

                if (overrideVelocityAllMax) velocity = 1;
                pianoDisplay.SetKeyDown(noteID, velocity);
            }
            else if (midiEvent.MidiEventType == MidiEventType.NoteOff)
            {
                synth.NotifyNoteReleased(noteID);
                //pianoDisplay.DrawNoteReleased(midiEvent.Note);
                pianoDisplay.SetKeyUp(noteID);
            }
            else if (midiEvent.MidiEventType == MidiEventType.ControlChange)
            {
                //Debug.Log(midiEvent.ControlChangeType);
            }
        }

        static MidiPlaybackState Load(byte[] midiData)
        {
            MidiFile midi = new(midiData);
            int tempo = 0;


            MidiPlaybackTrack[] playbackTracks = new MidiPlaybackTrack[midi.Tracks.Length];

            for (int trackIndex = 0; trackIndex < midi.Tracks.Length; trackIndex++)
            {
                MidiTrack track = midi.Tracks[trackIndex];
                playbackTracks[trackIndex] = new MidiPlaybackTrack();
                playbackTracks[trackIndex].track = track;

                // Get track name (if exists)
                foreach (TextEvent textEvent in track.TextEvents)
                {
                    if (textEvent.TextEventType == TextEventType.TrackName)
                    {
                        playbackTracks[trackIndex].name = textEvent.Value;
                        break;
                    }
                }

                foreach (MidiEvent e in playbackTracks[trackIndex].track.MidiEvents)
                {
                    if (e.MidiEventType is MidiEventType.MetaEvent)
                    {
                        if (e.MetaEventType is MetaEventType.Tempo)
                        {
                            tempo = e.Arg2;
                        }
                    }
                }
            }


            return new MidiPlaybackState()
            {
                ticksPerQuarterNote = midi.TicksPerQuarterNote,
                tracks = playbackTracks,
                tempo = tempo
            };
        }


        public class MidiPlaybackState
        {
            public MidiPlaybackTrack[] tracks;
            public int tick;
            public int ticksPerQuarterNote;
            public int tempo;
        }

        public class MidiPlaybackTrack
        {
            public string name = "Untitled Track";
            public MidiTrack track;
            public int nextEventIndex;
        }
    }
}