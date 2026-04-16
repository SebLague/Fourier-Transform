using System;

namespace Audio.Helpers
{
    public static class MidiHelper
    {
        public const int NoteIndexC2 = 36;
        public const int NoteIndexC4 = 60; // Middle C
        public const int NoteIndexC6 = 84;
        public const int NoteIndexA0 = 21;
        const int NotesInOctave = 12; // [C...B] including black keys

        public static int GetMidiNoteIndex(NoteInfo noteInfo) => GetMidiNoteIndex(noteInfo.noteLetter, noteInfo.octaveNumber, noteInfo.isSharp);

        public static int GetMidiNoteIndex(NoteLetter noteLetter, int octaveNumber, bool isSharp)
        {
            int naturalIndex = (int)noteLetter; // [C, B] => [0, 6]

            int index = naturalIndex switch
            {
                0 => 0, // C
                1 => 2, // D
                2 => 4, // E
                3 => 5, // F
                4 => 7, // G
                5 => 9, // A
                6 => 11, // B
                _ => throw new System.Exception("Unexpected note index")
            };

            if (isSharp) index++;

            index += NotesInOctave * (octaveNumber + 1);
            return index;
        }
        
        public static NoteInfo GetNoteInfoFromMidiNumber(int midiNoteNumber)
        {
            // Calculate octave number (octaves start on C to make life confusing)
            // For instance, middle C (midi note = 60) is the start of octave 4
            int octaveNumber = (midiNoteNumber - NotesInOctave) / NotesInOctave;
            // Test if note is an accidental (sharp)
            int semitonesAboveC = midiNoteNumber % NotesInOctave;
            bool isSharp = semitonesAboveC is 1 or 3 or 6 or 8 or 10;

            NoteLetter noteLetter = semitonesAboveC switch
            {
                0 or 1 => NoteLetter.C,
                2 or 3 => NoteLetter.D,
                4 => NoteLetter.E,
                5 or 6 => NoteLetter.F,
                7 or 8 => NoteLetter.G,
                9 or 10 => NoteLetter.A,
                11 => NoteLetter.B,
                _ => throw new Exception($"Unexpected note number {midiNoteNumber}")
            };
            
            return new NoteInfo(noteLetter, octaveNumber, isSharp);
        }
    }
}