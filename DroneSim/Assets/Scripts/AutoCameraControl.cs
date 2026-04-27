using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

// 1.5, 0.8, 2
// change Project Settings for Script Execution Order with Camera Controller after the Default Time
public class AutoCameraController : MonoBehaviour, ICameraManager
{
    public DroneManager droneManager;
    public bool fixedCameraMode = false;
    public float rotationSpeed = 80f;  // Rotation speed
    public float moveSpeed = 20f;        // Movement speed
    public float rotationSmoothTime = 0.1f; // Smoothing time for rotation
    public float movementSmoothTime = 0.1f; // Smoothing time for movement
    public float camScaleDistToBoxEdgeX = 0.5f; // edge of box as scale 1, the additional scaling for camera to be away from box
    public float camScaleDistToBoxEdgeY = 0.3f;
    public float camScaleDistToBoxEdgeZ = 0.3f;
    public float camInterpolationSpeed = 0.5f;
    public bool rotateForSecondary = true;
    // private bool _isSimulating = true;
    private float _camLerpVal = 0.0f;
    private float _camRefLerpVal = 0.0f;
    private bool _camMovementPriority = true; // if true, camera is moving but not the drones
    
    private Quaternion targetRotation;  // Stores the camera's desired rotation
    private Vector3 targetPosition;     // Stores the camera's target position
    private Quaternion startRotation;
    private Vector3 startPosition;
    
    private Vector3 _camPathLeftBackPos; 
    private Vector3 _camPathRightBackPos;
    private Vector3 _camPathLeftFrontPos;
    private Vector3 _camPathRightFrontPos;
    private Vector3 _upRightFrontPos = Vector3.zero;
    private Vector3 _upLeftBackPos = Vector3.zero;
    private List<Vector3> _camPathCornerList = new List<Vector3>();
    private List<Vector3> _oldCamPathCornerList = new List<Vector3>();
    private int _startCornerIndex = 0; // the corner to start the camera
    private int _endCornerIndex = 1; // the corner to end the camera
    private int _currLoopStartIndex = 0;
    // private int _cornerVisits = 1;
    private string _currCubeCommand;
    private Vector3 _currBoxCenter;
    
    private bool isRotating = false;
    private Quaternion _camStartRotation;
    private Quaternion _camTargetRotation;
    private float rotationTimer = 0f;
    private float _camRotationSpeed = 0.02f;
    public float rotationDuration = 0.5f; // how long to rotate
    private float _defaultArcLength;
    private float _newArcLength;
    private bool _getFirstArcLength = false;
    
    private void OnTargetChanged(Vector3 newTargetPosition)
    {
        isRotating = true;
        _camStartRotation = transform.rotation;

        Vector3 direction = newTargetPosition - transform.position;
        if (direction != Vector3.zero)
            _camTargetRotation = Quaternion.LookRotation(direction, Vector3.up);
    }
    
    // required just for the interface but no implementation 
    public void FollowDrone(GameObject drone)
    {
        return;
    }

    public bool GetCameraMovementPriority()
    {
        return _camMovementPriority;
    }
    void Start()
    {   
        // startRotation = transform.rotation;
        // startPosition = transform.position;
        // targetRotation = transform.rotation;
        // targetPosition = transform.position;
        // initialize the clockwise corner visits
        _camPathCornerList.Add(_camPathLeftBackPos);
        _camPathCornerList.Add(_camPathRightBackPos);
        _camPathCornerList.Add(_camPathRightFrontPos);
        _camPathCornerList.Add(_camPathLeftFrontPos);
        // _camPathCornerList.Add(_camPathLeftBackPos);
        _currBoxCenter = droneManager.GetBoxCenterPos();
        UpdateCamPathPlane();
        // AutoCamMovement();
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     _isSimulating = !_isSimulating;
        // }
        // Reset();
        // HandleRotation();
        // HandleMovement();
        if (fixedCameraMode)

        {

            _camMovementPriority = false;

            return;

        }
        
        if (_currBoxCenter != droneManager.GetBoxCenterPos())
        {
            OnTargetChanged(droneManager.GetBoxCenterPos());
        }
        UpdateCamPathPlane();
        AutoCamMovement(_oldCamPathCornerList);
    }
    
