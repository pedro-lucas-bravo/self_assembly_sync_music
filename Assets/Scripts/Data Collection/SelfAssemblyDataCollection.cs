using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Threading.Tasks;

public class SelfAssemblyDataCollection : MonoBehaviour
{
    public SelfAssemblyController selfAssemblyController;

    public string dataPath = "C:/data/";
    public bool saveData = true;

    private List<(int, float)> _allSyncData;
    private List<(float, int, int, List<SelfAssemblyController.StructureData>)> _allStructures;//(timestamp, agent_id_ref,event:0 join, 1 detach, structure)

    private void Start() {
        _allSyncData = new List<(int, float)>();
        _allStructures = new List<(float, int, int, List<SelfAssemblyController.StructureData>)>();
        foreach (var agent in selfAssemblyController.Agents) {
            agent.OnJoin += OnJoin;
            agent.OnDetach += OnDetach;
            agent.OnPhasorClimax += OnPhasorClimax;
        }
        selfAssemblyController.OnFinishSimulation += OnFinishSimulation;
    }

    private void OnFinishSimulation() {
        if (saveData)
            SaveAll();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            if (saveData)
                SaveAll();
        }
    }

    private void OnDestroy() {
        if (selfAssemblyController != null) {
            foreach (var agent in selfAssemblyController.Agents) {
                agent.OnJoin -= OnJoin;
                agent.OnDetach -= OnDetach;
                agent.OnPhasorClimax -= OnPhasorClimax;
            }
            selfAssemblyController.OnFinishSimulation -= OnFinishSimulation;
        }
    }

    private void OnJoin(float timestamp, SelfAssemblyAgent agent) {
       AddStructure(timestamp, agent, 0);
    }

    private void OnDetach(float timestamp, SelfAssemblyAgent agent) {
        AddStructure(timestamp, agent, 1);
    }

    private float _lastStructTime = -1f;
    private void AddStructure(float timestamp, SelfAssemblyAgent agent, int eventType) {
        if (saveData) {//Keep the last structure if the timestamp is the same
            if (!Mathf.Approximately(_lastStructTime, timestamp))
                _allStructures.Add((timestamp, agent.AgentContext.ID, eventType, selfAssemblyController.CollectAllStructures()));
            else
                _allStructures[_allStructures.Count - 1] = (timestamp, agent.AgentContext.ID, eventType, selfAssemblyController.CollectAllStructures());
            _lastStructTime = timestamp;
        }
    }

    private void OnPhasorClimax(int agentID, float timestamp) { 
        if (saveData)
            _allSyncData.Add((agentID, timestamp));
    }


    private string GetFileName() {
        string currentDateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        var fileName = currentDateTime +
            "-N-" + selfAssemblyController.Agents.Count +
            "-ls-" + selfAssemblyController.minAgentSpeed +
            "-hs-" + selfAssemblyController.maxAgentSpeed +
            "-wt-" + selfAssemblyController.onlyWanderingTime +
            "-jt-" + selfAssemblyController.maxJoinTime +
            "-jr-" + selfAssemblyController.agentJoinRadiusFactor +
            "-js-" + selfAssemblyController.maxJointsSize +
            "-rb-" + selfAssemblyController.radiusBoundary +
            "-f-" + selfAssemblyController.Frequency;
        return fileName;
    } 

    private void SaveSync(string fileName) {
        bool trySaving = true;
        string exception = "";
        while (trySaving) {
            try {
                string header = "id,time\n";
                string data = "";
                for (int i = 0; i < _allSyncData.Count; i++) {
                    data += _allSyncData[i].Item1 + "," + _allSyncData[i].Item2 + "\n";
                }
                System.IO.File.WriteAllText(fileName + ".csv", header + data);
                trySaving = false;
            } catch (Exception e) {
                //Writing the message in a log file
                exception += e.Message + "\n";
                trySaving = true;
                System.IO.File.WriteAllText(fileName + "_EXC.logerror", exception);
            }
        }
    }

    // Implementing a simplified version of SaveStructures here:
    private void SaveStructures(string fileName) {
        try {
            string[] dataRows = new string[_allStructures.Count];

            Parallel.For(0, _allStructures.Count, i => {
                var data = "";
                //var Nagents = 0;
                for (int j = 0; j < _allStructures[i].Item4.Count; j++) {
                    var structure = _allStructures[i].Item4[j];
                    data = data + _allStructures[i].Item1 + "," + 
                    _allStructures[i].Item2 + ","+
                    _allStructures[i].Item3 + "," +
                    structure.Items.Count + "," + structure.ToString() + "\n";
                    //Nagents += structure.Items.Count;
                }
                dataRows[i] = data; //+ "N =" + Nagents + "\n";
            });

            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.Append("time,agent_ref,event,N,structure\n");
            for (int i = 0; i < dataRows.Length; i++) {
                csvBuilder.Append(dataRows[i]);
            }

            System.IO.File.WriteAllText(fileName + ".csv", csvBuilder.ToString());
        } catch (Exception e) {
            //Writing the message in a log file
            System.IO.File.WriteAllText(fileName + "_EXC.logerror", e.Message + "\n");
        }
    }

    private void SaveAll() { 
        var fileName = GetFileName();
        SaveSync( dataPath + "SYNC-" + fileName);
        SaveStructures(dataPath + "STRUCT-" + fileName);
    }
}
