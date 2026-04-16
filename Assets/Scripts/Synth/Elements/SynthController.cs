using Audio.Display;
using UnityEngine;

namespace Audio.Synth.Controller
{
    public class SynthController : MonoBehaviour
    {
        public AudioSystem audioSystem;
        public SynthBase synth;
        public PianoDisplay piano;
        
        
        void Start()
        {
            audioSystem.SetAudioSource(synth.SampleFill);
            piano.OnKeyPressed += OnNotePressed;
            piano.OnKeyReleased += OnNoteReleased;
        }
        

        void OnNotePressed(int midiId, float velocity)
        {
            synth.NotifyNotePressed(midiId, velocity);
        }

        void OnNoteReleased(int midiId)
        {
            synth.NotifyNoteReleased(midiId);
        }
    }
}