    // see if visual requires y scaling as well for better outcome 
    // lots of issue for if one pointonly changed and also adding to the list
    void UpdateCamPathPlane()
    {
        var upRightFrontPos = droneManager.GetBoxRightFrontPos();
        var upLeftBackPos = droneManager.GetBoxLeftBackPos();
        var boxCenterPos = droneManager.GetBoxCenterPos();
        
        // used when camera is updating not via add, sub, or run.
        // The inter step to transition to another mode movement if y gets updated
        var tempCamPathRightFrontPos = _camPathRightFrontPos;
        var tempCamPathLeftBackPos = _camPathLeftBackPos; 
        var tempCamPathLeftFrontPos = _camPathLeftFrontPos;
        var tempCamPathRightBackPos = _camPathRightBackPos;
        
        
        if (_upRightFrontPos == upRightFrontPos && _upLeftBackPos == upLeftBackPos)
        {
            // if (tempCamPathLeftFrontPos != _camPathLeftFrontPos)
            // {
            //     Debug.Log("the other two coordinates are different");
            //     Debug.Log(_camPathCornerList[0] + " " + _camPathCornerList[1] + " " + _camPathCornerList[2] + " " + _camPathCornerList[3]);
            // }
            // Debug.Log(_camPathCornerList[0] + " " + _camPathCornerList[1] + " " + _camPathCornerList[2] + " " + _camPathCornerList[3]);
            return;
        }
        
        // check if there's new corner or if the box center changed (need to recheck since ratio changed)
        if (_upRightFrontPos != upRightFrontPos || _currBoxCenter != boxCenterPos)
        {   
            // Debug.Log(upRightFrontPos);
            _upRightFrontPos = upRightFrontPos;
            _camPathRightFrontPos = new Vector3(
                upRightFrontPos.x + (camScaleDistToBoxEdgeX * (upRightFrontPos.x - boxCenterPos.x)),
                upRightFrontPos.y + (camScaleDistToBoxEdgeY * (upRightFrontPos.y - boxCenterPos.y)),
                upRightFrontPos.z + (camScaleDistToBoxEdgeZ * (upRightFrontPos.z - boxCenterPos.z))
            );
            // Vector3 boxHalfExtents = upRightFrontPos - boxCenterPos;
            //
            // _camPathRightFrontPos = boxCenterPos + new Vector3(
            //     boxHalfExtents.x * (1 + camScaleDistToBoxEdgeX),
            //     boxHalfExtents.y * (1 + camScaleDistToBoxEdgeY),
            //     boxHalfExtents.z * (1 + camScaleDistToBoxEdgeZ)
            // );
            
            // if the y position shifted (using 0.001 for precision issue if representing same spot)
            // if (_camPathRightFrontPos.y - tempCamPathRightFrontPos.y > 0.001f)
            var dist = Vector3.Distance(_camPathRightFrontPos, tempCamPathRightFrontPos);
            // if (_camPathLeftBackPos.y - tempCamPathLeftBackPos.y > 0.001f)
            if (dist > 0.005f)
            {
                _oldCamPathCornerList = new List<Vector3>();
                _oldCamPathCornerList.Add(tempCamPathLeftBackPos);
                _oldCamPathCornerList.Add(tempCamPathRightBackPos);
                _oldCamPathCornerList.Add(tempCamPathRightFrontPos);
                _oldCamPathCornerList.Add(tempCamPathLeftFrontPos);
                _oldCamPathCornerList.Add(tempCamPathLeftBackPos);
            }
        }

        if (_upLeftBackPos != upLeftBackPos || _currBoxCenter != boxCenterPos)
        {   
            // Debug.Log("change upleftback");
            _upLeftBackPos = upLeftBackPos;
            _camPathLeftBackPos = new Vector3(
                upLeftBackPos.x - (camScaleDistToBoxEdgeX * (boxCenterPos.x - upLeftBackPos.x)),
                upLeftBackPos.y + (camScaleDistToBoxEdgeY * (upLeftBackPos.y - boxCenterPos.y)),
                upLeftBackPos.z - (camScaleDistToBoxEdgeZ * (boxCenterPos.z - upLeftBackPos.z))
            );
            // Vector3 boxHalfExtents = upRightFrontPos - boxCenterPos;
            //
            // _camPathRightFrontPos = boxCenterPos + new Vector3(
            //     -boxHalfExtents.x * (1 + camScaleDistToBoxEdgeX),
            //     boxHalfExtents.y * (1 + camScaleDistToBoxEdgeY),
            //     -boxHalfExtents.z * (1 + camScaleDistToBoxEdgeZ)
            // );
            
            var dist = Vector3.Distance(_camPathLeftBackPos, tempCamPathLeftBackPos);
            // if (_camPathLeftBackPos.y - tempCamPathLeftBackPos.y > 0.001f)
            if (dist > 0.005f)
            {   
                _oldCamPathCornerList = new List<Vector3>();
                _oldCamPathCornerList.Add(tempCamPathLeftBackPos);
                _oldCamPathCornerList.Add(tempCamPathRightBackPos);
                _oldCamPathCornerList.Add(tempCamPathRightFrontPos);
                _oldCamPathCornerList.Add(tempCamPathLeftFrontPos);
                _oldCamPathCornerList.Add(tempCamPathLeftBackPos);
            }
        }
        
        // update the ramaining two corners if the other two corners have new updates
        _camPathLeftFrontPos = new Vector3(
            _camPathLeftBackPos.x,
            _camPathLeftBackPos.y,
            _camPathLeftBackPos.z + (_camPathRightFrontPos.z - _camPathLeftBackPos.z)
        );
        _camPathRightBackPos = new Vector3(
            _camPathRightFrontPos.x,
            _camPathRightFrontPos.y,
            _camPathRightFrontPos.z - (_camPathRightFrontPos.z - _camPathLeftBackPos.z)
        );
        
        // counterclockwise
        _camPathCornerList[0] = _camPathLeftBackPos;
        _camPathCornerList[1] = _camPathRightBackPos;
        _camPathCornerList[2] = _camPathRightFrontPos;
        _camPathCornerList[3] = _camPathLeftFrontPos;
        // _camPathCornerList[4] = _camPathLeftBackPos;
        
        // Debug.Log(_camPathCornerList[0] + " " + _camPathCornerList[1] + " " + _camPathCornerList[2] + " " + _camPathCornerList[3]);
    }
    
