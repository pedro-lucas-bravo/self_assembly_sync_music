using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public FirstPersonCamera cameraController;
    public UserInteractor grabber;

    private void Awake() {
        if(grabber == null)
            grabber = GetComponent<UserInteractor>();
    }

    private void Update() {
        cameraController.LockToDontMove = grabber.IsDragging;
    }
}
