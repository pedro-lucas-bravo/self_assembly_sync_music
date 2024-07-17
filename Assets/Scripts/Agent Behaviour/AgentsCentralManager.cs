using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AgentsCentralManager : MonoBehaviour
{

    #region Enums

    public enum InitPositionMode {
        Origin,//PLaced in the center of the world (0,0,0)
        Random,//Assign a random position within a radius
        Explicit//Assign a specific position per agent
    }

    #endregion Enums

    public static AgentsCentralManager Instance { get; private set; }

    [Header("Agents")]
    public Agent agentPrefab;//Custom prefab
    public Agent sphereAgentPrefab;
    public Agent cubeAgentPrefab;
    public Agent pyramidAgentPrefab;
    public StigmergicObject stigmergicObjPrefab;
    public GameObject audioSynthesizerPrefab;

    [Header("Environment")]
    public GameObject sphereBoundaryPrefab;
    public GameObject cubeBoundaryPrefab;

    [Header("Global Config")]
    public bool SetDataInUpdate = true;

    public List<Agent> AllAgentsArray { get; private set; }
    public Dictionary<int, Agent> AllAgentsDic { get; } = new Dictionary<int, Agent>();

    public List<StigmergicObject> AllStigmergicObjects { get; private set; } = new List<StigmergicObject>();

    private int _lastStigmerticObjID = 0;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        AllAgentsArray = FindObjectsOfType<Agent>().OrderBy(a => a.ID).ToList();
        for (int i = 0; i < AllAgentsArray.Count; i++) {
            AllAgentsDic.Add(AllAgentsArray[i].ID, AllAgentsArray[i]);
        }
        Camera.main.transparencySortMode = UnityEngine.TransparencySortMode.Orthographic;
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    private void Update() {
        //if(useExternalUpdate)
        //    ExternalCommunicationManager.Instance.SendUpdateRequest(Time.deltaTime);
    }

    #region Agents Instants Management

    public void InstantiateAgents(int size, float scale = 1.0f, bool assignColors = false, float genericRadius = 0, 
        Agent.Shape shape = Agent.Shape.Custom, Agent.MovementMode movMode = Agent.MovementMode.Teleport, 
        InitPositionMode initPosMode = InitPositionMode.Origin, float instantiationRadius = 10.0f, 
        List<Vector3> explicitInitPositions = null, List<int> explicitIds = null) {
        var chosenAgentPrefab = agentPrefab;
        switch (shape) {
            case Agent.Shape.Sphere:
                chosenAgentPrefab = sphereAgentPrefab;
                break;
            case Agent.Shape.Cube:
                chosenAgentPrefab = cubeAgentPrefab;
                break;
            case Agent.Shape.Pyramid:
                chosenAgentPrefab = pyramidAgentPrefab;
                break;
        }
        var lastID = AllAgentsDic.Count <= 0 ? 0 : AllAgentsDic.Max( a => a.Value.ID);
        bool useExplicitIds = explicitIds != null;
        int initIndex = useExplicitIds ? 0 : 1 + lastID;
        int lastIndex = useExplicitIds ? explicitIds.Count - 1: size + lastID;
        for (int i = initIndex; i <= lastIndex; i++) {           
            var id = useExplicitIds ? explicitIds[i] : i;
            if (AllAgentsDic.ContainsKey(id)) continue;
            var newAgent = Instantiate(chosenAgentPrefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newAgent.gameObject, gameObject.scene);
            newAgent.transform.localScale = Vector3.one * scale;
            newAgent.ID = id;
            newAgent.visualFeeback.SetIdLabel(id);
            newAgent.visualFeeback.ShowIdLabel(_showAgentsIdDefault);
            newAgent.movementMode = movMode;
            if(assignColors)
                newAgent.visualFeeback.SetColor(Color.HSVToRGB(((i - 1) / (float)size), 1, 1), false);
            newAgent.visualFeeback.genericRadius.localScale = Vector3.one * genericRadius * 2f / scale;
            switch (initPosMode) {
                case InitPositionMode.Random:
                    newAgent.ReceivedExternalData.LastReceivedPosition = Random.insideUnitSphere * instantiationRadius;
                    break;
                case InitPositionMode.Explicit:
                    if (explicitInitPositions != null && explicitInitPositions.Count >= size) {
                        newAgent.ReceivedExternalData.LastReceivedPosition = explicitInitPositions[useExplicitIds ? i : i - 1 - lastID];
                    } else {
                        newAgent.ReceivedExternalData.LastReceivedPosition = Vector3.zero;
                    }
                    break;
                case InitPositionMode.Origin:
                    newAgent.ReceivedExternalData.LastReceivedPosition = Vector3.zero;
                    break;
            }
            newAgent.ImmediatePosition = newAgent.ReceivedExternalData.LastReceivedPosition;
            newAgent.speed = 0;
            AllAgentsDic.Add(id, newAgent);
            AllAgentsArray.Add(newAgent);
        }
        //useExternalUpdate = true;
    }

    public void RemoveAgents(int size, int shape, int movMode) {
        if (shape < 0 && movMode < 0) {
            for (int i = 0; i < size; i++) {
                if(AllAgentsArray.Count == 0) break;
                var agentToRemove = AllAgentsArray.Last();                
                Destroy(agentToRemove.gameObject);
                AllAgentsDic.Remove(agentToRemove.ID);
                AllAgentsArray.Remove(agentToRemove);
            }
        } else { 
            var agentsToRemove = AllAgentsDic.Values.Where(a => 
                (shape < 0 || a.shape == (Agent.Shape)shape) && (movMode < 0 || a.movementMode == (Agent.MovementMode)movMode)).
                TakeLast(size).ToArray();
            foreach (var agentToRemove in agentsToRemove) {
                Destroy(agentToRemove.gameObject);
                AllAgentsDic.Remove(agentToRemove.ID);
                AllAgentsArray.Remove(agentToRemove);
            }
        }
    }

    public void RemoveById(int id) {
        if (AllAgentsDic.ContainsKey(id)) {
            var agentToRemove = AllAgentsDic[id];
            Destroy(agentToRemove.gameObject);
            AllAgentsDic.Remove(agentToRemove.ID);
            AllAgentsArray.Remove(agentToRemove);
        }
    }

    public void RemoveAllAgents() {
        foreach (var agent in AllAgentsDic) {
            Destroy(agent.Value.gameObject);
        }
        AllAgentsDic.Clear();
        AllAgentsArray.Clear();
        //useExternalUpdate = false;
    }

    #endregion Agents Instants Management

    #region Agents Visual Management

    public void SetAgentsColors(int size, int shape, bool isRandom, List<string> colorsHex) {
        var agents = AllAgentsDic.Values.Where(a => (shape < 0 || a.shape == (Agent.Shape)shape)).Take(size).ToArray();
        for (int i = 0; i < agents.Length; i++) {
            if (isRandom) {
                agents[i].visualFeeback.SetColor(Color.HSVToRGB(Random.value, 1, 1), false);
                continue;
            } else {
                if (i < colorsHex.Count) {
                    UnityEngine.ColorUtility.TryParseHtmlString("#" + colorsHex[i], out Color color);
                    agents[i].visualFeeback.SetColor(color, false);
                }
            }
        }
    }

    public void SetAgentColorById(int id, string colorHex) {
        if (AllAgentsDic.ContainsKey(id)) {
            UnityEngine.ColorUtility.TryParseHtmlString("#" + colorHex, out Color color);
            AllAgentsDic[id].visualFeeback.SetColor(color, false);
        }
    }

    public void SetAgentGenericRadiusById(int id,float radius, float alpha, string colorHex) {
        if (AllAgentsDic.ContainsKey(id)) {
            UnityEngine.ColorUtility.TryParseHtmlString("#" + colorHex, out Color color);
            color.a = alpha;
            AllAgentsDic[id].visualFeeback.SetGenericRadius(radius, color);
        }
    }

    #endregion Agents Visual Management

    #region Audio Features

    public void AddSoundSynthesizers(int size, int shape, int movMode) { 
        if (AllAgentsArray.Count == 0) return;
        if (shape < 0 && movMode < 0) {
            for (int i = 0; i < size; i++) {
                if(i >= AllAgentsArray.Count) break;
                AddSoundSynthesizer(AllAgentsArray[i]);
            }
        } else {
            var filteredAgents = AllAgentsDic.Values.Where(a =>
                (shape < 0 || a.shape == (Agent.Shape)shape) && (movMode < 0 || a.movementMode == (Agent.MovementMode)movMode)).
                TakeLast(size).ToArray();
            foreach (var agent in filteredAgents) {
                AddSoundSynthesizer(agent);
            }
        }
    }

    public void RemoveSoundSynthesizers(int size, int shape, int movMode) {
        if (AllAgentsArray.Count == 0) return;
        if (shape < 0 && movMode < 0) {
            for (int i = 0; i < size; i++) {
                if (i >= AllAgentsArray.Count) break;
                RemoveSoundSynthesizer(AllAgentsArray[i]);
            }
        } else {
            var filteredAgents = AllAgentsDic.Values.Where(a =>
                           (shape < 0 || a.shape == (Agent.Shape)shape) && (movMode < 0 || a.movementMode == (Agent.MovementMode)movMode)).
                TakeLast(size).ToArray();
            foreach (var agent in filteredAgents) {
                RemoveSoundSynthesizer(agent);
            }
        }
    }

    public void AddSoundSynthesizerById(int id) {
        if (AllAgentsDic.ContainsKey(id)) {
            AddSoundSynthesizer(AllAgentsDic[id]);
        }
    }

    public void RemoveSoundSynthesizerById(int id) {
        if (AllAgentsDic.ContainsKey(id)) {
            RemoveSoundSynthesizer(AllAgentsDic[id]);
        }
    }

    private void AddSoundSynthesizer(Agent agent) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe != null) return;
        var newSynth = Instantiate(audioSynthesizerPrefab, agent.transform);
        newSynth.transform.localPosition = Vector3.zero;      
        agent.visualFeeback.ChangeDefaultAlpha(0.5f);
    }

    private void RemoveSoundSynthesizer(Agent agent) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe != null) {
            Destroy(synthe.gameObject);
        }
        agent.visualFeeback.ChangeDefaultAlpha(1f);
    }

    private BasicSynthesizer GetBasicSoundSynthesizer(Agent agent) {
        return agent.GetComponentInChildren<BasicSynthesizer>();
    }

    public void SetSynthesizerParameterValue(Agent agent,int bank, int paramId, float value, bool showVisualFeedback = false) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe == null || (synthe != null && synthe.bank != bank)) return;
        synthe.SetValue(paramId, value);
        if (showVisualFeedback) {
            ShowSyntheVisualFeedback(synthe);
        }
    }

    public void SetSynthesizerParameterMidi(Agent agent, int bank, int paramId, int midiValue, bool showVisualFeedback = false) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe == null || (synthe != null && synthe.bank != bank)) return;
        synthe.SetCC(paramId, midiValue);
        if (showVisualFeedback) {
            ShowSyntheVisualFeedback(synthe);
        }
    }

    public void SetSynthesizerNote(Agent agent, int bank, int midiNote, int velocity, bool showVisualFeedback = false) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe == null || (synthe != null && synthe.bank != bank)) return;
        if (velocity != 0)
            synthe.Play(midiNote, velocity);
        else
            synthe.Stop(midiNote);
        if (showVisualFeedback) {
            ShowSyntheVisualFeedback(synthe);
        }
    }

    private void ShowSyntheVisualFeedback(BasicSynthesizer synthe) { 
        var colorSignaling = synthe.AudioSource.GetComponentInChildren<ColorSignaling>();
        if (colorSignaling != null) {
            colorSignaling.Do();
        }
    }

    public void SetFlushToSynthesizer(Agent agent, int bank, bool showVisualFeedback = false) {
        var synthe = GetBasicSoundSynthesizer(agent);
        if (synthe == null || (synthe != null && synthe.bank != bank)) return;
        synthe.FlushNotes();
        if (showVisualFeedback) {
            ShowSyntheVisualFeedback(synthe);
        }
    }

    #endregion Audio Features

    #region Stigmergic Objects Management

    public void RemoveAllStimergicObjects() {
        foreach (var stObj in AllStigmergicObjects) {
            Destroy(stObj.gameObject);
        }
        AllStigmergicObjects.Clear();
        _lastStigmerticObjID = 0;
    }

    public void InstantiateStimergicObjects(int size, StigmergicObject.Type type, float scale = 1.0f, float instantiationRadius = 10.0f) {        
        for (int i = 0; i < size; i++) {
            InstantiateStimergicObject(0, type, Random.insideUnitSphere * instantiationRadius, 0, scale);// Random position in a sphere of radious 10
        }
    }

    public void InstantiateStimergicObject(int ID, StigmergicObject.Type type, Vector3 initPosition, float genericRadius = 1.0f, float scale = 1.0f) {
        var id = ID == 0 ? _lastStigmerticObjID + 1 : ID;        
        var newStObj = Instantiate(stigmergicObjPrefab);
        newStObj.ID = id;
        newStObj.transform.position = initPosition;
        newStObj.transform.localScale = Vector3.one * scale;
        newStObj.genericRadius.localScale = Vector3.one * genericRadius * 2f / scale;
        newStObj.SetAndPrepareType(type);
        AllStigmergicObjects.Add(newStObj);  
        _lastStigmerticObjID = Mathf.Max(id, _lastStigmerticObjID);
        Debug.Log("Add");
    }

    public void DestroyStigmergicObject(int id) {
        var stObj = AllStigmergicObjects.Find(x => x.ID == id);
        if (stObj != null) {
            AllStigmergicObjects.Remove(stObj);
            Destroy(stObj.gameObject);
        }
    }

    public void AddStimergicObject(StigmergicObject obj) {
        if(!AllStigmergicObjects.Contains(obj))
            AllStigmergicObjects.Add(obj);
    }

    private void RemoveStimergicObject(StigmergicObject obj) {
        if (AllStigmergicObjects.Contains(obj))
            AllStigmergicObjects.Remove(obj);
    }

    public void SetStigmergicObjectPosition(int id, Vector3 position) {
        var stObj = AllStigmergicObjects.Find(x => x.ID == id);
        if (stObj != null) {
            stObj.Position = position;
        }
    }

    #endregion Stigmergic Objects Management

    #region OSC Data Receiver Events

    public void OnAddStigmergicObjet(List<object> values) {
        var id = (int)values[0];
        var type = (StigmergicObject.Type)values[1];
        var position = new Vector3(
                               System.Convert.ToInt32(values[2]),
                               System.Convert.ToInt32(values[4]),
                               System.Convert.ToInt32(values[3])) * 0.001f;
        var genericRadius = 0.5f;
        if (values.Count > 5)
            genericRadius = System.Convert.ToInt32(values[5]) * 0.001f;

        var scale = 1f;
        if (values.Count > 6)
            scale = System.Convert.ToInt32(values[6]) * 0.001f;
        InstantiateStimergicObject(id, type, position, genericRadius, scale);
    }

    public void OnRemoveStigmergicObjet(List<object> values) {
        var id = (int)values[0];
        DestroyStigmergicObject(id);
        Debug.Log("Remove");
    }

    public System.Action<int, int, byte[]> OnReceivedPacket { get; set; } //receiverID, senderID, data

    public void ReceivedPacket(int receiverId, int senderId, byte[] packet) {
        OnReceivedPacket?.Invoke(receiverId, senderId, packet);
    }

    #endregion

    #region User Interaction

    public System.Action<Agent> OnTouch1 { get; set; }
    public System.Action<Agent> OnTouch2 { get; set; }
    public System.Action<Agent> OnRelease1 { get; set; }
    public System.Action<Agent> OnRelease2 { get; set; }
    public System.Action<Agent> OnStartGrab { get; set; }
    public System.Action<Agent> OnEndGrab { get; set; }

    public void ApplyInteractiveAction(Agent agent, Agent.InteracionType interacionType) {
        switch (interacionType) {
            case Agent.InteracionType.Touch1:
                OnTouch1?.Invoke(agent);
                break;
            case Agent.InteracionType.Touch2:
                OnTouch2?.Invoke(agent);
                break;
            case Agent.InteracionType.Release1:
                OnRelease1?.Invoke(agent);
                break;
            case Agent.InteracionType.Release2:
                OnRelease2?.Invoke(agent);
                break;
            case Agent.InteracionType.StartGrab:
                OnStartGrab?.Invoke(agent);
                break;
            case Agent.InteracionType.EndGrab:
                OnEndGrab?.Invoke(agent);
                break;
        }
    }

    bool _showAgentsIdDefault = false;
    public void ShowAgentsId(bool show) {
        for (int i = 0; i < AllAgentsArray.Count; i++) {
            AllAgentsArray[i].visualFeeback.ShowIdLabel(show);            
        }
        _showAgentsIdDefault = show;
    }

    #endregion

    #region Boundary Management

    private Dictionary<int,GameObject> _boundaries = new Dictionary<int, GameObject>();

    public void AddBoundary(int id, int shape, Vector3 position, Vector3 scale, float alpha, string colorHex) {
        if (_boundaries.ContainsKey(id)) {
            Destroy(_boundaries[id]);
            _boundaries.Remove(id);
        }
        GameObject boundary = null;
        switch (shape) {
            case 0:
                boundary = Instantiate(sphereBoundaryPrefab);
                break;
            case 1:
                boundary = Instantiate(cubeBoundaryPrefab);
                break;
        }
        boundary.transform.position = position;
        boundary.transform.localScale = scale;
        _boundaries.Add(id, boundary);
        if((alpha >= 0 && alpha <= 1) || string.IsNullOrEmpty(colorHex))            
            SetBoundaryColor(id, alpha, colorHex);
    }

    public void RemoveBoundary(int id) {
        if (_boundaries.ContainsKey(id)) {
            Destroy(_boundaries[id]);
            _boundaries.Remove(id);
        }
    }

    public void SetBoundaryPosition(int id, Vector3 position) {
        if (_boundaries.ContainsKey(id)) {
            _boundaries[id].transform.position = position;
        }
    }

    public void SetBoundaryScale(int id, Vector3 scale) {
        if (_boundaries.ContainsKey(id)) {
            _boundaries[id].transform.localScale = scale;
        }
    }

    public int GetBoundaryType(int id) {
        if (_boundaries.ContainsKey(id)) {
            var boundary = _boundaries[id];
            if (boundary.name.Contains("Sphere"))
                return 0;
            else if (boundary.name.Contains("Cube"))
                return 1;
        }
        return -1;
    }

    public Vector3 GetBoundaryScale(int id) {
        if (_boundaries.ContainsKey(id)) {
            return _boundaries[id].transform.localScale;
        }
        return Vector3.zero;
    }

    public void SetBoundaryColor(int id, float alpha, string colorHex) {
        if (_boundaries.ContainsKey(id)) {            
            var material = _boundaries[id].GetComponent<Renderer>().material;
            Color currentColor = material.color;
            if (!string.IsNullOrEmpty(colorHex))
                UnityEngine.ColorUtility.TryParseHtmlString("#" + colorHex, out currentColor);

            if (alpha >= 0 && alpha <= 1)
                currentColor.a = alpha;
            else
                currentColor.a = material.color.a;
            material.color = currentColor;
        }
    }

    public System.Action<int, int> OnEnterBoundary { get; set; } //agentId, boundaryId
    public System.Action<int, int> OnExitBoundary { get; set; } //agentId, boundaryId

    public void EnterBoundary(Agent agent, GameObject boundary) {
        var agentId = agent.ID;
        var boundaryId = _boundaries.FirstOrDefault(x => x.Value == boundary).Key;
        OnEnterBoundary?.Invoke(agentId, boundaryId);
    }

    public void ExitBoundary(Agent agent, GameObject boundary) {
        var agentId = agent.ID;
        var boundaryId = _boundaries.FirstOrDefault(x => x.Value == boundary).Key;
        OnExitBoundary?.Invoke(agentId, boundaryId);
    }

    #endregion Boundary Management
}
