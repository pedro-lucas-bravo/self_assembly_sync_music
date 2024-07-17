using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SelfAssemblyAgent.Sync, synchornization logic, internal orcillator.
/// </summary>

public partial class SelfAssemblyAgent{

    [Header("Music Params")]
    public float Frequency = 1.0f;

    private Color _pulseColor = Color.red;
    private Color _beatColorJoined = Color.magenta;
    private Color _beatColorAlone = Color.white;
    //private float _period => 1.0f / Frequency;

    public System.Action<int,float> OnPhasorClimax { get; set; }


    #region Rhythm Knowledge

    (int, int)[] _divisions = new (int, int)[] {
        (1, 1), (1, 2), (1, 4), (1, 8), 
        //(2, 1), (4, 2)
        //(1, 2)
    };

    private void InitializeSyncFeatures() { 
        //take a random division
        var randomDivision = _divisions[Random.Range(0, _divisions.Length)];
        division_cycles = randomDivision.Item1;
        division_beats = randomDivision.Item2;

        //start phasor in a random position
        //_beatPhasor = Random.Range(0.0f, GetDivisionTime()); 
        //var cyclePhase = GetPhase();
        //_cyclePhasor = cyclePhase;
        _cyclePhasor = Random.Range(0.0f, 1.0f);
        _beatPhasor = _cyclePhasor % GetDivisionTime();
    }

    private float GetDivisionTime() {
        return (float)division_cycles / division_beats;
    }

    #endregion Rhythm Knowledge

    #region Internal Phasor oscillator

    public float _cyclePhasor = 0.0f;
    public float _beatPhasor = 0.0f;
    public int division_cycles = 1;
    public int division_beats = 1;

    private int _cycleCounter = 0;

    public void FixedUpdatePhasor(float fixedDeltaTime) {        
        _cyclePhasor += Frequency * fixedDeltaTime;
        _beatPhasor += Frequency * fixedDeltaTime;

        if (_cyclePhasor >= 1.0f) {
            _cyclePhasor = 0.0f;
            ExecuteSyncCycleActions();

            _cycleCounter = (_cycleCounter + 1) % division_cycles;//teh counting method try to avoid unsync behaviour between the beat phasor and teh cycle phasor
            if (_cycleCounter == 0) {
                _beatPhasor = 0.0f;
                ExecuteSyncBeatActions();
            }            
        }

        if (_beatPhasor >= GetDivisionTime()) {
            _beatPhasor = 0.0f;
            ExecuteSyncBeatActions();
        }
    }

    #endregion Internal Phasor oscillator

    private void ExecuteSyncCycleActions() {
        //ActivePulseFeedback(_pulseColor).Forget();
        if(OnPhasorClimax != null)
            OnPhasorClimax(AgentContext.ID,SelfAssemblyController.Instance.TotalTime);
    }

    private async UniTask ActivePulseFeedback(Color color) {
        _pulseFeedbackRender.material.color = color;
        await UniTask.Delay(100, cancellationToken: AgentContext.destroyCancellationToken);
        _pulseFeedbackRender.material.color = Color.black;
    }

    private int _nextSlotIndex = 0;

    /// <summary>
    /// Trigger actions according to phasor current value _phasor.
    /// </summary>
    private void ExecuteSyncBeatActions() {
        //Debug.Log("ExecuteSyncActions");
        //play occupied slots
        //for(int i = 0; i < _joinedAgents.Length; i++) {
        //    var occupiedSlot = !IsSlotAvailable(i);
        //    if(occupiedSlot) {
        //       PlayJointSound(i);
        //    }
        //}
        if (IsAlone) {
            if(!SelfAssemblyController.Instance.OnlySimulate)
                ActivePulseFeedback(_beatColorAlone).Forget();
            return;
        }
        bool occupiedSlot = false;
        while (!occupiedSlot){
            occupiedSlot = !IsSlotAvailable(_nextSlotIndex);
            if(!occupiedSlot)
                _nextSlotIndex = (_nextSlotIndex + 1) % _joinedAgents.Length;
        } 
        PlayJointSound(_nextSlotIndex);
        _nextSlotIndex = (_nextSlotIndex + 1) % _joinedAgents.Length;
        if (!SelfAssemblyController.Instance.OnlySimulate)
            ActivePulseFeedback(_beatColorJoined).Forget();
    }

    public float GetPhase() {
        //return _beatPhasor % _period;
        return _cyclePhasor;
    }

    [System.NonSerialized]
    List<SelfAssemblyAgent> _helperSyncAgents = new List<SelfAssemblyAgent>();
    private void SyncStructure(float phase) {
        _helperSyncAgents.Clear();
        void SearchInStructure(SelfAssemblyAgent agent) {
            agent.SyncOscillator(phase);
            _helperSyncAgents.Add(agent);
            for (int i = 0; i < agent._joinedAgents.Length; i++) {
                if (agent._joinedAgents[i] != null && !_helperSyncAgents.Contains(agent._joinedAgents[i])) {
                    SearchInStructure(agent._joinedAgents[i]);
                }
            }
        }
        SearchInStructure(this);
    }

    public void SyncOscillator(float phase) { 
        //Sync is restarting the oscillator
        ResetPhasor(phase);
    }

    private void ResetPhasor(float phase) {
        _cyclePhasor = phase;
        _beatPhasor = phase % GetDivisionTime();
    }

    [System.NonSerialized]
    List<SelfAssemblyAgent> _helperCounterAgents = new List<SelfAssemblyAgent>();
    //private int CountStrutureAgentsX() {
    //    _helperCounterAgents.Clear();
    //    void CountAgents(SelfAssemblyAgent agent) {
    //        _helperCounterAgents.Add(agent);
    //        for (int i = 0; i < agent._joinedAgents.Length; i++) {
    //            if (agent._joinedAgents[i] != null && !_helperCounterAgents.Contains(agent._joinedAgents[i])) {
    //                CountAgents(agent._joinedAgents[i]);
    //            }
    //        }
    //    }
    //    CountAgents(this);
    //    return _helperCounterAgents.Count;
    //}

    [System.NonSerialized]
    Stack<SelfAssemblyAgent> _helperCounterAgentsStack = new Stack<SelfAssemblyAgent>();

    private int CountStrutureAgents() {
        _helperCounterAgents.Clear();
        _helperCounterAgentsStack.Clear();
        _helperCounterAgentsStack.Push(this);
        _helperCounterAgents.Add(this);

        while (_helperCounterAgentsStack.Count > 0) {
            SelfAssemblyAgent currentAgent = _helperCounterAgentsStack.Pop();

            for (int i = 0; i < currentAgent._joinedAgents.Length; i++) {
                SelfAssemblyAgent joinedAgent = currentAgent._joinedAgents[i];
                if (joinedAgent != null && !_helperCounterAgents.Contains(joinedAgent)) {
                    _helperCounterAgentsStack.Push(joinedAgent);
                    _helperCounterAgents.Add(joinedAgent);
                }
            }
        }

        var structureCount = _helperCounterAgents.Count;

        for(int i = 0; i < _helperCounterAgents.Count; i++) {
            _helperCounterAgents[i]._currentStructureCount = structureCount;
        }

        return structureCount;
    }

}
