using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

/// <summary>
/// SelfAssemblyAgent.Music.cs
/// </summary>
public partial class SelfAssemblyAgent{
    #region Theoretical Music Structure

    public static readonly int[] ChromaticScale = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    public static readonly int[] MinorBluesScale = new int[] { 0, 3, 5, 6, 7, 10 };
    public static readonly int[] MajorBluesScale = new int[] { 0, 2, 4, 5, 6, 9 };
    public static readonly int[] MajorScale = new int[] { 0, 2, 4, 5, 7, 9, 11 };
    public static readonly int[] MinorScale = new int[] { 0, 2, 3, 5, 7, 8, 10 };
    public static readonly int[] MajorPentatonic = new int[] { 0, 2, 4, 7, 9 };
    public static readonly int[] FifthSequence = new int[] { 0, 7, 2, 9, 4, 11, 6, 1, 8, 3, 10, 5 };

    private static readonly int[] _triadCache = new int[3];
    private static readonly Chord[] _circleSectionCache1 = new Chord[6];
    private static readonly Chord[] _circleSectionCache2 = new Chord[6];
    private static readonly Chord[] _circleSectionCache3 = new Chord[6];
    private static readonly int[] _scaleCache7 = new int[7];//Might be others with diffrerent size
    private static readonly int[] _allNotesFromCircleSection = new int[18];

    public enum ChordType {
        Major,
        Minor
    }

    public struct Chord {
        public int RootNote;
        public ChordType ChordType;


        public Chord(int rootNote, ChordType chordType) {
            RootNote = rootNote;
            ChordType = chordType;
        }
        
        //To String by showing root note as a letter and chord type
        public override string ToString() {
            string noteLetter = GetNoteLetter(RootNote);
            return noteLetter + " " + ChordType.ToString();
        }

        public override bool Equals(object obj) {
            return obj is Chord chord &&
                   RootNote == chord.RootNote &&
                   ChordType == chord.ChordType;
        }

        public static bool operator ==(Chord left, Chord right) {
            return left.Equals(right);
        }

        public static bool operator !=(Chord left, Chord right) {
            return !(left == right);
        }

        public override int GetHashCode() {
            int hashCode = RootNote.GetHashCode() + ChordType.GetHashCode();
            return hashCode;
        }
    }

    private static string GetNoteLetter(int note) {
        switch (note) { 
            case 0:
                return "C";
            case 1:
                return "C#";
            case 2:
                return "D";
            case 3:
                return "D#";
            case 4:
                return "E";
            case 5:
                return "F";
            case 6:
                return "F#";
            case 7:
                return "G";
            case 8:
                return "G#";
            case 9:
                return "A";
            case 10:
                return "A#";
            case 11:
                return "B";
            default:
                return "C";
        }
    }

    //IMPORTANT: It will not work if process are performed in multiple threads
    public static int[] GetTriad(Chord chord) {
        (int, int, int) triad;
        switch (chord.ChordType) {
            case ChordType.Major:
                triad = GetMayorTriad(chord.RootNote);
                break;
            case ChordType.Minor:
                triad = GetMinorTriad(chord.RootNote);
                break;
            default:
                triad =  GetMayorTriad(chord.RootNote);
                break;               
        }
        _triadCache[0] = triad.Item1;
        _triadCache[1] = triad.Item2;
        _triadCache[2] = triad.Item3;
        return _triadCache;
    }

    public static (int, int, int) GetMayorTriad(int rootNote) {
        return (rootNote, rootNote + 4, rootNote + 7);
    }

    public static (int, int, int) GetMinorTriad(int rootNote) {
        return (rootNote, rootNote + 3, rootNote + 7);
    }

    public static (int, int, int) GetDiminishedTriad(int rootNote) {
        return (rootNote, rootNote + 3, rootNote + 6);
    }

    public static (int, int, int) GetAugmentedTriad(int rootNote) {
        return (rootNote, rootNote + 4, rootNote + 8);
    }

    public static int[] GetMajorScale(int rootNote) {
        _scaleCache7[0] = rootNote;
        _scaleCache7[1] = rootNote + 2;
        _scaleCache7[2] = rootNote + 4;
        _scaleCache7[3] = rootNote + 5;
        _scaleCache7[4] = rootNote + 7;
        _scaleCache7[5] = rootNote + 9;
        _scaleCache7[6] = rootNote + 11;
        return _scaleCache7;
    }