    // uniform scaling for finding the corner to place camera for sub add (independent xyz stretching causes inaccurate detection in FindClosesPoint)
    private List<Vector3> _UniformScaleCamPathCornerList(float scale)
    {   
        var outputList = new List<Vector3>();
        var upRightFrontPos = droneManager.GetBoxRightFrontPos();
        var upLeftBackPos = droneManager.GetBoxLeftBackPos();
        var boxCenterPos = droneManager.GetBoxCenterPos();
        
        var uniformCamPathRightFrontPos = new Vector3(
            upRightFrontPos.x + (scale * (upRightFrontPos.x - boxCenterPos.x)),
            upRightFrontPos.y + (scale * (upRightFrontPos.y - boxCenterPos.y)),
            upRightFrontPos.z + (scale * (upRightFrontPos.z - boxCenterPos.z))
        );
        
        var uniformCamPathLeftBackPos = new Vector3(
            upLeftBackPos.x - (scale * (boxCenterPos.x - upLeftBackPos.x)),
            upLeftBackPos.y + (scale * (upLeftBackPos.y - boxCenterPos.y)),
            upLeftBackPos.z - (scale * (boxCenterPos.z - upLeftBackPos.z))
        );
        
        // update the ramaining two corners if the other two corners have new updates
        var uniformCamPathLeftFrontPos = new Vector3(
            uniformCamPathLeftBackPos.x,
            uniformCamPathLeftBackPos.y,
            uniformCamPathLeftBackPos.z + (uniformCamPathRightFrontPos.z - uniformCamPathLeftBackPos.z)
        );
        var uniformCamPathRightBackPos = new Vector3(
            uniformCamPathRightFrontPos.x,
            uniformCamPathRightFrontPos.y,
            uniformCamPathRightFrontPos.z - (uniformCamPathRightFrontPos.z - uniformCamPathLeftBackPos.z)
        );
        
        // counterclockwise
        outputList.Add(uniformCamPathLeftBackPos);
        outputList.Add(uniformCamPathRightBackPos);
        outputList.Add(uniformCamPathRightFrontPos);
        outputList.Add(uniformCamPathLeftFrontPos);
        
        return outputList;
    }
    
