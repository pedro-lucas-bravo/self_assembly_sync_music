using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AgentsCentralManager : MonoBehaviour
{

    public static AgentsCentralManager Instance { get; private set; }

    [Header("Agents")]
    public Agent agentPrefab;//Custom prefab

    public List<Agent> AllAgentsArray { get; private set; }
    public Dictionary<int, Agent> AllAgentsDic { get; } = new Dictionary<int, Agent>();

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

    #region Agents Instants Management

    public void InstantiateAgents(int size, float scale = 1.0f) {
        var chosenAgentPrefab = agentPrefab;
        for (int i = 1; i <= size; i++) {
            var id = i;
            if (AllAgentsDic.ContainsKey(id)) continue;
            var newAgent = Instantiate(chosenAgentPrefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newAgent.gameObject, gameObject.scene);
            newAgent.transform.localScale = Vector3.one * scale;
            newAgent.ID = id;
            newAgent.visualFeeback.SetIdLabel(id);
            newAgent.visualFeeback.ShowIdLabel(_showAgentsIdDefault);
            AllAgentsDic.Add(id, newAgent);
            AllAgentsArray.Add(newAgent);
        }
    }

    bool _showAgentsIdDefault = false;
    public void ShowAgentsId(bool show) {
        for (int i = 0; i < AllAgentsArray.Count; i++) {
            AllAgentsArray[i].visualFeeback.ShowIdLabel(show);
        }
        _showAgentsIdDefault = show;
    }

    #endregion Agents Instants Management

}