    public static int[] GetNaturalMinorScale(int rootNote) {
        _scaleCache7[0] = rootNote;
        _scaleCache7[1] = rootNote + 2;
        _scaleCache7[2] = rootNote + 3;
        _scaleCache7[3] = rootNote + 5;
        _scaleCache7[4] = rootNote + 7;
        _scaleCache7[5] = rootNote + 8;
        _scaleCache7[6] = rootNote + 10;
        return _scaleCache7;
    }


    public static (Chord[], Chord[], Chord[]) GetCircleAdjacentSections(Chord chord) {
        var index = System.Array.IndexOf(FifthSequence, chord.RootNote);
        var nextIndex = (index + 1) % FifthSequence.Length;
        var previousIndex = (index - 1 + FifthSequence.Length) % FifthSequence.Length;

        int otherTypeIndex, otherTypeNextIndex, otherTypePreviousIndex;
        ChordType chordType = chord.ChordType;
        ChordType otherChordType = chordType == ChordType.Major ? ChordType.Minor : ChordType.Major;
        switch (chord.ChordType) {
            case ChordType.Major:
                otherTypeIndex = (index + 3) % FifthSequence.Length;                
                break;
            case ChordType.Minor:
            default:
                otherTypeIndex = (index - 3 + FifthSequence.Length) % FifthSequence.Length;
                break;
        }
        otherTypeNextIndex = (otherTypeIndex + 1) % FifthSequence.Length;
        otherTypePreviousIndex = (otherTypeIndex - 1 + FifthSequence.Length) % FifthSequence.Length;

        //Central section
        _circleSectionCache1[0] = new Chord(FifthSequence[previousIndex], chordType);
        _circleSectionCache1[1] = new Chord(FifthSequence[index], chordType);
        _circleSectionCache1[2] = new Chord(FifthSequence[nextIndex], chordType);
        _circleSectionCache1[3] = new Chord(FifthSequence[otherTypePreviousIndex], otherChordType);
        _circleSectionCache1[4] = new Chord(FifthSequence[otherTypeIndex], otherChordType);
        _circleSectionCache1[5] = new Chord(FifthSequence[otherTypeNextIndex], otherChordType);

        //Previous section
        _circleSectionCache2[0] = new Chord(FifthSequence[(previousIndex - 1 + FifthSequence.Length) % FifthSequence.Length], chordType);
        _circleSectionCache2[1] = new Chord(FifthSequence[previousIndex], chordType);
        _circleSectionCache2[2] = new Chord(FifthSequence[index], chordType);
        _circleSectionCache2[3] = new Chord(FifthSequence[(otherTypePreviousIndex - 1 + FifthSequence.Length) % FifthSequence.Length], otherChordType);
        _circleSectionCache2[4] = new Chord(FifthSequence[otherTypePreviousIndex], otherChordType);
        _circleSectionCache2[5] = new Chord(FifthSequence[otherTypeIndex], otherChordType);

        //Next section
        _circleSectionCache3[0] = new Chord(FifthSequence[index], chordType);
        _circleSectionCache3[1] = new Chord(FifthSequence[nextIndex], chordType);
        _circleSectionCache3[2] = new Chord(FifthSequence[(nextIndex + 1) % FifthSequence.Length], chordType);
        _circleSectionCache3[3] = new Chord(FifthSequence[otherTypeIndex], otherChordType);
        _circleSectionCache3[4] = new Chord(FifthSequence[otherTypeNextIndex], otherChordType);
        _circleSectionCache3[5] = new Chord(FifthSequence[(otherTypeNextIndex + 1) % FifthSequence.Length], otherChordType);

        return (_circleSectionCache1, _circleSectionCache2, _circleSectionCache3);
    }

    private int CalculateChordDistance(int[] notes, int[] chord) {
        int totalDistance = 0;
        for (int i = 0; i < notes.Length; i++) { 
            totalDistance += CalculateChordDistance(notes[i], chord);
        }
        return totalDistance;
    }