    void AutoCamMovement(List<Vector3> cornerList)
    {
        // camera always moves before any drone movements if not in secondary nmode (needs _camMovementPriority to determine the state)
        if (_camMovementPriority == false && _currCubeCommand == droneManager.GetCurrCubeCommand() && droneManager.GetCurrMode() != "secondary")
        {
            return;
        }
        // rotation of camera to track box center
        if (isRotating)
        {   
            _currBoxCenter = droneManager.GetBoxCenterPos();
            // Debug.Log("rotating around center:" + _currBoxCenter);
            _camMovementPriority = true;
            rotationTimer += Time.deltaTime;
            var t = Mathf.Clamp01(rotationTimer / rotationDuration);
            // float t = rotationTimer / rotationDuration;
            // Debug.Log(rotationTimer);
            transform.rotation = Quaternion.Slerp(_camStartRotation, _camTargetRotation, t);

            if (t >= 1f)
            {
                // Debug.Log("reached rotationTime");
                rotationTimer = 0f;
                isRotating = false;
            }
            else
            {
                return;
            }
        }
        // transition movement between camera mode movements if needed
        if (cornerList.Count != 0)
        {
            _camMovementPriority = true;
            var reachEndPos = InterpolateCamPos(cornerList[_startCornerIndex], _camPathCornerList[_startCornerIndex],false);
            if (reachEndPos)
            {
                _oldCamPathCornerList = new List<Vector3>();
            }
            else
            {
                return;
            }
        }
        
        _currCubeCommand = droneManager.GetCurrCubeCommand();
        var mode = droneManager.GetCurrMode();
        // Debug.Log(mode);
        // mode as run, rotate around once and back to leftBackPos of Box
        if (mode == "run")
        {
            // bool newStartInd;
            // if (transform.position != _camPathCornerList[_startCornerIndex])
            // {
            //     newStartInd = InterpolateCamPos(transform.position, _camPathCornerList[_endCornerIndex]);
            // }
            // else
            // {
            // var newStartInd = InterpolateCamPos(_camPathCornerList[_startCornerIndex], 
            //         _camPathCornerList[_endCornerIndex]);
            //
            // loop detected
            // if (newStartInd)
            // {
            //     Debug.Log(_camPathCornerList[_startCornerIndex] + "to" + _camPathCornerList[_endCornerIndex]);
            // }
            
            var newStartInd = InterpolateCamPos(_camPathCornerList[_startCornerIndex], 
                _camPathCornerList[_endCornerIndex]);
            
            if (_startCornerIndex == _currLoopStartIndex && newStartInd)
            {
                // Debug.Log("loop detected");
                _camMovementPriority = false;
            }
            else
            {
                _camMovementPriority = true;
            }
        }
        else if (mode == "secondary" && rotateForSecondary) // when drones are adding or subtracting with green & blue drones on screen
        {
            InterpolateCamPos(_camPathCornerList[_startCornerIndex],
                _camPathCornerList[_endCornerIndex]);   
        }
        else if (mode == "secondary" && !rotateForSecondary)
        {   
            // nothing to do so pass on 
            return; 
        }
        // case when mode is add or sub
        else
        {
            string[] values = droneManager.GetCurrCubeCommand().Split(',');
            // Debug.Log(droneManager.GetCurrCubeCommand());
            var x = float.Parse(values[1]);
            var y = float.Parse(values[2]);
            var z = float.Parse(values[3]);
            var bottomLeftBackCornerDronePos = Utils.rotateYZ(new Vector3(x, y, z));
            // bottomLeftBackCornerDronePos = Utils.rotateCustom(bottomLeftBackCornerDronePos, droneManager.roll,
            //     droneManager.pitch, droneManager.yaw);
            // requires the cubeCenter without custom rotation to compare for closesPoint
            // Vector3 unifromCubeCenter = CubeHelper.cubeCenter(bottomLeftBackCornerDronePos, droneManager.spacing, droneManager.roll, droneManager.pitch, droneManager.yaw);
            // Tuple<float, float, float> rot = droneManager.GetCustomRotationAngle();
            // bottomLeftBackCornerDronePos = Utils.rotateCustom(bottomLeftBackCornerDronePos, 
            //                                                 rot.Item1, 
            //                                                 rot.Item2, 
            //                                                 rot.Item3);
            Vector3 newCubeCenter = CubeHelper.cubeCenter(bottomLeftBackCornerDronePos, droneManager.spacing, droneManager.roll, droneManager.pitch, droneManager.yaw);
            var uniformCornerList = _UniformScaleCamPathCornerList(1f);
            int ind = Utils.FindClosestPoint(newCubeCenter, uniformCornerList); // returns the closest corner 
            _currLoopStartIndex = ind; // assign for later in run mode, as the loop index 
            // Debug.Log(_currLoopStartIndex);
            var newStartInd = InterpolateCamPos(_camPathCornerList[_startCornerIndex],
                _camPathCornerList[_endCornerIndex]);
            // arrived at the corner we want for sub/add 
            if (_startCornerIndex == _currLoopStartIndex && newStartInd)
            {
                _camMovementPriority = false;
            }
            else
            {
                _camMovementPriority = true;
            }
        }
    }
    
