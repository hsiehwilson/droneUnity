using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICameraManager
{
    public bool GetCameraMovementPriority();

    public void FollowDrone(GameObject drone);
}