using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Audio.Helpers;
using UnityEngine;

namespace Audio.Synth
{
    public abstract class SynthBase : MonoBehaviour
    {
        // List of notes currently being played -- available to the audio thread
        protected readonly List<Note> activeNotes = new();
        // Notes that should be played -- waiting to be moved over to the audio thread
        protected readonly ConcurrentQueue<Note> notesToAddToActiveList = new();
        // Note lookup by its midi index
        protected readonly Dictionary<int, Note> allNotes = new();

        protected int NyquistThreshold;
        public const float TAU = MathF.PI * 2f;

        public abstract string SynthName { get; }


        public void NotifyNotePressed(int midiID, float velocity = 0.5f)
        {
            double time = AudioSystem.GlobalTime;

            if (!allNotes.ContainsKey(midiID))
            {
                Note newNote = CreateNote(midiID);
                allNotes.Add(midiID, newNote);
            }

            Note note = allNotes[midiID];
            UpdateNoteSettings(note);

            if (!note.IsActive)
            {
                notesToAddToActiveList.Enqueue(note);
            }

            note.Press(velocity);
        }

        public void NotifyNoteReleased(int midiID)
        {
            double time = AudioSystem.GlobalTime;

            if (allNotes.TryGetValue(midiID, out Note note))
            {
                note.Release();
            }
            else
            {
                throw new Exception("Tried to release note that has not been pressed?!");
            }
        }

        public void SampleFill(float startTime, float timeStep, float[] data)
        {
            NyquistThreshold = AudioSystem.SampleRate / 2;
            double endTime = startTime + timeStep * data.Length;

            UpdateActiveNotes();
            ActiveNotesFill(startTime, timeStep, data);
            FlagFinishedNotes((float)endTime);
        }


        void UpdateActiveNotes()
        {
            // Retire any notes flagged as finished
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                if (!activeNotes[i].IsActive)
                {
                    activeNotes.RemoveAt(i);
                }
            }

            // Load any new active notes from main thread into audio thread list
            while (notesToAddToActiveList.Count > 0)
            {
                if (notesToAddToActiveList.TryDequeue(out Note note)) activeNotes.Add(note);
            }
        }

        /*
         *  readonly ConcurrentQueue<Note> pendingNotes = new();
           readonly List<Note> activeNotes = new(128);

           // Run on audio thread (just prior to synthesizer block)
           void UpdateActiveNotes()
           {
               // Load pending notes into active list
               while (pendingNotes.Count > 0)
               {
                   if (pendingNotes.TryDequeue(out Note note)) activeNotes.Add(note);
               }

               // Remove finished notes from active list
               for (int i = activeNotes.Count - 1; i >= 0; i--)
               {
                   if (NoteIsFinished(activeNotes[i])) activeNotes.RemoveAt(i);
               }
           }

           bool NoteIsFinished(Note note)
           {
               if (note.IsPressed) return note.PressedTimer > envelope.attack + envelope.decay;
               return note.ReleasedTimer > envelope.release;
           }
         */

        protected abstract Note CreateNote(int midiIndex);

        // Flag notes in active notes list if finished
        protected abstract void FlagFinishedNotes(float time);

        protected abstract void ActiveNotesFill(float startTime, float timeStep, float[] data);

        protected virtual void UpdateNoteSettings(Note note)
        {
        }
    }
}