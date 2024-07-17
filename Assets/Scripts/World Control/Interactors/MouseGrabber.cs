using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseGrabber : UserInteractor
{
    public string TargetTag;
    public Transform visualFeedback;

    public override bool IsDragging {
        get  =>   _cubeGrabber.IsDragging;
    }

    private CubeGrabber _cubeGrabber;

    private void Awake() {
        _cubeGrabber = new CubeGrabber(TargetTag, visualFeedback);
    }


    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            var caughtCube = _cubeGrabber.StartAction();
            if (caughtCube != null) {
                caughtCube.SendMessage("OnStartGrabReceiver", SendMessageOptions.DontRequireReceiver);
            }
        }

        if (Input.GetMouseButtonUp(0)) {
            var caughtCube = _cubeGrabber.EndAction();
            if (caughtCube != null) {
                caughtCube.SendMessage("OnEndGrabReceiver", SendMessageOptions.DontRequireReceiver);
            }
        }
        _cubeGrabber.UpdateAction();
    }
}
