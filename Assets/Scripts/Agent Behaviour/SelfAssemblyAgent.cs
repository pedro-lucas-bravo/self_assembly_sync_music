using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public partial class SelfAssemblyAgent {

    public enum State {
        Wandering,
        WanderingAndDetecting,
        ToJoin,
        IndirectToJoin,
        Joined
    }

    public int JointsSize { get; set; }
    public float Speed { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Agent AgentContext { get; set; }
    public float JointsRadius { get; set; }
    public float RadiusBoundary { get; set; }

    public float DefaultAudioGain { get; set; } = 0.2f;

    public float MaxJoinTime { get => maxJoinTime_; set => maxJoinTime_ = value; }
    public float OnlyWanderingTime { get; set; } = 1f;

    public State CurrentState {
        get => _currentState;
        set {
            if (_currentState != value)
                OnChangeState(value, _currentState);
            _currentState = value;
        }
    }

    public int SlotAssigned { get; set; } = -1;

    [System.NonSerialized]
    private SelfAssemblyAgent[] _joinedAgents = null;
    private SelfAssemblyAgent _agentToJoin = null;
    private int _agentToJoinSlot = -1;

    private SelfAssemblyAgent _agentToFollow = null;
    private int _agentToFollowSlot = -1;
    private int _localSlotForFollowing = -1;

    private State _currentState = State.Wandering;

    //Visual feedback
    private Color _wanderingColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);//Orange
    private Color _wanderingAndDetectingColor = Color.cyan;
    private Color _toJoinColor = Color.green;
    private Color _joinedColor = Color.blue;
    private Color _indirectToJoinColor = Color.yellow;

    private float maxJoinTime_ = 1f;
    private float joinTimer_ = 0f;
    private float onlyWanderingTimer_ = 0f;
    private bool _detach = false;

    private Renderer _pulseFeedbackRender;

    private CsoundUnity _globalSoundSource;

    public Action<float, SelfAssemblyAgent> OnJoin { get; set; }
    public Action<float, SelfAssemblyAgent> OnDetach { get; set; }
    public SelfAssemblyAgent[] JoinedAgents => _joinedAgents;

    public SelfAssemblyAgent(Vector3 initPosition,
                            Vector3 initDirection,
                            float speed, int jointsSize,
                            float jointRadius,
                            float radiusboundary,
                            CsoundUnity globalSoundSource,
                            Agent agentContex) {
        Position = initPosition;
        Direction = initDirection;
        Speed = speed;
        JointsSize = jointsSize;
        JointsRadius = jointRadius;
        RadiusBoundary = radiusboundary;
        _joinedAgents = new SelfAssemblyAgent[JointsSize];
        AgentContext = agentContex;
        _globalSoundSource = globalSoundSource;
        _pulseFeedbackRender = agentContex.transform.Find("PulseFeedback").GetComponent<Renderer>();
        InitializeMusicFeatures();
        InitializeSyncFeatures();
        OnChangeState(State.Wandering, State.Wandering);
    }

    public void UpdateMovement(float deltaTime) {

        //State Machine
        switch (CurrentState) {
            case State.Wandering: {
                    Position += Speed * deltaTime * Direction;
                    //Bouncing random direction
                    var detected = DetectBoundaryAndChangeDirection();
                    if (detected) {
                        onlyWanderingTimer_ = 0f;
                    }
                    if (onlyWanderingTimer_ < OnlyWanderingTime) {
                        onlyWanderingTimer_ += deltaTime;
                    } else {
                        onlyWanderingTimer_ = 0f;
                        CurrentState = State.WanderingAndDetecting;
                    }
                }
                break;
            case State.WanderingAndDetecting: {
                    Position += Speed * deltaTime * Direction;
                    //Bouncing random direction
                    var detected = DetectBoundaryAndChangeDirection();
                    if (detected) {
                        onlyWanderingTimer_ = 0f;
                        CurrentState = State.Wandering;
                    }


                    //if (joinTimer_ < maxJoinTime_) {
                    //    joinTimer_ += deltaTime;
                    //} else {
                    //    joinTimer_ = 0f;
                    //    DetachFromStructure();
                    //}
                }
                break;
            case State.ToJoin:
                //Travel to join while the other slot is available, we have an available slot (since maybe other could join), 
                //and we are not in the same structure
                if (!DetectBoundaryAndChangeDirection() &&
                    (_agentToJoin.IsSlotAvailable(_agentToJoinSlot) && SlotAvailable >= 0 && !OtherIsInTheSameStructure(_agentToJoin))) {
                    var otherSlotPosition = _agentToJoin.SlotPosition(_agentToJoinSlot);
                    Position += (otherSlotPosition - Position).normalized * Speed * deltaTime;
                    //var detetected = DetectBoundaryAndChangeDirection();
                    //if (detetected) {
                    //    //Cancel join
                    //    SetAllToState(State.Joined);
                    //    CurrentState = State.WanderingAndDetecting;
                    //} else {
                        if (Vector3.Distance(Position, _agentToJoin.Position)
                            <=
                            _agentToJoin.AgentContext.transform.lossyScale.x * 0.5f) {//If the agent is or pass the surface of the other agent
                                                                                      //Join
                            JoinTo(_agentToJoin, _agentToJoinSlot);//Changes in state happens here
                        }
                    //}
                } else {
                    //Cancel join
                    SetAllToState(State.Joined);
                    CurrentState = State.Wandering;
                }
                break;
            case State.Joined:
            case State.IndirectToJoin:
                Position = _agentToFollow.SlotPosition(_agentToFollowSlot);
                Position -= RotateToSlotDirection(_localSlotForFollowing, _agentToFollow.SlotLocalPosition(_agentToFollowSlot).normalized);
                //if (CurrentState == State.Joined) { //start: JUST for testing
                //    if (joinTimer_ < maxJoinTime_) {
                //        joinTimer_ += deltaTime;
                //    } else {
                //        DetachFromStructure();
                //    }                
                //}//end: JUST for testing
                break;
            default:
                break;
        }
        if (!_detach) {
            if (joinTimer_ < maxJoinTime_) {
                joinTimer_ += deltaTime;
            } else {
                _detach = true;
            }
        }
    }

    public bool BoundaryDetected() {
        return Position.magnitude >= RadiusBoundary;
    }

    private void ChangeDirection() {
        var randomDir = UnityEngine.Random.onUnitSphere;
        var normalToBoundary = Position.normalized;
        if (Vector3.Angle(normalToBoundary, randomDir) < 90.0f)
            randomDir = -randomDir;
        Direction = randomDir;
    }

    private bool DetectBoundaryAndChangeDirection() {
        bool detected = false;
        //Bouncing random direction
        if (BoundaryDetected()) {
            ChangeDirection();
            detected = true;
        }
        return detected;
    }

    public bool IsAlone {
        get {
            //Use traditional for loop to avoid Linq garbage
            for(int i = 0; i < _joinedAgents.Length; i++) {
                if (_joinedAgents[i] != null)
                    return false;
            }
            return true;
        }    
    }

    public int SlotAvailable => System.Array.IndexOf(_joinedAgents, null);

    public bool IsSlotAvailable(int slot) {
        return _joinedAgents[slot] == null;
    }

    public Vector3 SlotLocalPosition(int slot) {
        var slotDir = AgentContext.transform.right;
        switch (slot) {
            case 0:
                slotDir = AgentContext.transform.right;
                break;
            case 1:
                slotDir = -AgentContext.transform.right;
                break;
            case 2:
                slotDir = AgentContext.transform.forward;
                break;
            case 3:
                slotDir = -AgentContext.transform.forward;
                break;
            case 4:
                slotDir = AgentContext.transform.up;
                break;
            case 5:
                slotDir = -AgentContext.transform.up;
                break;
            default:
                break;
        }
        return slotDir * AgentContext.transform.lossyScale.x * 0.5f;
    }

    public Vector3 SlotPosition(int slot) {
        return Position + SlotLocalPosition(slot);
    }

    public Vector3 RotateToSlotDirection(int localSlot, Vector3 otherSlotDirection) {
        var slotDir = AgentContext.transform.right;
        switch (localSlot) {
            case 0:
                AgentContext.transform.right = -otherSlotDirection;
                slotDir = AgentContext.transform.right;
                break;
            case 1:
                AgentContext.transform.right = otherSlotDirection;
                slotDir = -AgentContext.transform.right;
                break;
            case 2:
                AgentContext.transform.forward = -otherSlotDirection;
                slotDir = AgentContext.transform.forward;
                break;
            case 3:
                AgentContext.transform.forward = otherSlotDirection;
                slotDir = -AgentContext.transform.forward;
                break;
            case 4:
                AgentContext.transform.up = -otherSlotDirection;
                slotDir = AgentContext.transform.up;
                break;
            case 5:
                AgentContext.transform.up = otherSlotDirection;
                slotDir = -AgentContext.transform.up;
                break;
            default:
                break;
        }
        var positionCompensation = slotDir * AgentContext.transform.lossyScale.x * 0.5f;
        return positionCompensation;
    }

    public void JoinTo(SelfAssemblyAgent otherAgent, int otherSlot) {
        int slot = SlotAvailable;
        if (slot >= 0 && otherAgent.IsSlotAvailable(otherSlot)) {
            //Sync this before joining to reduce overhead
            var otherPhase = otherAgent.GetPhase();
            SyncStructure(otherPhase);
            //Debug.Log("MyPhase: " + GetPhase() + " OtherPhase: " + otherPhase);
            //Joining
            _joinedAgents[slot] = otherAgent;
            _agentToFollow = otherAgent;
            _agentToFollowSlot = otherSlot;
            _localSlotForFollowing = slot;
            otherAgent._joinedAgents[otherSlot] = this;
            otherAgent._agentToFollow = this;
            otherAgent._agentToFollowSlot = slot;
            otherAgent._localSlotForFollowing = otherSlot;
            CurrentState = State.Joined;

            //CurrentState = _agentToJoin.CurrentState == State.Wandering || _agentToJoin.CurrentState == State.Joined ?
            //    State.Joined : State.IndirectToJoin;
            //Give control to the other agent
            FixFollowersStructure(_agentToFollow);
            SetAllToState(State.Joined);
            _agentToFollow.CurrentState = State.Wandering;
            joinTimer_ = 0f;
            _detach = false;

            //update structure count
            UpdateStructureCount(CurrentStructureCount + otherAgent.CurrentStructureCount);

            //Sound and music
            ReassingNotes();
            
            if (OnJoin != null)
                OnJoin(SelfAssemblyController.Instance.TotalTime, this);
        } else {
            SetAllToState(State.Joined);
            CurrentState = State.Wandering;
        }        
    }

    public void DetachFromStructure() {     

        //Remove from other attached agents
        for (int i = 0; i < _joinedAgents.Length; i++) {
            if (_joinedAgents[i] != null) {
                var otherIndex = System.Array.IndexOf(_joinedAgents[i]._joinedAgents, this);
                if (otherIndex >= 0) {
                    //_joinedAgents[i].StopJointSound(otherIndex);
                    _joinedAgents[i]._joinedAgents[otherIndex] = null;                    
                    if (_joinedAgents[i].IsAlone)
                        _joinedAgents[i].CurrentState = State.Wandering;
                }
                //Count and update structure count of each separated group
                _joinedAgents[i].CountStrutureAgents();
            }
        }

        //Remove from self
        System.Array.Fill(_joinedAgents, null);
        onlyWanderingTimer_ = 0f;

        //update structure count
        UpdateStructureCount(1);

        if (OnDetach != null)
            OnDetach(SelfAssemblyController.Instance.TotalTime, this);
    }

    int _currentStructureCount = 1;
    public int CurrentStructureCount => _currentStructureCount;

    public void UpdateStructureCount(int newCount) { 
        if(CurrentStructureCount != newCount) {
            _currentStructureCount = newCount;
            for (int i = 0; i < _joinedAgents.Length; i++) {
                if (_joinedAgents[i] != null) {
                    _joinedAgents[i].UpdateStructureCount(newCount);
                }
            }
        }
    }


    //public void AssumeControlOrBeControlled() { 
    //    //Search in the structure if someone in control (meaning in Wandering state)
    //    var visitedAgents = new List<SelfAssemblyAgent>();
    //    void SearchInStructure(SelfAssemblyAgent agent, ref bool found, ref State stateOfFound) {
    //        visitedAgents.Add(agent);
    //        if (agent != this && 
    //            (agent.CurrentState == State.Wandering || agent.CurrentState == State.ToJoin)) {
    //            found = true;         
    //            stateOfFound = agent.CurrentState;
    //        } else {
    //            for (int i = 0; i < agent._joinedAgents.Length; i++) {
    //                if (agent._joinedAgents[i] != null && !visitedAgents.Contains(agent._joinedAgents[i])) {
    //                    SearchInStructure(agent._joinedAgents[i], ref found, ref stateOfFound);
    //                }
    //            }
    //        }
    //    }
    //    bool foundSomeoneElseInControl = false;
    //    State stateOfFound = State.Wandering;
    //    SearchInStructure(this, ref foundSomeoneElseInControl, ref stateOfFound);
    //    if (foundSomeoneElseInControl) {
    //        //If someone in control, then this agent is controlled
    //        CurrentState = stateOfFound == State.Wandering ? State.Joined : State.IndirectToJoin;
    //    } else {
    //        //If no one in control, then this agent is in control
    //        SetAllToState(State.Joined);
    //        CurrentState = State.Wandering;
    //    }
    //}

    public void OnDetectCloseAvailableAgent(SelfAssemblyAgent otherAgent, int otherAgentFreeSlot) {
        //*Check if this agent is attached to a structure and detach it
        //Remove from other attached agents if not alone
        if (_detach && !IsAlone) {
            DetachFromStructure();
        }

        //* Go to the other agent
        //Give complete control to join
        SetAllToState(State.IndirectToJoin);
        //Only this agent join
        CurrentState = State.ToJoin;
        _agentToJoin = otherAgent;
        _agentToJoinSlot = otherAgentFreeSlot;
        // Now this agent is in control
        FixFollowersStructure(this);
    }

    private void SetAllToState(State state) {
        var visitedAgents = new List<SelfAssemblyAgent>();
        void SetToState(SelfAssemblyAgent agent) {
            visitedAgents.Add(agent);
            agent.CurrentState = state;
            for (int i = 0; i < agent._joinedAgents.Length; i++) {
                if (agent._joinedAgents[i] != null && !visitedAgents.Contains(agent._joinedAgents[i])) {
                    SetToState(agent._joinedAgents[i]);
                }
            }
        }
        SetToState(this);
    }

    [System.NonSerialized]
    List<SelfAssemblyAgent> _helperVisitedAgentsOthersSameStructure = new List<SelfAssemblyAgent>();
    //public bool OtherIsInTheSameStructureX(SelfAssemblyAgent otherAgent) {
    //    //Search in the structure if someone in control (meaning in Wandering state)
    //    _helperVisitedAgentsOthersSameStructure.Clear();
    //    void SearchInStructure(SelfAssemblyAgent agent, ref bool found) {
    //        _helperVisitedAgentsOthersSameStructure.Add(agent);
    //        if (agent == otherAgent) {
    //            found = true;
    //        } else {
    //            for (int i = 0; i < agent._joinedAgents.Length; i++) {
    //                if (agent._joinedAgents[i] != null && !_helperVisitedAgentsOthersSameStructure.Contains(agent._joinedAgents[i])) {
    //                    SearchInStructure(agent._joinedAgents[i], ref found);
    //                }
    //            }
    //        }
    //    }
    //    bool found = false;
    //    SearchInStructure(this, ref found);
    //    return found;
    //}

    [System.NonSerialized]
    Stack<SelfAssemblyAgent> _helperAgentStack = new Stack<SelfAssemblyAgent>();
    public bool OtherIsInTheSameStructure(SelfAssemblyAgent otherAgent) {
        _helperVisitedAgentsOthersSameStructure.Clear();
        _helperAgentStack.Clear();

        // Start with the current agent
        _helperAgentStack.Push(this);
        _helperVisitedAgentsOthersSameStructure.Add(this);

        while (_helperAgentStack.Count > 0) {
            SelfAssemblyAgent currentAgent = _helperAgentStack.Pop();

            if (currentAgent == otherAgent) {
                return true;
            }

            for (int i = 0; i < currentAgent._joinedAgents.Length; i++) {
                SelfAssemblyAgent joinedAgent = currentAgent._joinedAgents[i];
                if (joinedAgent != null && !_helperVisitedAgentsOthersSameStructure.Contains(joinedAgent)) {
                    _helperAgentStack.Push(joinedAgent);
                    _helperVisitedAgentsOthersSameStructure.Add(joinedAgent);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Reassing the agents to follow on every agent to fix who follows who and avoid loops
    /// </summary>
    /// <param name="wanderingAgent"></param>
    public void FixFollowersStructure(SelfAssemblyAgent wanderingAgent) {
        var visitedAgents = new List<SelfAssemblyAgent>();
        void TraverseInStructure(SelfAssemblyAgent agent) {
            visitedAgents.Add(agent);
            for (int i = 0; i < agent._joinedAgents.Length; i++) {
                if (agent._joinedAgents[i] != null && !visitedAgents.Contains(agent._joinedAgents[i])) {
                    agent._joinedAgents[i]._agentToFollow = agent;
                    agent._joinedAgents[i]._agentToFollowSlot = i;
                    agent._joinedAgents[i]._localSlotForFollowing = System.Array.IndexOf(agent._joinedAgents[i]._joinedAgents, agent);
                    TraverseInStructure(agent._joinedAgents[i]);
                }
            }
        }
        TraverseInStructure(wanderingAgent);
    }

    private void OnChangeState(State newState, State oldState) {
        switch (newState) {
            case State.Wandering:
                AgentContext.visualFeeback.SetColor(_wanderingColor, false);
                break;
            case State.WanderingAndDetecting:
                AgentContext.visualFeeback.SetColor(_wanderingAndDetectingColor, false);
                break;
            case State.ToJoin:
                AgentContext.visualFeeback.SetColor(_toJoinColor, false);
                break;
            case State.Joined:
                AgentContext.visualFeeback.SetColor(_joinedColor, false);
                break;
            case State.IndirectToJoin:
                AgentContext.visualFeeback.SetColor(_indirectToJoinColor, false);
                break;
            default:
                break;
        }
    }

    [System.NonSerialized]
    private List<SelfAssemblyAgent> visitedStrutureAgents = new List<SelfAssemblyAgent>();
    public List<SelfAssemblyAgent> GetStructure() {
        visitedStrutureAgents.Clear();
        void TraverseInStructure(SelfAssemblyAgent agent) {
            visitedStrutureAgents.Add(agent);
            for (int i = 0; i < agent._joinedAgents.Length; i++) {
                if (agent._joinedAgents[i] != null && !visitedStrutureAgents.Contains(agent._joinedAgents[i])) {
                    TraverseInStructure(agent._joinedAgents[i]);
                }
            }
        }
        TraverseInStructure(this);
        return visitedStrutureAgents;
    }
}