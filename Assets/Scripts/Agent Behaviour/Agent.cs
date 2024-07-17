using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;

public class Agent : MonoBehaviour{

    public enum Shape {        
        Sphere = 0,
        Cube = 1,
        Pyramid = 2,
        Custom = 3

    }

    public enum MovementMode { 
        Teleport,
        FollowTarget,
        Velocity
    }

    public class ReceivedExtData {
        public object LockPosition = new object();
        public Vector3 LastReceivedPosition;
        public bool SetOutline;
        public float LastReceivedOutlineWidth;
        public Color LastReceivedOutlineColor;
    }

    #region Components

    [SerializeField]
    private int _ID;

    [Header("Components")]
    public AgentTransmitterDevice Transmitter;
    public AgentReceiverDevice Receiver;
    public bool testTransmission;
    public AgentVisualFeedback visualFeeback;

    [Header("Settings")]
    public Shape shape = Shape.Custom;//This is set in teh editor per prefab
    public MovementMode movementMode = MovementMode.Teleport;
    public float speed = 1f;
    public Vector3 direction = Vector3.forward;
    #endregion


    public Vector3 Position {
        get => _trans.position;
    }

    public Vector3 TargetPosition { get; set; }

    public Vector3 ImmediatePosition {
        set {
            ReceivedExternalData.LastReceivedPosition = value;
            _trans.position = value;
            TargetPosition = value;
        }
    }

    private Transform _trans;
    private Vector3 _lastPosition;
    public ReceivedExtData ReceivedExternalData { get;} = new ReceivedExtData();

    public int ID {
        get => _ID;
        set {
            _ID = value;
            if (Transmitter != null)
                Transmitter.AgentID = value;
        }
    }

    #region Unity Messages

    private void Awake() {
        _trans = transform;
        visualFeeback = GetComponent<AgentVisualFeedback>();       
    }

    private void Start() {
        if (Receiver != null)
            Receiver.OnReceivePackage += OnReceivePackageForExternal;
    }

    private void OnDestroy() {
        if (Receiver != null)
            Receiver.OnReceivePackage -= OnReceivePackageForExternal;
    }

    private void Update() {
        if (Transmitter != null) {
            Transmitter.SetApparience(visualFeeback);
        }
        if (Transmitter != null && testTransmission && Input.GetKeyDown(KeyCode.A)) {
            // Send message in one transmission cycle
            Transmitter.Send(Encoding.ASCII.GetBytes("from A" + ID));// is recommended to identify what agent are you (ID) plus any message you want to convey AND TODO: optimize so that you can send a byte array directly
        }
        if (AgentsCentralManager.Instance != null && 
            AgentsCentralManager.Instance.SetDataInUpdate) {            
            SetReceivedData();
        } else { 
            //Normal Update
        }        
    }

    private void FixedUpdate() {
        switch (movementMode) {
            case MovementMode.Teleport:
                //Control externally or internally when InmmediatePosition is set
                break;
            case MovementMode.FollowTarget:
                _trans.position = Vector3.MoveTowards(_trans.position, TargetPosition, speed * Time.fixedDeltaTime);
                break;
            case MovementMode.Velocity:
                _trans.position += direction * speed * Time.fixedDeltaTime;
                break;
        }
        if (shape == Shape.Pyramid) { 
            var dir = _trans.position - _lastPosition;//Direct the pyramid
            if (dir != Vector3.zero)
                _trans.forward = dir;
            _lastPosition = _trans.position;
        }
    }

    #endregion

    #region Events

    private void OnReceivePackageForExternal(int senderID, byte[] pkg) {
        //Debug.Log("In Agent " + ID +" receive "+pkg);
        if(AgentsCentralManager.Instance != null)
            AgentsCentralManager.Instance.ReceivedPacket(ID, senderID, pkg);        
    }

    #endregion

    private void SetReceivedData() {
        lock (ReceivedExternalData.LockPosition) {
            _trans.position = ReceivedExternalData.LastReceivedPosition;
            if(ReceivedExternalData.SetOutline)
                visualFeeback.SetOutline(ReceivedExternalData.LastReceivedOutlineColor, ReceivedExternalData.LastReceivedOutlineWidth);
        }
    }

    #region Interaction Features

    public enum InteracionType {
        None,
        Touch1,
        Release1,
        Touch2,
        Release2,
        StartGrab,
        EndGrab
    }

    public void SetInteraction(InteracionType type) {
        if (AgentsCentralManager.Instance != null) {
            AgentsCentralManager.Instance.ApplyInteractiveAction(this, type);
        }
    }

    #endregion

    #region Unity Messages Receiver

    public void OnTouch1Receiver() {
        SetInteraction(InteracionType.Touch1);
    }

    public void OnRelease1Receiver() {
        SetInteraction(InteracionType.Release1);
    }

    public void OnTouch2Receiver() {
        SetInteraction(InteracionType.Touch2);
    }

    public void OnRelease2Receiver() {
        SetInteraction(InteracionType.Release2);
    }

    public void OnStartGrabReceiver() {
        SetInteraction(InteracionType.StartGrab);
    }

    public void OnEndGrabReceiver() {
        SetInteraction(InteracionType.EndGrab);
    }

    #endregion Unity Messages Receiver

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Boundary"))
            AgentsCentralManager.Instance.EnterBoundary(this, other.gameObject);
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Boundary"))
            AgentsCentralManager.Instance.ExitBoundary(this, other.gameObject);
    }
}
