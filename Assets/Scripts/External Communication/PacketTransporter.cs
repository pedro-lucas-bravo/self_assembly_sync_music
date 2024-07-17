using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketTransporter : MonoBehaviour
{
    private byte[] _packet;
    private float _maxCommunicationRange = 15;
    private float _communicationLatency = 1;
    private float _timerCommunication;
    private Transform _communicationRangeTrans;
    private bool _useCollider;
    private List<int> _usedId = new List<int>();

    private AgentTransmitterDevice _transmitter;

    public int AgentID => _transmitter.AgentID;

    private void Awake() {
        _communicationRangeTrans = transform;
        enabled = false;
    }

    public void SetPacketParams(AgentTransmitterDevice transmitter, float maxCommunicationRange, float communicationLatency, bool useCollider) {
        _transmitter = transmitter;
        _maxCommunicationRange = maxCommunicationRange;
        _communicationLatency = communicationLatency;
        _useCollider = useCollider;
        if (!useCollider) {
            DestroyImmediate(gameObject.GetComponent<Collider>());
        }
    }

    public void SetApparience(bool showVisuals, Color bubbleColor) {
        if (!showVisuals) {
            DestroyImmediate(gameObject.GetComponent<Renderer>());
            return;
        }
        bubbleColor.a = 0.17f;//make a little transparent
        var renderer = gameObject.GetComponent<Renderer>();
        renderer.enabled = true;
        renderer.material.color = bubbleColor;
    }

    public void StartTransmission() {
        enabled = true;             
    }

    private void FixedUpdate() {
        var deltaTime = Time.fixedDeltaTime;
        _timerCommunication += deltaTime;
        if (_timerCommunication <= _communicationLatency) {
            var currentDisplacement = _maxCommunicationRange * (_timerCommunication / _communicationLatency) * Vector3.one;
            _communicationRangeTrans.localScale = currentDisplacement * 2f;
            if (!_useCollider)
                SendPacketByDistance(currentDisplacement.x);            
        } else {
            enabled = false;
            if (!_useCollider)
                SendPacketByDistance(_maxCommunicationRange);
            GameObject.Destroy(gameObject);            
        }        
    }

    private void SendPacketByDistance( float currentDistance) {
        if(AgentsCentralManager.Instance == null) return;
        var allAgents = AgentsCentralManager.Instance.AllAgentsArray;
        for (int i = 0; i < allAgents.Count; i++) {
            var id = allAgents[i].ID;
            if (_usedId.Contains(id)) continue;
            if (allAgents[i].Transmitter == _transmitter) {
                _usedId.Add(id);
                continue;
            }
            var distanceAgentTransmitter = Vector3.Distance(allAgents[i].Position, _communicationRangeTrans.position);
            if (currentDistance >= distanceAgentTransmitter) {
                if (allAgents[i].Receiver.OnReceivePackage != null) {
                    allAgents[i].Receiver.OnReceivePackage(AgentID, GetPacket());
                    _usedId.Add(id);
                }
            }
        }
    }

    public byte[] GetPacket() {
        return _packet;
    }

    public void SetPacket(byte[] pkg) {
        _packet = pkg;
    }

    public bool EqualTRansmitter(AgentTransmitterDevice other) {
        return _transmitter == other;
    }
}
