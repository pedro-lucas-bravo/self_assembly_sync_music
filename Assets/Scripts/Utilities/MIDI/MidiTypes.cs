using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MIDI{

    [System.Serializable]
    public struct MidiCC
    {
        public int id;        
        public float min;
        public float max;
        public float exp;
        public int value { get; set; }
        public float GetScaledValue() {
            var x = value;
            var in_low = 0f;
            var in_high = 127f;
            var out_low = min;
            var out_high = max;
            //Using Max Msp scale function
            return ((x - in_low) / (in_high - in_low) == 0) ? 
                out_low : 
                (((x - in_low) / (in_high - in_low)) > 0) ? 
                    (out_low + (out_high - out_low) * Mathf.Pow((x - in_low) / (in_high - in_low), exp)) : 
                    (out_low + (out_high - out_low) * -(Mathf.Pow(((-x + in_low) / (in_high - in_low)), exp)));        
        }

        public void SetMidiValueFromScaledValue(float scaledValue) {
            var x = scaledValue;
            var in_low = 0f;
            var in_high = 127f;
            var out_low = min;
            var out_high = max;
            //Using Max Msp scale function
            var midiVal =  ((x - out_low) / (out_high - out_low) == 0) ? 
                in_low : 
                (((x - out_low) / (out_high - out_low)) > 0) ? 
                    (in_low + (in_high - in_low) * Mathf.Pow((x - out_low) / (out_high - out_low), 1/exp)) : 
                    (in_low + (in_high - in_low) * -(Mathf.Pow(((-x + out_low) / (out_high - out_low)), 1/exp)));    
            value = (int)midiVal;
        }
    }
}
