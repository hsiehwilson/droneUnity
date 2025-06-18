using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine;

// change Project Settings for Script Execution Order with Drone Camera Controller after the Default Time
public class DroneCameraController : MonoBehaviour, ICameraManager
{   
    public DroneManager droneManager;
    public float camYOffset;
    
    private bool _camMovementPriority = false;  
    private float _lerpVal = 0.0f;
    private float rotationTimer = 0f;
    private float rotationDuration = Constants.RotationDurationDroneCam; // how long to rotate
    private Quaternion _startRotation;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _chargedDroneStartPos; // used to detect if droneCam finished following a drone inside the simulation switching to a new one
    private int _trackDroneId;
    private Vector3 _trackedDronePos; // the position of the drone that the cam is built on (used for most of the logic check)
    private Tuple<int, Vector3, Vector3, Vector3> _currDroneInfo; // droneId, startPos, endPos

    private Vector3 _currForward;
    private Vector3 _targetForward;

    private Transform _currentDrone;
    private Tuple<int, GameObject> _currentDroneObject;
    
    private int _currentFollowedDroneId = -999; 
    private bool _isSwitching = false;
    public bool GetCameraMovementPriority()
    {
        return _camMovementPriority;
    }
    // Start is called before the first frame update
    void Start()
    {
        _currDroneInfo = droneManager.GetDroneInfo(997);
        // starts from the charged drone so only update this once
        
        _chargedDroneStartPos = _currDroneInfo.Item2;
        _currentDroneObject = droneManager.GetDroneObject(_currDroneInfo.Item1);
        FollowDrone(_currentDroneObject.Item2);
        // _startRotation = transform.rotation;
        // _assignFields();
        // _startPos = _currDroneInfo.Item2;
        // _endPos = Vector3.zero;
        // Debug.Log(_endPos);
        // _FinishRotateCam();
    }

    // Update is called once per frame
    void Update()
    {   
        // entered the simulation
        
        if (_isSwitching == false && _currentFollowedDroneId != -1 && (transform.parent.position - Vector3.zero).sqrMagnitude < 0.1f)
        {   
            // Debug.Log("entered");
            _isSwitching = true;
            StartCoroutine(SwitchFollowDelayed(-1));
            // _currentFollowedDroneId = -1;
        }
        // Debug.Log(transform.parent.position);
        // if ((transform.parent.position - Vector3.zero).sqrMagnitude < 0.0001f && droneManager.switchDroneFollow)
        // {   
        //     // Debug.Log(transform.parent.position);
        //     _currDroneInfo = droneManager.GetDroneInfo(-1);
        //     _currentDroneObject = droneManager.GetDroneObject(-1);
        //     FollowDrone(_currentDroneObject.Item2);
        //     droneManager.switchDroneFollow = false;
        // }
        else if (_isSwitching == false && _currentFollowedDroneId != 997 && (transform.parent.position - _chargedDroneStartPos).sqrMagnitude < 0.1f)
        {   
            _isSwitching = true;
            StartCoroutine(SwitchFollowDelayed(997));
            // _currentFollowedDroneId = 997;
        }
        // else
        // {
        //     _currDroneObject = droneManager.GetDroneInfo();
        // }
        /*
        Debug.Log(_camMovementPriority);
        if (!droneManager.GetIsSimulating())
        {
            // if (_trackedDronePos == Vector3.zero)
            // {
            //     _currDroneInfo = droneManager.GetDroneInfo(-1);
            // }
            // // ended at the charging station position 
            // else if (_trackedDronePos == _chargedDroneStartPos)
            // {
            //     _currDroneInfo = droneManager.GetDroneInfo(997);
            // }
            // else
            // {
            //     _currDroneInfo = droneManager.GetDroneInfo(_trackDroneId);
            // }
            // _assignFields();
            //
            // Debug.Log("rotating camera");
            // do the rotation 
            _FinishRotateCam();
        }
        // else if (droneManager.GetDroneRotatePriority() && !_camMovementPriority)
        // {
        //     
        // }
        // enter this condition if new droneInfo is required (camera moved to the end position)
        else if (_InterpolateToEndPosition())
        {   
            _lerpVal = 0.0f; // reset lerp for next cycle
            // ended at start of simulation
            if (_trackedDronePos == Vector3.zero)
            {
                _currDroneInfo = droneManager.GetDroneInfo(-1);
            }
            // ended at the charging station position 
            else if (_trackedDronePos == _chargedDroneStartPos)
            {
                _currDroneInfo = droneManager.GetDroneInfo(997);
            }
            else
            {
                _currDroneInfo = droneManager.GetDroneInfo(_trackDroneId);
            }
            _assignFields();
        }
        */
    }
    
