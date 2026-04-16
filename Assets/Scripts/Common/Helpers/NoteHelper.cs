using UnityEngine;

namespace Audio.Helpers
{
    public static class NoteHelper
    {
        const int NumNoteLetters = 7;

        // Get note index (starting at C0 = 0), and ignoring black keys
        // So D0 = 1, E0 = 2, F0 = 3, etc.
        public static int GetNaturalNoteIndex(NoteInfo note)
        {
            int noteIndex = (int)note.noteLetter;
            return noteIndex + note.octaveNumber * NumNoteLetters;
        }

        public static NoteInfo GetNoteFromNaturalIndex(int naturalIndex)
        {
            int letterIndex = naturalIndex % NumNoteLetters;
            int octaveNumber = naturalIndex / NumNoteLetters;

            return new NoteInfo((NoteLetter)letterIndex, octaveNumber, false);
        }

        public static NoteInfo SemitoneUp(NoteInfo note)
        {
            int noteIndex = MidiHelper.GetMidiNoteIndex(note);
            return MidiHelper.GetNoteInfoFromMidiNumber(noteIndex + 1);
        }

        public static NoteInfo GetNextNaturalNote(NoteInfo note)
        {
            NoteLetter nextNoteLetter = GetNextNoteLetter(note.noteLetter);
            int nextOctaveNumber = note.octaveNumber;

            if (nextNoteLetter == NoteLetter.C)
            {
                nextOctaveNumber++;
            }

            return new NoteInfo(nextNoteLetter, nextOctaveNumber, false);
        }

        public static bool IsSameNote(NoteInfo a, NoteInfo b)
        {
            return a.noteLetter == b.noteLetter && a.octaveNumber == b.octaveNumber && a.isSharp == b.isSharp;
        }

        public static NoteLetter GetNextNoteLetter(NoteLetter noteLetter)
        {
            int letterIndex = (int)noteLetter;
            letterIndex++;
            letterIndex %= NumNoteLetters;
            return (NoteLetter)letterIndex;
        }
        
        public static float CalculateFrequencyFromMidiIndex(int midiIndex)
        {
            int semitonesAboveA0 = midiIndex - MidiHelper.NoteIndexA0;
            return CalculateFrequency(semitonesAboveA0);
        }

        public static float CalculateFrequency(NoteInfo note)
        {
            int midiIndex = MidiHelper.GetMidiNoteIndex(note);
            int semitonesAboveA0 = midiIndex - MidiHelper.NoteIndexA0;
            return CalculateFrequency(semitonesAboveA0);
        }

        const double FrequencyBase = 1.059463094359;

        public static float CalculateFrequency(int semitonesAboveA0)
        {
            const double A0Frequency = 27.5;
            return (float)(A0Frequency * System.Math.Pow(FrequencyBase, semitonesAboveA0));
        }

        public static float NextFrequency(float frequency)
        {
            return (float)(frequency * FrequencyBase);
        }
    }
}