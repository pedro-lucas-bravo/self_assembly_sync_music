using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;

public class Agent : MonoBehaviour{

    #region Components

    [SerializeField]
    private int _ID;

    [Header("Components")]
    public AgentVisualFeedback visualFeeback;

    #endregion


    public int ID {
        get => _ID;
        set {
            _ID = value;
        }
    }

    #region Unity Messages

    private void Awake() {
        visualFeeback = GetComponent<AgentVisualFeedback>();       
    }

    #endregion

}
