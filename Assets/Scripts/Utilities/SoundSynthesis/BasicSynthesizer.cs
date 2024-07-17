using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasicSynthesizer : MonoBehaviour
{

    public int bank = 0;
    public abstract AudioSource AudioSource { get; }

    public abstract void SetCC(int id, int midiValue);
    public abstract void SetValue(int id, float value);
    public abstract void Play(int midiNote, int velocity);
    public abstract void Stop(int midiNote);
    public abstract void FlushNotes();
}
