using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CsoundBasicSynthesizer : BasicSynthesizer {

    public enum OscillatorType {
        Sine = 0,
        Square = 1,
        Saw = 2,
        Triangle = 3
    }

    public CsoundUnity csoundAudioSource;
    public OscillatorType oscillatorType = OscillatorType.Sine;
    public bool flushNotes = false;
    public bool printCurrentSettings = false;

    [Header("CC Mapping")]
    public MIDI.MidiCC attack = new MIDI.MidiCC() { id = 74, min = 0.01f, max = 1.0f, exp = 1.0f };
    public MIDI.MidiCC decay = new MIDI.MidiCC() { id = 71, min = 0.01f, max = 1.0f, exp = 1.0f };
    public MIDI.MidiCC sustain = new MIDI.MidiCC() { id = 72, min = 0.7f, max = 1.0f, exp = 1.0f };
    public MIDI.MidiCC release = new MIDI.MidiCC() { id = 73, min = 0.2f, max = 1.0f, exp = 1.0f };
    public MIDI.MidiCC gain = new MIDI.MidiCC() { id = 16, min = 0.0f, max = 1.0f, exp = 1.00f };
    public MIDI.MidiCC lpfCutOff = new MIDI.MidiCC() { id = 93, min = 20.0f, max = 20000.0f, exp = 1.06f };
    public MIDI.MidiCC lpgQ = new MIDI.MidiCC() { id = 18, min = 1.0f, max = 8.0f, exp = 1.0f };
    public MIDI.MidiCC reverbRoom = new MIDI.MidiCC() { id = 19, min = -10000f, max = 0f, exp = 1.0f };

    [Header("Other Mappings")]
    public int presetId = 0;
    public int oscillatorTypeId = 1;
    public int frequencyId = 2;

    public override AudioSource AudioSource => _baseAudioSource;

    private List<int> _hangingNotesControl = new List<int>();

    private bool _csoundReady = false;
    private AudioSource _baseAudioSource;
    private AudioLowPassFilter _lowPassFilter;
    private AudioReverbFilter _reverbFilter;

    private void Awake() {

        //Initialize envelope default values
        attack.SetMidiValueFromScaledValue(0.01f);
        decay.SetMidiValueFromScaledValue(0.01f);
        sustain.SetMidiValueFromScaledValue(0.7f);
        release.SetMidiValueFromScaledValue(0.2f);

        //Default gain
        var defaultGain = 0.5f;
        gain.SetMidiValueFromScaledValue(defaultGain);
        _baseAudioSource = transform.parent.GetComponent<AudioSource>();
        _baseAudioSource.volume = defaultGain;

        //Default LPF
        _lowPassFilter = transform.parent.GetComponent<AudioLowPassFilter>();
        var defaultLPFCutOff = 20000.0f;
        lpfCutOff.SetMidiValueFromScaledValue(defaultLPFCutOff);
        _lowPassFilter.cutoffFrequency = defaultLPFCutOff;
        var dafultQ = 1.0f;
        lpgQ.SetMidiValueFromScaledValue(dafultQ);
        _lowPassFilter.lowpassResonanceQ = dafultQ;

        //Default Reverb
        _reverbFilter = transform.parent.GetComponent<AudioReverbFilter>();
        var defaultReverbRoom = -10000f;
        reverbRoom.SetMidiValueFromScaledValue(defaultReverbRoom);
        _reverbFilter.room = defaultReverbRoom;
        
    }

    public override void FlushNotes(){
        while (_hangingNotesControl.Count > 0) {
            Stop(_hangingNotesControl[0]);
        }
    }

    private void Update() {
        if (!_csoundReady && csoundAudioSource.IsInitialized) {
            csoundAudioSource.SetChannel("gain", 0.1f);
            _csoundReady = true;
        }
        if (flushNotes) {
            FlushNotes();
            flushNotes = false;
        }
        if (printCurrentSettings) {
            printCurrentSettings = false;
            Debug.Log("Attack: " + attack.GetScaledValue());
            Debug.Log("Decay: " + decay.GetScaledValue());
            Debug.Log("Sustain: " + sustain.GetScaledValue());
            Debug.Log("Release: " + release.GetScaledValue());
            Debug.Log("Gain: " + gain.GetScaledValue());
        }   
    }

    public override void Play(int midiNote, int velocity) {
        if (_hangingNotesControl.Contains(midiNote)) {
            Stop(midiNote);
        }
        //TODO: Implement velocity
        var freq = MIDI.MidiUtils.MidiToFrequency(midiNote);
        var csoundNote = midiNote + 127 * bank;
        SetFrequency(freq);
        csoundAudioSource.SendScoreEvent("i 1." + csoundNote + " 0 -1 " + 
            (int)(oscillatorType + 1) + " " +
            freq + " " +
            attack.GetScaledValue() + " " + 
            decay.GetScaledValue() + " " +
            sustain.GetScaledValue() + " " +
            release.GetScaledValue());
        _hangingNotesControl.Add(midiNote);
    }

    public override void Stop(int midiNote) {
        var csoundNote = midiNote + 127 * bank;
        csoundAudioSource.SendScoreEvent("i -1." + csoundNote + " 0 0");
        _hangingNotesControl.Remove(midiNote);
    }


    public override void SetCC(int id, int midiValue) {
        if (id == presetId) {
            SetPreset(midiValue);
        } else if (id == oscillatorTypeId) {
            SetOscillatorType((OscillatorType)midiValue);
        }else if (id == attack.id) {
            attack.value = midiValue;
        } else if (id == decay.id)
            decay.value = midiValue;
        else if (id == sustain.id)
            sustain.value = midiValue;
        else if (id == release.id)
            release.value = midiValue;
        else if (id == gain.id) {
            gain.value = midiValue;
            _baseAudioSource.volume =  gain.GetScaledValue();
        }else if (id == lpfCutOff.id) {
            lpfCutOff.value = midiValue;
            _lowPassFilter.cutoffFrequency = lpfCutOff.GetScaledValue();
        } else if (id == lpgQ.id) {
            lpgQ.value = midiValue;
            _lowPassFilter.lowpassResonanceQ = lpgQ.GetScaledValue();
        } else if (id == reverbRoom.id) {
            reverbRoom.value = midiValue;
            _reverbFilter.room = reverbRoom.GetScaledValue();
        }
    }

    public override void SetValue(int id, float value) {
        if (id == presetId) {
            SetPreset(Mathf.RoundToInt(value));
        } else if (id == oscillatorTypeId) {
            SetOscillatorType((OscillatorType)Mathf.RoundToInt(value));
        } else if (id == attack.id) {
            attack.SetMidiValueFromScaledValue(value);
        } else if (id == decay.id)
            decay.SetMidiValueFromScaledValue(value);
        else if (id == sustain.id)
            sustain.SetMidiValueFromScaledValue(value);
        else if (id == release.id)
            release.SetMidiValueFromScaledValue(value);
        else if (id == gain.id) {
            gain.SetMidiValueFromScaledValue(value);
            _baseAudioSource.volume = gain.GetScaledValue();
        } else if (id == lpfCutOff.id) {
            lpfCutOff.SetMidiValueFromScaledValue(value);
            _lowPassFilter.cutoffFrequency = lpfCutOff.GetScaledValue();
        } else if (id == lpgQ.id) {
            lpgQ.SetMidiValueFromScaledValue(value);
            _lowPassFilter.lowpassResonanceQ = lpgQ.GetScaledValue();
        } else if (id == reverbRoom.id) {
            reverbRoom.SetMidiValueFromScaledValue(value);
            _reverbFilter.room = reverbRoom.GetScaledValue();
        } else if (id == frequencyId) {
            SetFrequency(value);
        }
    }

    public void SetCCPreset(
        float attack_value, 
        float decay_value,
        float sustain_value,
        float release_value,
        float lpfcutOff_value,
        float lpgQ_value,
        float reverbRoom_value,
        float gain_value
        ) {
        attack.SetMidiValueFromScaledValue(attack_value);
        decay.SetMidiValueFromScaledValue(decay_value);
        sustain.SetMidiValueFromScaledValue(sustain_value);
        release.SetMidiValueFromScaledValue(release_value);

        lpfCutOff.SetMidiValueFromScaledValue(lpfcutOff_value);
        lpgQ.SetMidiValueFromScaledValue(lpgQ_value);
        reverbRoom.SetMidiValueFromScaledValue(reverbRoom_value);

        gain.SetMidiValueFromScaledValue(gain_value);

        _baseAudioSource.volume = gain.GetScaledValue();
        _lowPassFilter.cutoffFrequency = lpfCutOff.GetScaledValue();
        _lowPassFilter.lowpassResonanceQ = lpgQ.GetScaledValue();
        _reverbFilter.room = reverbRoom.GetScaledValue();
    }

    public void SetPreset(int presetCode) {
        switch (presetCode) {
            case 1:
            SetCCPreset(0.01f, 0.7f, 0.8f, 0.53f, 5374.0f, 1.0f, -1800f, 0.5f);
            break;
            case 2:
            SetCCPreset(0.46f, 0.7f, 0.8f, 0.35f, 400.0f, 5.0f, -1873f, 0.5f);
            break;
            case 3:
            SetCCPreset(0.36f,0.39f, 0.92f, 0.42f, 20000.0f, 1.0f, -1985f, 0.5f);
            break;
            case 4:
            SetCCPreset(0.01f, 1f, 0.77f, 0.68f, 6713.0f, 5.0f, -3000f, 0.5f);
            break;
        }
    }

    public void SetOscillatorType(OscillatorType type) {
        //Keep in the range
        if ((int)type < 0 || (int)type > 3)
            oscillatorType = 0;
         else
            oscillatorType = type;
    }

    public void SetFrequency(float frequency) {
        csoundAudioSource.SetChannel("freq", frequency);
    }
}
