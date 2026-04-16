using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Minis;
using UnityEngine;
using UnityEngine.InputSystem;
using static Audio.Helpers.MidiHelper;

public class MidiListener : MonoBehaviour
{
    public event NotePressedDelegate OnNotePressed;
    public event NoteReleasedDelegate OnNoteReleased;

    public delegate void NotePressedDelegate(NoteInfo note, float velocity);
    public delegate void NoteReleasedDelegate(NoteInfo note);

    readonly List<MidiDevice> connectedMidiDevices = new();
    public List<string> info_devices = new();
    static MidiListener _instance;


    public static void Subscribe(NotePressedDelegate pressed, NoteReleasedDelegate released)
    {
        Instance.OnNotePressed -= pressed;
        Instance.OnNoteReleased -= released;
        
        Instance.OnNotePressed += pressed;
        Instance.OnNoteReleased += released;
    }

    public static void Unsubscribe(NotePressedDelegate pressed, NoteReleasedDelegate released)
    {
        Instance.OnNotePressed -= pressed;
        Instance.OnNoteReleased -= released;
    }

    void Start()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        RegisterMidiDevices();
    }


    void MidiNoteOn(MidiNoteControl note, float velocity)
    {
        OnNotePressed?.Invoke(GetNoteInfoFromMidiNumber(note.noteNumber), velocity);
    }

    void MidiNoteOff(MidiNoteControl note)
    {
        OnNoteReleased?.Invoke(GetNoteInfoFromMidiNumber(note.noteNumber));
    }


    void RegisterMidiDevices()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is not MidiDevice midiDevice) continue;

            midiDevice.onWillNoteOn += MidiNoteOn;
            midiDevice.onWillNoteOff += MidiNoteOff;

            connectedMidiDevices.Add(midiDevice);
            info_devices.Add(midiDevice.displayName);
        }
    }


    void DisconnectAllDevices()
    {
        foreach (MidiDevice midiDevice in connectedMidiDevices)
        {
            midiDevice.onWillNoteOn -= MidiNoteOn;
            midiDevice.onWillNoteOff -= MidiNoteOff;
        }

        connectedMidiDevices.Clear();
        info_devices.Clear();
    }


    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not MidiDevice) return;

        DisconnectAllDevices();
        RegisterMidiDevices();
    }

    void OnDestroy()
    {
        DisconnectAllDevices();
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    public static MidiListener Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindFirstObjectByType<MidiListener>();
                if (!_instance) _instance = new GameObject("MidiListener").AddComponent<MidiListener>();
            }

            return _instance;
        }
    }
}