    // return true if new startInd is used
    /**
     * the bool specifies if camera currently is moving via the run, add, sub logic (true)
     * if not (i.e. gapping the jump if y position changed for _camPath... --> (false))
     */
    bool InterpolateCamPos(Vector3 startPos, Vector3 endPos, bool inModeMovement=true)
    {   
        // _camLerpVal += Time.deltaTime * camInterpolationSpeed;
        // clip between 0 and 1 
        // _camLerpVal = Mathf.Clamp01(_camLerpVal);

        if (inModeMovement)
        {   
            // 🎯 Project everything to the height of the arc plane
            float heightY = startPos.y;
            Vector3 flatCenter = new Vector3(droneManager.GetBoxCenterPos().x, heightY, droneManager.GetBoxCenterPos().z);
            Vector3 flatStart = new Vector3(startPos.x, heightY, startPos.z);
            Vector3 flatEnd = new Vector3(endPos.x, heightY, endPos.z);

            // Vector3 startDir = flatStart - flatCenter;
            // Vector3 endDir = flatEnd - flatCenter;
            //
            // Vector3 interpolatedPos = flatCenter + interpolatedDir * radius;

            // float radius = startDir.magnitude;
            // float radius2 = endDir.magnitude;
            // if ((radius2 - radius) > 0.1f)
            // {
            //     Debug.Log("radius of start" + startPos + " , radius of end" + endPos + "not equal");
            // }

            // Compute angles
            // float angleStart = Mathf.Atan2(startDir.z, startDir.x);  // radians
            // float angleEnd = Mathf.Atan2(endDir.z, endDir.x);        // radians

            // Make sure angle difference is minimal (handles wrapping around 360°)
            // float angleDiff = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStart, Mathf.Rad2Deg * angleEnd);
            // float angleLerp = angleStart + Mathf.Deg2Rad * (angleDiff * _camLerpVal);

            // Compute interpolated position on circle
            // Vector3 offset = new Vector3(Mathf.Cos(angleLerp), 0, Mathf.Sin(angleLerp)) * radius;
            // Vector3 interpolatedPos = flatCenter + offset;

            Vector3 startDir = (flatStart - flatCenter);
            Vector3 endDir = (flatEnd - flatCenter);
            float startDirMagnitude = startDir.magnitude;
            float endDirMagnitude = endDir.magnitude;
            // float radius = (flatStart - flatCenter).magnitude;

            Vector3 startDirFlip = flatCenter - flatStart;
            Vector3 endDirFlip = flatCenter - flatEnd;
            float startDirFlipMagnitude = startDirFlip.magnitude;
            float endDirFlipMagnitude = endDirFlip.magnitude;
            // Debug.Log(startDirFlipMagnitude);
            // Debug.Log(endDirFlipMagnitude);
            // Interpolate direction using spherical interpolation
            // float dotProduct = Mathf.Abs(Vector3.Dot(startDirFlip, endDirFlip)) / (startDirFlipMagnitude * endDirFlipMagnitude);
            float dotProduct = Vector3.Dot(startDir, endDir) / (startDirMagnitude * endDirMagnitude);
            float angleRadians = Mathf.Acos(dotProduct);
            // Debug.Log(angleRadians);
            float arcLength = startDirFlipMagnitude * angleRadians;
            // if (_getFirstArcLength == false)
            // {
            //     _defaultArcLength = arcLength;
            //     _newArcLength = arcLength;
            //     _getFirstArcLength = true;
            //     // Debug.Log(startDirFlipMagnitude);
            //     // Debug.Log(endDirFlipMagnitude);
            //     // Debug.Log(angleRadians);
            // }
            // else if (_camLerpVal == 0f)
            // {
            //     _newArcLength = arcLength;
            //     // Debug.Log(startDirFlipMagnitude);
            //     // Debug.Log(endDirFlipMagnitude);
            //     // Debug.Log(angleRadians);
            // }
            // float arclengthRatio = _newArcLength/ _defaultArcLength;
            // lerpVal increase base on how large arclength is, thus longer arclength, less increment per frame
            _camLerpVal += (camInterpolationSpeed * Time.deltaTime) / arcLength;
            // _camRefLerpVal += (camInterpolationSpeed * Time.deltaTime) / arcLength;
            // lerping value is the reciprocal of the arclengthRatio to get constant speed based on first interpolation
            // _camLerpVal = _camRefLerpVal * (1 / arclengthRatio);
            _camLerpVal = Mathf.Clamp01(_camLerpVal);
            transform.position = Vector3.Slerp(startDir, endDir, _camLerpVal);
            transform.position += flatCenter;
            // Vector3 interpolatedPos = flatCenter + interpolatedDir * radius;
            //
            // transform.position = interpolatedPos;
            // if (_camLerpVal >= 1)
            // {
            //     Debug.Log(transform.position);
            // }
            transform.LookAt(droneManager.GetBoxCenterPos(), Vector3.up);
        }

        else
        {   
            float pathLength = Vector3.Distance(startPos, endPos);
            // camInterpolationSpeed is now in units/sec
            _camLerpVal += (camInterpolationSpeed * Time.deltaTime) / pathLength;
            _camLerpVal = Mathf.Clamp01(_camLerpVal);
            transform.position = Vector3.Lerp(startPos, endPos, _camLerpVal);
            transform.LookAt(droneManager.GetBoxCenterPos(), Vector3.up);
        }
        
        
        if (_camLerpVal == 1 && inModeMovement)
        {
            // Debug.Log("we are changing start index");
            _startCornerIndex = (_startCornerIndex + 1) % 4; // four corners to loop 
            _endCornerIndex = (_endCornerIndex + 1) % 4;
            _camLerpVal = 0.0f; // reset for next lerp
            // _camRefLerpVal = 0.0f; // reset for next lerp
            return true;
        }
        if (_camLerpVal == 1)
        {   
            _camLerpVal = 0.0f;
            return true;
        }

        return false;
    }
    
