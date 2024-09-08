using System.Linq;
using UnityEngine;

public class WavSaveTest : MonoBehaviour
{
    public string fileName;
    const string inputFolder = "Input";
    const string outputFolder = "Output";

    void Start()
    {
        Signal source = Wav.ReadWav(Wav.GetWavPath(inputFolder, fileName));

        // Test speed change effects (reverse, slow down, speed up)
        Signal reversed = new(source.Samples.Reverse().ToArray(), source.SampleRate);
        Signal slow = new(source.Samples, source.SampleRate / 2);
        Signal fast = new(source.Samples, source.SampleRate * 2);

        Save(reversed, "Reversed");
        Save(slow, "Slow");
        Save(fast, "Fast");

        // Test fourier transform (reconstruct with various numbers of frequencies)
        FrequencyData[] spectrum = Fourier.DFT(source.Samples, source.SampleRate);
        spectrum = spectrum.OrderByDescending(s => s.Amplitude).ToArray();

        Signal reconstructA = SignalGenerator.GenerateSignal(spectrum[0..100], source.NumSamples, source.Duration);
        Signal reconstructB = SignalGenerator.GenerateSignal(spectrum[0..1000], source.NumSamples, source.Duration);
        Signal reconstructC = SignalGenerator.GenerateSignal(spectrum[0..5000], source.NumSamples, source.Duration);

        Save(reconstructA, "ReconstructA");
        Save(reconstructB, "ReconstructB");
        Save(reconstructC, "ReconstructC");
    }

    void Save(Signal signal, string name)
    {
        string path = Wav.GetWavPath(outputFolder, name);
        Wav.WriteWav(signal, path);
        Debug.Log("Saved: " + name);
    }

}
