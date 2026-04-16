using Audio.Core;
using Audio.Helpers;
using Audio.IO;
using UnityEngine;

public class AudioRecorder : MonoBehaviour
{

    public string fileName = "Test";
    public bool record;
    public float info_recordDuration;
    AudioSystem audioSystem;
    
    void Start()
    {
        audioSystem = FindFirstObjectByType<AudioSystem>();    
    }
    
    void Update()
    {
        if (record && !audioSystem.IsRecording)
        {
            audioSystem.StartRecording();
        }

        if (!record && audioSystem.IsRecording)
        {
            Signal recording = audioSystem.StopRecording();
            AudioFileHelper.SaveToWav(recording, fileName, true);
            info_recordDuration = 0;
        }

        if (audioSystem.IsRecording)
        {
            info_recordDuration += Time.deltaTime;
        }
    }
}
