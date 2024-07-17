using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MIDI {
    public class MidiUtils : MonoBehaviour
    {
        public static float MidiToFrequency(int note) {
            return Mathf.Pow(2, (note - 69) / 12.0f) * 440;
        }
    }
}
