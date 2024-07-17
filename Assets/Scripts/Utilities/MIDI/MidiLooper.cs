using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MIDI {

    public class MidiEvent {
        public int MidiNumber { get; private set; }
        public int Velocity { get; private set; }
        public double Timestamp { get; private set; } // Record the time when the event happened

        public MidiEvent(int midiNumber, int velocity, double timestamp) {
            MidiNumber = midiNumber;
            Velocity = velocity;
            Timestamp = timestamp;
        }
    }

    public class MidiLooper {

        private List<MidiEvent> _events = new List<MidiEvent>();
        private List<MidiEvent> _eventsToLoopPlay = new List<MidiEvent>();
        private bool _recording = false;
        private bool _playing = false;
        private double _startTime;
        private double _duration;
        private CsoundBasicSynthesizer _csoundBasicSynthesizer;

        private double _startPlayTime;

        private List<(int, int)> _currentNotesOn = new List<(int, int)>();

        public bool IsRecording => _recording;

        public Action OnPlayLoop { get; set; }
        public Action OnStopLoop { get; set; }
        public Action OnPlayNoteEvent { get; set; }

        public MidiLooper(CsoundBasicSynthesizer csoundBasicSynthesizer) {
            _csoundBasicSynthesizer = csoundBasicSynthesizer;
        }

        public void AddNote(int note, int velocity, double currentTime) {
            if(!_recording) return;
            if (velocity > 0) {
                _currentNotesOn.Add((note, velocity));
            } else {
                _currentNotesOn.RemoveAll(element => element.Item1 == note);
            }
            AddEvent(note, velocity, currentTime);
        }

        public void StartRecording(double currentTime) {
            _csoundBasicSynthesizer.FlushNotes();
            _events.Clear();
            _eventsToLoopPlay.Clear();            

            _recording = true;
            _playing = false;
            _startTime = currentTime;

            //Record current notes on
            for (int i = 0; i < _currentNotesOn.Count; i++) {
                var note = _currentNotesOn[i].Item1;
                var velocity = _currentNotesOn[i].Item2;
                AddEvent(note, velocity, currentTime);
            }

            _currentNotesOn.Clear();
        }

        private void AddEvent(int midiNumber, int velocity, double currentTime) {
            var midiEvent = new MidiEvent(midiNumber, velocity, currentTime - _startTime);//relative to duration
            _events.Add(midiEvent);//relative to duration
        }

        public void StopRecording(double currentTime) {
            _recording = false;
            _duration = currentTime - _startTime;
            //FixHangingNotes();

            //record current notes off
            for (int i = 0; i < _currentNotesOn.Count; i++) {
                var note = _currentNotesOn[i].Item1;
                AddEvent(note, 0, currentTime);
            }
            _currentNotesOn.Clear();
            Play(currentTime);
        }

        public void Play(double currentTime) {
            if (_events.Count == 0) return;
            PlayRestartLoop(currentTime);
            OnPlayLoop?.Invoke();
        }

        private void PlayRestartLoop(double currentTime) {            
            _startPlayTime = currentTime;
            _playing = true;
            _eventsToLoopPlay.Clear();
            for (int i = 0; i < _events.Count; i++) {
                _eventsToLoopPlay.Add(_events[i]);
            }
        }

        public void Stop() {
            _playing = false;
            _eventsToLoopPlay.Clear();
            _csoundBasicSynthesizer.FlushNotes();
            OnStopLoop?.Invoke();
        }

        public void Update(double currentTime) {
            if (_playing) {
                var loopTime = currentTime - _startPlayTime;
                if (loopTime < _duration) {
                    //Play in loop time
                    for (int i = 0; i < _eventsToLoopPlay.Count; i++) {
                        if (loopTime >= _eventsToLoopPlay[i].Timestamp) {
                            ExecuteNoteEvent(_eventsToLoopPlay[i].MidiNumber, _eventsToLoopPlay[i].Velocity);
                            _eventsToLoopPlay.Remove(_eventsToLoopPlay[i]);
                            i--;
                        }
                    }
                } else {
                    //Play left notes
                    while (_eventsToLoopPlay.Count != 0) {
                        ExecuteNoteEvent(_eventsToLoopPlay[0].MidiNumber, _eventsToLoopPlay[0].Velocity);
                        _eventsToLoopPlay.Remove(_eventsToLoopPlay[0]);
                    }
                    PlayRestartLoop(currentTime);
                }
            }
        }

        void ExecuteNoteEvent(int note, int velocity) { 
            if (velocity > 0) {
                _csoundBasicSynthesizer.Play(note, velocity);
                OnPlayNoteEvent?.Invoke();
            } else {
                _csoundBasicSynthesizer.Stop(note);
            }
        }

        //private List<int> _hangingNotesOn = new List<int>();
        ////private List<int> _hangingNotesOff = new List<int>(); //This must be handed when collecting events before recording
        //private void FixHangingNotes() {

        //    _hangingNotesOn.Clear();
        //    //_hangingNotesOff.Clear();
        //    //Identify hanging notes
        //    for (int i = 0; i < _events.Count; i++) {
        //        if (_events[i].Velocity > 0) {
        //            _hangingNotesOn.Add(_events[i].MidiNumber);
        //        } else {
        //            if (_hangingNotesOn.Contains(_events[i].MidiNumber)) {
        //                _hangingNotesOn.Remove(_events[i].MidiNumber);
        //            }
        //        }

        //    }
        //    //for (int i = _events.Count - 1; i >= 0; i--) {
        //    //    if (_events[i].Velocity <= 0) {
        //    //        _hangingNotesOff.Add(_events[i].MidiNumber);                    
        //    //    } else {
        //    //        if (_hangingNotesOff.Contains(_events[i].MidiNumber)) {
        //    //            _hangingNotesOff.Remove(_events[i].MidiNumber);
        //    //        }
        //    //    }
        //    //}

        //    //Fix hanging notes
        //    for (int i = 0; i < _hangingNotesOn.Count; i++) {
        //        _events.Add(new MidiEvent(_hangingNotesOn[i], 0, _duration));
        //    }
        //    //for (int i = 0; i < _hangingNotesOff.Count; i++) {
        //    //    _events.Add(new MidiEvent(_hangingNotesOff[i], 100, _startTime));
        //    //}
        //}   

    }
}
