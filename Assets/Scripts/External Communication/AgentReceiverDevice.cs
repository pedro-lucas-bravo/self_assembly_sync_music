using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentReceiverDevice : MonoBehaviour
{

    public Action<int, byte[]> OnReceivePackage { get; set; } //senderID, data

    AgentTransmitterDevice _localTransmitter;

    private void Awake() {
        _localTransmitter = transform.GetComponentInChildren<AgentTransmitterDevice>();
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("AgentCommunication")) {
            var packet = other.GetComponent<PacketTransporter>();
            if (!packet.EqualTRansmitter(_localTransmitter)) {
                if(OnReceivePackage != null) OnReceivePackage(packet.AgentID, packet.GetPacket());
            }
        }
    }
}