    private int GetPitchClass(int note) {
        return note < 0 ? 12 + note % 12 : note % 12;
    }

    private int CalculateChordDistance(int note, int[] chord) {
        //normalize to a class note also considering negative notes
        note = GetPitchClass(note);
       return chord.Min(chordNote =>
                          Mathf.Min(Mathf.Abs(note - chordNote), 12 - Mathf.Abs(note - chordNote)));
    }

    private int CalculateChordAffinity(int[] notes, int[] chord) {
        int totalAffinity = 0;
        for (int i = 0; i < notes.Length; i++) {
            totalAffinity += chord.Contains(GetPitchClass(notes[i])) ? 1 : 0;
        }
        return totalAffinity;
    }

    public Chord FindClosestChord(int[] notes) {

        Chord bestChord = new Chord(0, ChordType.Major);//Deaful a C major chord
        int lowestTotalDistance = int.MaxValue;

        //Iterate chord types and roots in the circle of fifths
        int length = System.Enum.GetValues(typeof(ChordType)).Length;
        for (int chordType = 0; chordType < length; chordType++) {
            for (int rootIndex = 0; rootIndex < FifthSequence.Length; rootIndex++) {
                var fifthChord = FifthSequence[rootIndex];
                var triad = GetTriad(new Chord(fifthChord, (ChordType)chordType));
                int totalDistance = CalculateChordDistance(notes, triad);
                if (totalDistance < lowestTotalDistance) {
                    lowestTotalDistance = totalDistance;
                    bestChord = new Chord(fifthChord, (ChordType)chordType);
                }
            }
        }

        return bestChord;
    }

    public Chord FindMostAffineChord(int[] notes) {

        Chord bestChord = new Chord(0, ChordType.Major);//Deaful a C major chord
        int highestAffinity = -1;

        //Iterate chord types and roots in the circle of fifths
        int length = System.Enum.GetValues(typeof(ChordType)).Length;
        for (int chordType = 0; chordType < length; chordType++) {
            for (int rootIndex = 0; rootIndex < FifthSequence.Length; rootIndex++) {
                var fifthChord = FifthSequence[rootIndex];
                var triad = GetTriad(new Chord(fifthChord, (ChordType)chordType));
                int totalAffinity = CalculateChordAffinity(notes, triad);
                if (totalAffinity > highestAffinity) {
                    highestAffinity = totalAffinity;
                    bestChord = new Chord(fifthChord, (ChordType)chordType);
                }
            }
        }

        return bestChord;
    }

    public static float MIDItoFrequency(int midiNote) {
        return 440f * Mathf.Pow(2f, (midiNote - 69f) / 12f);
    }

    #endregion Theoretical Music Structure

    private int[] notesOnJoints;
    private float[] _proportionsUtilities;
    private void InitializeMusicFeatures() {
        //sourcesOnJoints = AgentContext.GetComponentsInChildren<CsoundUnity>();
        //for (int i = 0; i < sourcesOnJoints.Length; i++) {
        //    sourcesOnJoints[i].transform.position = SlotPosition(i);
        //}
        notesOnJoints = new int[JointsSize];
        _proportionsUtilities = new float[JointsSize];
        var seed = (int)System.DateTime.Now.Ticks + AgentContext.ID;
        Random.InitState(seed);
        var scaleIndexToUse = Random.Range(0, 3);
        int[] scaleToUse;
        switch (scaleIndexToUse) {
            case 0:
                scaleToUse = MajorScale;
                break;
            case 1:
                scaleToUse = MajorBluesScale;
                break;
            case 2:
                scaleToUse = MajorPentatonic;
                break;
            default:
                scaleToUse = MajorScale;
                break;
        }
        for (int i = 0; i < JointsSize; i++) {
            //var randomMIDInote = Random.Range(48, 108); // from C3(included) to B7(included)
            //var randomMIDInote = Random.Range(60, 80);
            //var randomMIDInote = MajorScale[Random.Range(0, MajorScale.Length)] + 60 + Random.Range(-2, 3) * 12;
            //var randomMIDInote = MinorBluesScale[Random.Range(0, MinorBluesScale.Length)] + 60 + Random.Range(-2, 3) * 12;
            //var randomMIDInote = MajorPentatonic[i % MajorPentatonic.Length] + 60 + Random.Range(-2, 3) * 12;
            var randomMIDInote = scaleToUse[i % scaleToUse.Length] + 60 + Random.Range(-2, 3) * 12;
            //var randomMIDInote = ChromaticScale[i % ChromaticScale.Length] + 60 + Random.Range(-2, 3) * 12;
            var intNote = randomMIDInote - 60; //Assuming as reference C4 = 60
            notesOnJoints[i] = intNote;
        }
        //var closestInitChord = FindMostAffineChord(notesOnJoints);//FindMostAffineChord(notesOnJoints.Skip(0).Take(3).ToArray());//Pick the first 3 notes
        //Debug.LogWarning(/*"Notes: " + string.Join(",", notesOnJoints.Select(note => GetPitchClass(note))) + */"Closest chord in the beginning: " + closestInitChord.ToString());
    }
    
