using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StigmergicObject : MonoBehaviour{

    public enum Type { Attractor, Repeller}
    

    public int ID;
    public Type type = Type.Attractor;
    public Material attractorMaterial;
    public Material repellerMaterial;
    public Transform genericRadius;

    //Position to be send thourg OSC messages (different thread)
    public Vector3 Position {
        get => _trans.position;
        set { _trans.position = value; }
    }

    private Transform _trans;
    private Renderer _renderer;

    private void Awake() {
        _trans = transform;
        _renderer = GetComponent<Renderer>();
    }

    private void Start() {
        AgentsCentralManager.Instance.AddStimergicObject(this);
    }    

    public void SetAndPrepareType(Type type) {
        this.type = type;
        switch (type) {
            case Type.Attractor:
                _renderer.material = new Material(attractorMaterial);
                break;
            case Type.Repeller:
                _renderer.material = new Material(repellerMaterial);
                break;
        }
    }
}
