using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SelfAssemblyController : MonoBehaviour
{

    [Header("Balance")]
    public int agentsSize = 20;
    public float agentsScale = 0.1f;
    public float minAgentSpeed = -10f;
    public float maxAgentSpeed = 10f;
    public float onlyWanderingTime = 1f;
    public float maxJoinTime = 1f;
    [Tooltip("It is relative to agent radius (half of the agenst scale) (e.g. 1f is twice the scale S since 2 * 0.5 * S)")]
    public float agentJoinRadiusFactor = 1f; // It is relative to agent radius (half of the agenst scale) (e.g. 1f is twice the scale S since 2 * 0.5 * S)
    public int maxJointsSize = 6; //distributed evenly in the sphere

    public float radiusBoundary = 1f;
    public Transform boundaryTrans;    
    

    [Header("Music Params")]
    public bool PlaySound = false;
    public float Frequency = 1.0f;
    public float gain = 0.2f;
    public CsoundUnity csoundUnityGen;

    [Header("Simulation")]
    public bool OnlySimulate = false;
    public int iterations = 1000;


    public List<SelfAssemblyAgent> _selfAssemblyAgents;
    private float _actualJointDetectionRadius;// from center to center

    public static SelfAssemblyController Instance { get; private set; }

    public List<SelfAssemblyAgent> Agents => _selfAssemblyAgents;

    private float _totalTimeOnSimulation = 0f;
    public float TotalTime => OnlySimulate ? _totalTimeOnSimulation : Time.timeSinceLevelLoad;

    public System.Action OnFinishSimulation { get; set; }

    #region Unity Callbacks
    //private bool _playTestSound = false;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
            return;
        }

        if (SelfAssemblyExperimentsManager.Instance != null) {
            SelfAssemblyExperimentsManager.Instance.ConfigureController(this);
        }

        InstantiateAgents();
        _actualJointDetectionRadius = agentJoinRadiusFactor * agentsScale * 2f;
        boundaryTrans.localScale = Vector3.one * radiusBoundary * 2f;
        if(!OnlySimulate)
            CentralizedNearCheck().Forget();
    }

    // Update is called once per frame
    void Update() {


        //if (Input.GetKeyDown(KeyCode.Space)) {
        //    if (!_playTestSound) { //Play if not playing
        //        var frequency = 440;
        //        csoundUnityTest.SetChannel("freq", frequency);
        //        //"i1 0 -1 attack decay sustain release"
        //        csoundUnityTest.SendScoreEvent("i1 0 -1 0.01 0.01 0.7 0.2");
        //    } else { // Stop if playing
        //        csoundUnityTest.SendScoreEvent("i-1 0 0");
        //    }
        //    _playTestSound = !_playTestSound;
        //}
    }

    bool _ready = false;
    private void FixedUpdate() {
        if (OnlySimulate) {
            if (!_ready) {
                SimulateIterations();
                _ready = true;
            }
        } else {
            //CentralizedCheckForSimulation();
            UpdateAgents(Time.fixedDeltaTime);           
        }
    }

    #endregion Unity Callbacks

    #region Simulation Control

    void InstantiateAgents() {
        AgentsCentralManager.Instance.InstantiateAgents(agentsSize, agentsScale);
        _selfAssemblyAgents = new List<SelfAssemblyAgent>();
        for (int i = 0; i < agentsSize; i++) {
            var agent = new SelfAssemblyAgent(Random.insideUnitSphere * radiusBoundary *0.9f, 
                Random.onUnitSphere, 
                Random.Range(minAgentSpeed, maxAgentSpeed), 
                maxJointsSize, 
                agentJoinRadiusFactor * agentsScale,
                radiusBoundary,
                PlaySound ? csoundUnityGen: null,
                AgentsCentralManager.Instance.AllAgentsArray[i]);
            agent.MaxJoinTime = maxJoinTime;
            agent.OnlyWanderingTime = onlyWanderingTime;
            agent.DefaultAudioGain = gain;
            _selfAssemblyAgents.Add(agent);
            var radiusColor = Color.white;
            radiusColor.a = 0.05f;

            agent.Frequency = Frequency;

            agent.AgentContext.visualFeeback.SetGenericRadius(agentJoinRadiusFactor, radiusColor);
        }
    }


    void UpdateAgents(float fixedDeltaTime) {
        for (int i = 0; i < agentsSize; i++) {
            var agent = _selfAssemblyAgents[i];
            agent.UpdateMovement(fixedDeltaTime);
            agent.AgentContext.transform.position = agent.Position;

            agent.FixedUpdatePhasor(fixedDeltaTime);
        }
    }
 

    /// <summary>
    /// This update function execute the checks by having one agent per frame checking the others, which optimizes the performance
    /// but introduces a delay in the detection of the agents, so when there are more agents, the delay is higher
    /// </summary>
    /// <returns></returns>
    async UniTask CentralizedNearCheck() {
        while (!destroyCancellationToken.IsCancellationRequested) {
            for (int i = 0; i < _selfAssemblyAgents.Count; i++) {
                if (!_selfAssemblyAgents[i].BoundaryDetected() &&
                (_selfAssemblyAgents[i].CurrentState == SelfAssemblyAgent.State.WanderingAndDetecting || _selfAssemblyAgents[i].CurrentState == SelfAssemblyAgent.State.Joined) &&
                    //(_selfAssemblyAgents[i].CurrentState != SelfAssemblyAgent.State.ToJoin && _selfAssemblyAgents[i].CurrentState != SelfAssemblyAgent.State.IndirectToJoin) &&
                    _selfAssemblyAgents[i].SlotAvailable >= 0) {
                    for (int j = i + 1; j < _selfAssemblyAgents.Count; j++) {
                        if (Vector3.Distance(_selfAssemblyAgents[i].Position, _selfAssemblyAgents[j].Position) <= _actualJointDetectionRadius &&
                            _selfAssemblyAgents[j].SlotAvailable >= 0 &&
                            //&& _selfAssemblyAgents[i].GetClosestStructureChord(false) == _selfAssemblyAgents[j].GetClosestStructureChord(false)
                            !_selfAssemblyAgents[i].OtherIsInTheSameStructure(_selfAssemblyAgents[j])) {
                            _selfAssemblyAgents[i].OnDetectCloseAvailableAgent(_selfAssemblyAgents[j], _selfAssemblyAgents[j].SlotAvailable);
                        }
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }
        }
    }

    void CentralizedCheckForSimulation() {
        for (int i = 0; i < _selfAssemblyAgents.Count; i++) {
            if (!_selfAssemblyAgents[i].BoundaryDetected() && 
                (_selfAssemblyAgents[i].CurrentState == SelfAssemblyAgent.State.WanderingAndDetecting || _selfAssemblyAgents[i].CurrentState == SelfAssemblyAgent.State.Joined) &&
                    //(_selfAssemblyAgents[i].CurrentState != SelfAssemblyAgent.State.ToJoin && _selfAssemblyAgents[i].CurrentState != SelfAssemblyAgent.State.IndirectToJoin) &&
                    _selfAssemblyAgents[i].SlotAvailable >= 0) {
                for (int j = i + 1; j < _selfAssemblyAgents.Count; j++) {
                    if (Vector3.Distance(_selfAssemblyAgents[i].Position, _selfAssemblyAgents[j].Position) <= _actualJointDetectionRadius &&
                        _selfAssemblyAgents[j].SlotAvailable >= 0 &&
                        //&& _selfAssemblyAgents[i].GetClosestStructureChord(false) == _selfAssemblyAgents[j].GetClosestStructureChord(false)
                        !_selfAssemblyAgents[i].OtherIsInTheSameStructure(_selfAssemblyAgents[j])) {
                        _selfAssemblyAgents[i].OnDetectCloseAvailableAgent(_selfAssemblyAgents[j], _selfAssemblyAgents[j].SlotAvailable);
                    }
                }
            }
        }
    }

    public static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    UnityEngine.Application.Quit();
#endif
    }

    private void SimulateIterations() { 
        float deltaTime = 0.01f;
        int N = iterations;//iterations
        for (int i = 0; i < N; i++) {
            CentralizedCheckForSimulation();
            UpdateAgents(deltaTime);
            _totalTimeOnSimulation += deltaTime;
        }
        if (OnFinishSimulation != null)
            OnFinishSimulation();

        if (SelfAssemblyExperimentsManager.Instance != null) {
            SelfAssemblyExperimentsManager.Instance.IsFinished = true;
        } else {
            Quit();
        }
    }

    public class StructureData {
        public Dictionary<int, int[]> Items;

        public StructureData(SelfAssemblyAgent agentRef) {
            Items = new Dictionary<int, int[]>();
            var structure = agentRef.GetStructure();
            for (int i = 0; i < structure.Count; i++) {
                var joinedAgents = structure[i].JoinedAgents.Select(joinedAgent => joinedAgent == null ? 0 : joinedAgent.AgentContext.ID);
                Items.Add(structure[i].AgentContext.ID, joinedAgents.ToArray());
            }
        }

        public bool Contains(SelfAssemblyAgent agent) {
            return Items.ContainsKey(agent.AgentContext.ID);
        }

        public override string ToString() {
            var result = "[";
            for (int i = 0; i < Items.Count; i++) {
                result += Items.ElementAt(i).Key + "(" + string.Join("-", Items.ElementAt(i).Value) + ")";
            }
            result += "]";
            return result;
        }
    }

    //private List<List<SelfAssemblyAgent>> _allStructures = new List<List<SelfAssemblyAgent>>();
    public List<StructureData> CollectAllStructures() {
        //_allStructures.Clear();
        List<StructureData> _allStructures = new List<StructureData>(); // need to be a new list for file collection
        bool AgentIsAnyStructure(SelfAssemblyAgent agent) {
            for (int i = 0; i < _allStructures.Count; i++) {
                if (_allStructures[i].Contains(agent)) {
                    return true;
                }
            }
            return false;
        }
        for (int i = 0; i < _selfAssemblyAgents.Count; i++) {
            if (AgentIsAnyStructure(_selfAssemblyAgents[i]))
                continue;
            var structure = new StructureData(_selfAssemblyAgents[i]);
            _allStructures.Add(structure);
        }
        return _allStructures;
    }

    #endregion Simulation Control
}
