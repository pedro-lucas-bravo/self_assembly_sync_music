using UnityEngine;
using System.Collections;

public class AgentTransmitterDevice : MonoBehaviour {

    public PacketTransporter prefabTransporter;
    public float maxCommunicationRange = 15;
    [Tooltip("How long the packet takes to reach the maximun communication range (or radius)")]
    public float communicationLatency = 1;

    public int AgentID { get; set; }
    public Color AgentColor { get; set; } = Color.red;
    public bool ShowVisuals { get; set; } = true;

    public void Send(byte[] pk, bool attachToAgent = true) {
        var newPacket = GameObject.Instantiate<PacketTransporter>(prefabTransporter);
        var communicationRange = maxCommunicationRange;
        if (attachToAgent) {
            newPacket.transform.parent = transform;
            communicationRange = maxCommunicationRange * transform.lossyScale.x;
        } else {
            try {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newPacket.gameObject, gameObject.scene);
            } catch (System.ArgumentException e) {
                //It might happen if scene has been unloaded
            }
        }
        newPacket.transform.rotation = Quaternion.identity;
        newPacket.transform.position = transform.position;
        newPacket.transform.localScale = Vector3.zero;
        newPacket.SetPacketParams(this, communicationRange, communicationLatency, false);//Change to collider if you prefer to try with collisions as triggers for communication
        newPacket.SetApparience(ShowVisuals, AgentColor);
        newPacket.SetPacket(pk);
        newPacket.StartTransmission();
    }

    public void SetApparience(AgentVisualFeedback visuals) { 
        AgentColor = visuals.TargetColor;
    }
    
}