    // update the camPath corner positions based on DroneManager's _uprightfrontPos and _upleftbackPos
    void Reset()
    {
        if (Input.GetKey(KeyCode.R))
        {
            targetPosition = startPosition;
            targetRotation = startRotation;
        }
    }
    /*
    void HandleRotation()
    {   
        // float yaw = Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime;  // Left/Right Arrow (Y-axis)
        // float pitch = -Input.GetAxisRaw("Vertical") * rotationSpeed * Time.deltaTime; // Up/Down Arrow (X-axis)

        float yaw = 0f;
        float pitch = 0f;
        // Use Arrow Keys for rotation
        if (Input.GetKey(KeyCode.LeftArrow)) yaw -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) yaw += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow)) pitch -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) pitch += rotationSpeed * Time.deltaTime;
        // Rotate around the Y-axis (left/right)
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);

        // Rotate around the X-axis (up/down) relative to the camera's right vector
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, transform.right);

        // Apply rotations
        targetRotation = yawRotation * pitchRotation * targetRotation;

        // Use Lerp instead of Slerp for more predictable smoothing
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothTime * 5f);
    }

    void HandleMovement()
    {
        float forwardMovement = 0f;
        float strafeMovement = 0f;

        // Use W/S for forward and backward movement
        if (Input.GetKey(KeyCode.W)) forwardMovement = 1f;
        if (Input.GetKey(KeyCode.S)) forwardMovement = -1f;

        // Use A/D for strafing left/right
        if (Input.GetKey(KeyCode.A)) strafeMovement = -1f;
        if (Input.GetKey(KeyCode.D)) strafeMovement = 1f;

        // Move in the direction relative to camera's orientation
        Vector3 moveDirection = (transform.forward * forwardMovement) + (transform.right * strafeMovement);
        targetPosition += moveDirection.normalized * moveSpeed * Time.deltaTime;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, targetPosition, movementSmoothTime * 5f);
    }
    */
}