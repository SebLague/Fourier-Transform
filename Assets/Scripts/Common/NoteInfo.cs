using Audio.Helpers;

namespace Audio
{
    public enum NoteLetter
    {
        C,
        D,
        E,
        F,
        G,
        A,
        B
    }

    [System.Serializable]
    public struct NoteInfo
    {
        public NoteLetter noteLetter;
        public bool isSharp;
        public int octaveNumber;

        public NoteInfo(NoteLetter noteLetter, int octaveNumber, bool isSharp)
        {
            this.noteLetter = noteLetter;
            this.isSharp = isSharp;
            this.octaveNumber = octaveNumber;
        }

        public override string ToString()
        {
            return noteLetter + (isSharp ? "#" : string.Empty) + octaveNumber;
        }

        public string NameShort => noteLetter + (isSharp ? "#" : string.Empty);

        public int MidiIndex => MidiHelper.GetMidiNoteIndex(this);
    }
}