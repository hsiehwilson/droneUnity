using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// potentially need change Project Settings for Script Execution Order with Camera Manager after both AutoCameraController and DroneCameraController
// class to switch between autoCam or droneCam, 
public class CameraManager : MonoBehaviour
{
    private ICameraManager _cameraManager;
    
    public AutoCameraController autoCameraController;
    public DroneCameraController droneCameraController;
    public bool autoCameraActive;
    
    // fully disable the other camera's logic when rendering the other
    private void Start()
    {
        if (autoCameraActive == false)
        {
            autoCameraController.gameObject.SetActive(false);
            droneCameraController.gameObject.SetActive(true);
            _cameraManager = droneCameraController;
        }
        else
        {
            autoCameraController.gameObject.SetActive(true);
            droneCameraController.gameObject.SetActive(false);
            _cameraManager = autoCameraController;
        }
    }

    public bool GetCameraMovementPriority()
    {
        return _cameraManager.GetCameraMovementPriority();
    }

    public bool GetAutoCameraActive()
    {
        return autoCameraActive;
    }

    public void FollowDrone(GameObject drone)
    {
        _cameraManager.FollowDrone(drone);
    }
}