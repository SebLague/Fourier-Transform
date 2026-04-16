using System.IO;
using Audio.Core;
using Audio.IO;
using Seb.Helpers;
using UnityEngine;

namespace Audio.Helpers
{
    public static class AudioFileHelper
    {
        public static Signal SignalFromAudioClip(AudioClip sourceClip)
        {
            float[] data = new float[sourceClip.samples];
            Debug.Assert(sourceClip.channels == 1, "Expected mono audioclip");
            sourceClip.GetData(data, 0);
            Signal signal = new(data, sourceClip.frequency, sourceClip.name);

            return signal;
        }

        public static string GetExternalWavSavePath(string name)
        {
            string fileName = $"{name}.wav";
            string directoryPath = Path.Combine(FileHelper.ProjectDirectory, "AudioOutput");
            string path = Path.Combine(directoryPath, fileName);
            Directory.CreateDirectory(directoryPath);

            return path;
        }


        public static string GetInternalWavSavePath(string directory, string fileName)
        {
            string BaseSavePath = Path.Combine(Application.dataPath, "Wav");
            return Path.Combine(BaseSavePath, directory, $"{Path.GetFileNameWithoutExtension(fileName)}.wav");
        }

        public static void SaveToWav(Signal signal, string name, bool logPath = false)
        {
            string path = GetExternalWavSavePath(name);
            Wav.WriteWav(signal, path);

            if (logPath) Debug.Log($"Wav saved to {path} (duration: {signal.Duration})");
        }
    }
}