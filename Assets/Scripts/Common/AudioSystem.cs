using System;
using System.Collections.Generic;
using Audio.Core;
using UnityEngine;
using static System.Math;

[RequireComponent(typeof(AudioSource))]
public class AudioSystem : MonoBehaviour
{
    public float amplitudeMultiplier = 0.2f;

    [Header("Info")]
    public int sampleRate;
    public int info_numChannels;
    public int bufferLength;
    public int numBuffers;
    public int batchesPerSecond;
    public long numTicks;
    public float debugInfo_audioTime;

    static AudioSystem instance;
    AudioDataFiller synthesizer;
    float[] singleChannelData;
    float TimeStep;
    bool isClipping;
    bool invalidData; // nans in signal

    bool isRecording;
    readonly List<float> recordedSamples = new(50000 * 10);

    public delegate void AudioDataFiller(float time, float timeStep, float[] data);

    void Awake()
    {
        instance = this;
        sampleRate = AudioSettings.outputSampleRate;
        TimeStep = 1.0f / sampleRate;

        AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
        singleChannelData = new float[bufferLength];
        batchesPerSecond = sampleRate / bufferLength;
        info_numChannels = (AudioSettings.speakerMode == AudioSpeakerMode.Stereo) ? 2 : 1;
        numTicks = 0;

        if (info_numChannels != 1)
        {
            Debug.LogError("Only single channel audio output currently supported");
        }
    }


    void Update()
    {
        if (isClipping)
        {
            isClipping = false;
            Debug.Log("Warning: audio clipping");
        }

        if (invalidData)
        {
            invalidData = false;
            Debug.Log("Warning: nans in audio!");
        }

        debugInfo_audioTime = (float)Time;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Set gain to zero");
            amplitudeMultiplier = 0;
        }
    }



    void FillAudioData(float[] audioData)
    {
        // Fill data from active source
        try
        {
            synthesizer.Invoke(Time, TimeStep, audioData);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message + e.StackTrace);
            Array.Clear(audioData, 0, audioData.Length);
            return;
        }

        // Keep our eardrums intact:
        const float MaxAmplitude = 0.95f;

        for (int i = 0; i < audioData.Length; i++)
        {
            float value = audioData[i] * amplitudeMultiplier;

            // Test if value is outside [-Max, +Max]
            if (Abs(value) > MaxAmplitude) isClipping = true;
            // Clamp value 
            if (isClipping) value = MaxAmplitude * Sign(value);

            // Alert if any values are invalid (and set remaining values to 0)
            if (float.IsNaN(value)) invalidData = true;
            if (invalidData) value = 0;

            audioData[i] = value;
        }
    }


    void OnAudioFilterRead(float[] data, int numChannels)
    {
        FillAudioData(data);
        if (isRecording) recordedSamples.AddRange(data);
        numTicks += data.Length;
    }
    

    public void SetAudioSource(AudioDataFiller sampler)
    {
        synthesizer = sampler;
    }

    public void StartRecording()
    {
        recordedSamples.Clear();
        isRecording = true;
    }

    public Signal StopRecording()
    {
        isRecording = false;
        return new Signal(recordedSamples.ToArray(), sampleRate);
    }

    public bool IsRecording => isRecording;

    public float Time => numTicks / (float)sampleRate;
    public static double GlobalTime => instance.Time;

    public static int SampleRate => instance.sampleRate;
    public static float Timestep => instance.TimeStep;
}