    public void PlayJointSound(int slot) {
        if (_globalSoundSource == null) return;
        var countInStructure = CurrentStructureCount;
        var attack = 0.01f * (1.0 + 99f *Mathf.InverseLerp(5.0f, 20.0f, countInStructure));
        var release = 0.2f * (1.0 + 9f * Mathf.InverseLerp(5.0f, 20.0f, countInStructure));
        var gain = DefaultAudioGain / Mathf.Sqrt(countInStructure);
        if (notesOnJoints != null && slot < notesOnJoints.Length) {
            //_globalSoundSource.SetChannel("freq", notesOnJoints[slot]);

            //"i 1 0 -1 freq attack decay sustain release"
            //_globalSoundSource.SendScoreEvent("i 1." + AgentContext.ID + " 0 -1 " + MIDItoFrequency(notesOnJoints[slot] + 60/*C4 as a reference*/) + " 0.01 0.01 0.7 0.2");
            //_globalSoundSource.SendScoreEvent("i 1." + AgentContext.ID + " 0 0.3 " + MIDItoFrequency(notesOnJoints[slot] + 60/*C4 as a reference*/) + " 0.01 0.01 0.7 0.2");
            _globalSoundSource.SendScoreEvent("i 1." + AgentContext.ID + 
                " 0 0.3 " + 
                MIDItoFrequency(notesOnJoints[slot] + 60/*C4 as a reference*/) + " "+
                attack+" 0.01 0.7 " + release + " " +
                gain
                );
        }
    }

    public void StopJointSound(int slot) {
        //if (notesOnJoints != null && slot < notesOnJoints.Length) {
        //    _globalSoundSource.SendScoreEvent("i -1." + AgentContext.ID + " 0 0");
        //}
    }

    #region Music On Agents

    [System.NonSerialized]
    List<SelfAssemblyAgent> _helperVisitedAgents = new List<SelfAssemblyAgent>();
    List<int[]> _agentsNotes = new List<int[]>();
    public Chord GetClosestStructureChord(bool useDistance) {
        //Search in the structure if someone in control (meaning in Wandering state)
        _helperVisitedAgents.Clear();
        _agentsNotes.Clear();
        void SearchInStructure(SelfAssemblyAgent agent) {
            _helperVisitedAgents.Add(agent);
            _agentsNotes.Add(agent.notesOnJoints);
            for (int i = 0; i < agent._joinedAgents.Length; i++) {
                if (agent._joinedAgents[i] != null && !_helperVisitedAgents.Contains(agent._joinedAgents[i])) {
                    SearchInStructure(agent._joinedAgents[i]);
                }
            }
        }
        SearchInStructure(this);
        var closestChord = useDistance ?
            FindClosestChord(_agentsNotes.SelectMany(array => array).ToArray()):
            FindMostAffineChord(_agentsNotes.SelectMany(array => array).ToArray());// WARNING: It generates garbage
        return closestChord;
    }

    // 3 section with 6 elements each
    int[] _sumSections = new int[3];

