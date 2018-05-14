using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.WSA.Input;
using MultiWorldAnchorSystem;

// MultiWorldAnchorSystemのサンプル
// Airtapで再設置処理開始
public class MultiWorldAnchorSystemSample : MonoBehaviour {
    public GameObject[] CenterAnchors;

    private ModelPositionManager modelPositionManager;
    // Use this for initialization
    void Start () {
        modelPositionManager = GetComponent<ModelPositionManager>();
        modelPositionManager.InitModelPositionManager(CenterAnchors);

        InteractionManager.InteractionSourceReleased += (obj) =>
        {
            modelPositionManager.StartSetting();
            for (int i = 0; i < CenterAnchors.Length; i++)
            {
                modelPositionManager.TrackingImageTargetEvent(i, CenterAnchors[i].transform.position, CenterAnchors[i].transform.rotation);
            }
        };
    }
}