    private IEnumerator SwitchFollowDelayed(int droneId)
    {
        yield return new WaitUntil(() =>
        {
            Vector3 currPos = droneManager.GetDroneObject(997).Item2.transform.position;
            return (currPos - _chargedDroneStartPos).sqrMagnitude < 0.1f;
        });
        _currDroneInfo = droneManager.GetDroneInfo(droneId);
        _currentDroneObject = droneManager.GetDroneObject(droneId);
        FollowDrone(_currentDroneObject.Item2);
        _currentFollowedDroneId = droneId;
        _isSwitching = false;
    }
    
    public void FollowDrone(GameObject drone)
    {
        _currentDrone = drone.transform;
        transform.SetParent(_currentDrone);
        transform.localPosition = new Vector3(0, camYOffset, 0);
        transform.localRotation = Quaternion.identity;
    }
    private void _assignFields()
    {
        _trackDroneId = _currDroneInfo.Item1;
        _startPos = _currDroneInfo.Item2;
        _endPos = _currDroneInfo.Item3;
        _trackedDronePos = _startPos;
    }
    private bool _InterpolateToEndPosition()
    {   
        bool nextDroneInfo = false;
        _lerpVal += Time.deltaTime * droneManager.interpolationSpeed; // move in sync with drone's speed
        // clip between 0 and 1 
        _lerpVal = Mathf.Clamp01(_lerpVal);
        _trackedDronePos = Vector3.Lerp(_startPos, _endPos, _lerpVal);
        _CamPlacementOnTrackedDrone();
        // _targetForward = (_endPos - _startPos).normalized;
        if (_lerpVal == 1)
        {
            nextDroneInfo = true;
            _camMovementPriority = true;
            _startRotation = transform.rotation;
            // if (_targetForward != _currForward)
            // {
            //     _camMovementPriority = true;
            // }
        }
        
        return nextDroneInfo;
    }
    
    // better visual for the cam on the drone
    private void _CamPlacementOnTrackedDrone()
    {
        transform.position = new Vector3(_trackedDronePos.x, _trackedDronePos.y + camYOffset, _trackedDronePos.z);
    }

    private void _FinishRotateCam()
    {
        rotationTimer += Time.deltaTime;
        var t = Mathf.Clamp01(rotationTimer / rotationDuration);
        Vector3 newDir = (_endPos - _startPos).normalized;
        newDir.y = 0.0f;
        if (newDir != Vector3.zero)
        {
            _targetForward = newDir;
        }
        // _targetForward = (_endPos - _startPos).normalized;
        // _targetForward.y = 0.0f; // keep the camera facing horizontal plane
        // for those when the drone keeps the same arrow direction
        float angleDiff = Quaternion.Angle(_startRotation, Quaternion.LookRotation(_targetForward));
        if (angleDiff < 0.1f) // threshold in degrees
        {
            Debug.Log("don't have to rotate");
            _startRotation = Quaternion.LookRotation(_targetForward);
            _camMovementPriority = false;
        }
        // if (_startRotation == Quaternion.LookRotation(_targetForward))
        // {
        //     Debug.Log("don't have to rotate");
        //     _camMovementPriority = false;
        // }
        transform.rotation = Quaternion.Slerp(
            _startRotation,
            Quaternion.LookRotation(_targetForward),
            t
        );
        if (t >= 1.0f)
        {
            rotationTimer = 0.0f;
            _currForward = _targetForward;
            _startRotation = Quaternion.LookRotation(_targetForward);
            _camMovementPriority = false;
        }
    }
}