    /// <summary>
    /// For now, reassignation is done before the agents are detached from the structure
    /// </summary>
    private void ReassingNotes() {
        //Dont reasign for now
        return;

        bool useDistance = false; //otherwise is affinity
        // 1. Get closest chord in structure, this is the last chord
        //var closestChordInStructure = new Chord(7, ChordType.Major);
        var closestChordInStructure = GetClosestStructureChord(useDistance);

        Debug.Log("Closest chord in structure: " + closestChordInStructure.ToString());

        //THIS 1 is not done, criteria is not clear
        //// 1. Calculate Harmonic Proportions (is it a good name?)
        ////utility (diatance) regarding the last choseen chord in structure
        //var u_lch = CalculateChordDistance(notesOnJoints, GetTriad(closestChordInStructure));
        ////independent utility (distance) regarding the closest chord in structure
        //for (int i = 0; i < notesOnJoints.Length; i++) {
        //    var noteOnJoint = notesOnJoints[i];
        //    var u_lch_i = CalculateChordDistance(noteOnJoint, GetTriad(closestChordInStructure));
        //    //proportion
        //    var p_u_i = ((float)u_lch_i) / u_lch;
        //    _proportionsUtilities[i] = p_u_i;
        //}


        //2. Find the closest adjacent section
        //Find adjacent sections        
        var circleSections = GetCircleAdjacentSections(closestChordInStructure);
        void CalculateSum(Chord[] chordsSetion, int sectionIndex) {
            for (int i = 0; i < chordsSetion.Length; i++) {
                var chordTriad = GetTriad(chordsSetion[i]); 
                if(useDistance)
                    _sumSections[sectionIndex] += CalculateChordDistance(notesOnJoints, chordTriad);
                else
                    _sumSections[sectionIndex] += CalculateChordAffinity(notesOnJoints, chordTriad);
            }
        }        
        CalculateSum(circleSections.Item1, 0);
        CalculateSum(circleSections.Item2, 1);
        CalculateSum(circleSections.Item3, 2);

        var targetIndex = useDistance ?
                            System.Array.IndexOf(_sumSections, _sumSections.Min()):
                            System.Array.IndexOf(_sumSections, _sumSections.Max());
        var selectedChordsSection = targetIndex == 0 ? circleSections.Item1 : targetIndex == 1 ? circleSections.Item2 : circleSections.Item3;

        //3. Reassing notes
        var keyNoteChord = selectedChordsSection[1];
        //*using some musical criteria
        //3.1. Get a random scale from a pool of related scales wih the key note: scale_source
        //Using only major or minor for now
        var scale_source = keyNoteChord.ChordType == ChordType.Major ? GetMajorScale(keyNoteChord.RootNote) : GetNaturalMinorScale(keyNoteChord.RootNote);

        //3.2. Get all the notes related to the triads from the selected chords section: triads_source
        for (int i = 0; i < selectedChordsSection.Length; i++) {
            var triad = GetTriad(selectedChordsSection[i]);
            _allNotesFromCircleSection[i * 3] = triad[0];
            _allNotesFromCircleSection[i * 3 + 1] = triad[1];
            _allNotesFromCircleSection[i * 3 + 2] = triad[2];
        }

        //3.3 Choose randomly one of the source of notes to keep as the searching set and other to be the reassigning set
        var searchingSet = Random.Range(0, 2) == 0 ? scale_source : _allNotesFromCircleSection;
        var reassignSet = searchingSet == scale_source ? _allNotesFromCircleSection : scale_source;

        //3.4. Check the notes in the notesOnJoints and keep the ones that are in the searching set, reassign the ones that are not in the set 
        //      by picking random notes from the reassing set, keep the octave the are playing (int(note/12))
        for (int i = 0; i < notesOnJoints.Length; i++) {
            var note = notesOnJoints[i];
            if (!searchingSet.Any(searchNote => GetPitchClass(searchNote) == GetPitchClass(note))) {
                var old_octave = note / 12;
                var randomNote = reassignSet[Random.Range(0, reassignSet.Length)];
                var new_octave = randomNote / 12;
                notesOnJoints[i] = randomNote + (old_octave - new_octave) * 12;
            }
        }
        //Show NotesONJoints pitch classes
        //Debug.Log("notesOnJoints: " + string.Join(",", notesOnJoints.Select(note => GetPitchClass(note))));
    }

    #endregion Music On Agents
}
