using System;
using System.Collections;
using System.Collections.Generic;
using SystemDrawingColor = System.Drawing.Color;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;



// left in variable is negative x in Unity Coordinate System 
// front in variable is positive z in Unity Coordinate System 
// up in variable is positive y in Unity Coordinate System 
// box refers to the cube assembled big prism shape
// (for box once added a unit in one direction, box assumes the prism grew to that size ignoring unfilled cubes)

// Rendering uses the CSV drone data as Z axis pointing up and Y axis in the horizontal plane
// Unity coordinate system is Y axis up

public class DroneManager : MonoBehaviour
{   
    // public AutoCameraController cameraController;
    public CameraManager cameraManager;
    
    // data for different Game Objects
    private Dictionary<int, GameObject> _drones = new Dictionary<int, GameObject>();
    private Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>> _droneInfo = new Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>>();
    private Dictionary<int, GameObject> _droneArrows = new Dictionary<int, GameObject>();
    private Dictionary<int, Tuple<Quaternion, Quaternion>> _droneRotations = new Dictionary<int, Tuple<Quaternion, Quaternion>>();
    private List<GameObject> _cubes = new List<GameObject>();
    
    // cube specific value to track
    private string _cubeRenderFolderPath = "CubeCommandAssets/changes.csv"; // Path to changes.csv
    private List<string> _cubeCommandLines;
    private string _currMode; // for cube to update if a new mode came in
    private string _camTrackMode = "run"; // three modes for other class to access (1. run, 2. add, 3. sub)
    private string _currAllPossibleMode; // for other classes to access (run, fill, secondary, file
    private string _currCubeCommand = ""; // commands to generate the drones(becomes the cube rendering commands)
    
    // Prefabs for Game Objects
    public GameObject dronePrefab; // drones to render
    public GameObject droneArrowPrefab; // arrows to render
    public GameObject chargingBlockPrefab;
    public GameObject defaultCubePrefab;
    public GameObject invertCubePrefab;
    private GameObject _cubeToRemove;
    
    // cube Toggle values to track
    public Toggle cubeToggle;
    private bool _cubeToggleState;
    
    // general rendering spec
    public float spacing = 5f;
    private int _currentFrame = 0;
    private bool _isSimulating = true; // track if overall drone simulation stops
    private bool _userPause = false; // track if user paused
    private bool _continueMode = false; // track if user don't want to stop simulation by default
    public float interpolationSpeed = 0.5f;
    private float _lerpVal = 0.0f; // value to track lerp value (0 ~ 1)
    private int _startDroneId; // the DroneId that has startPos as (0, 0, 0)
    private Vector3 _chargedDroneStartPos; 
    private Vector3 _chargedDroneFaceDirection;
    private Vector3 _exitDroneFaceDirection; // stores the initial drone facing direction when the drone gets to (1, 0, -1)
    
    // useful fields for drone camera mode
    private int _trackDroneId; // the DroneId that drone Cam is set on 
    private int _drainedDroneId; // the DroneId exiting the simulation for charging
    private Vector3 _droneCamPos; // the position of where the drone of droneCam is 
    private Vector3 _droneCamFaceDirection; // the direction droneCam will face assuming itself as center
    
    // automatic camera movement required fields
    private Vector3 _uprightfrontPos = Vector3.zero; // takes the max XYZ position of all cubes made, front is psotiive z direction in unity coordinates
    private Vector3 _upleftbackPos = Vector3.zero; // take the max Y position, min XZ position of all cubes made
    private Vector3 _botcenterPos = Vector3.zero; // keep track of the height(y direction) of the box
    private Vector3 _boxCenter = Vector3.zero;
    
    // drone rotations for drone camera view
    private float rotationTimer = 0.0f;
    private float rotationDuration = Constants.RotationDurationDroneCam;
    private float _t;
    private bool _droneRotatePriority = false;
    public bool switchDroneFollow = false;
    
    // NEW STUFF
    public CSVLogic csvLogic;
    private int _exitDroneId;
    

    /**
     * parameter as -1 if getting droneInfo from the drone that just entered the simulation (i.e. pos(0, 0, 0)
     * parameter as 997 for the charging drone info
     * default is the actual provided droneId that exists in the simulation
     * returns the droneId, startPos, endPos of requested Drone
     */
    public Tuple<int, Vector3, Vector3, Vector3> GetDroneInfo(int droneId)
    {   
        Tuple<int, Vector3, Vector3, Vector3> requestedDroneInfo;
        switch (droneId)
        {
            case -1:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(_startDroneId))
                {
                    requestedDroneInfo = null;
                }
                else
                {
                    requestedDroneInfo = new Tuple<int, Vector3, Vector3, Vector3>(_startDroneId, _droneInfo[_startDroneId].Item3, _droneInfo[_startDroneId].Item4, _droneInfo[_startDroneId].Item1);
                }
                break;
            case 997:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(997))
                {
                    requestedDroneInfo = null;
                }
                else
                {
                    requestedDroneInfo = new Tuple<int, Vector3, Vector3, Vector3>(997, _droneInfo[997].Item3, _droneInfo[997].Item4, _droneInfo[997].Item1);
                }
                break;
            default:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(droneId))
                {
                    requestedDroneInfo = null;
                }
                else
                {
                    requestedDroneInfo = new Tuple<int, Vector3, Vector3, Vector3>(droneId, _droneInfo[droneId].Item3, _droneInfo[droneId].Item4,  _droneInfo[droneId].Item1);
                }
                break;
        }
        return requestedDroneInfo;
    }

    public Tuple<int, GameObject> GetDroneObject(int droneId)
    {   
        Tuple<int, GameObject> requestedDroneObject;
        switch (droneId)
        {
            case -1:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(_startDroneId))
                {
                    requestedDroneObject = null;
                }
                else
                {   
                    GameObject drone = _drones[_startDroneId];
                    requestedDroneObject = new Tuple<int, GameObject>(_startDroneId, drone);
                }
                break;
            case 997:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(997))
                {
                    requestedDroneObject = null;
                }
                else
                {
                    GameObject drone = _drones[997];
                    requestedDroneObject = new Tuple<int, GameObject>(997, drone);
                }
                break;
            default:
                if (_droneInfo.Count == 0 || !_droneInfo.ContainsKey(droneId))
                {
                    requestedDroneObject = null;
                }
                else
                {
                    GameObject drone = _drones[droneId];
                    requestedDroneObject = new Tuple<int, GameObject>(droneId, drone);
                }
                break;
        }
        return requestedDroneObject;
    }
    /**
     * return the left back upper corner position of the total box created by cubes 
     */
    public Vector3 GetBoxLeftBackPos()
    {
        return _upleftbackPos;
    }

    public Vector3 GetBoxRightFrontPos()
    {
        return _uprightfrontPos;
    }

    public Vector3 GetBoxCenterPos()
    {
        return _boxCenter;
    }
    // current mode as in drones movement CSV's mode (not the sub, add, run for cubes of changes.csv)
    public string GetCurrMode()
    {
        return _currAllPossibleMode;
    }

    public string GetCurrCubeCommand()
    {
        return _currCubeCommand;
    }

    public bool GetDroneRotatePriority()
    {
        return _droneRotatePriority;
    }

    public bool GetIsSimulating()
    {
        return _isSimulating;
    }

    void Start()
    {
        dronePrefab.GetComponentInChildren<MeshRenderer>().enabled = false; // don't render the foundational drone
        chargingBlockPrefab.GetComponentInChildren<MeshRenderer>().enabled =
            false; // don't render the foundational charging block\
        defaultCubePrefab.GetComponentInChildren<MeshRenderer>().enabled = false; // don't render the foundational cube
        invertCubePrefab.GetComponentInChildren<MeshRenderer>().enabled =
            false; // don't render the foundational invert cube
        
        // NEW STUFF
        csvLogic.FetchChangesCSV(); // does same stuff as _cubeCommandLines =
        // CubeHelper.FetchCubeCsv(Path.Combine(Application.dataPath,
        //     _cubeRenderFolderPath)); in Start()
        _currCubeCommand = csvLogic.ReadChanges();
        // Debug.Log(_currCubeCommand);
        ///////// NEW STUFF ENDS // 
        /*
        // _cubeCommandLines =
        //     CubeHelper.FetchCubeCsv(Path.Combine(Application.dataPath,
        //         _cubeRenderFolderPath)); // stores all commands (run 2, , ), etc.
        */
        cubeToggle.onValueChanged.AddListener(_updateCubeRender); // detect if user changes cube rendering setting
        UpdateCube(true); // default one cube rendering
        _updateCubeRender(cubeToggle.isOn); // initial load set the _cubeToggleState with initial value
        // set the desired location for charging drone to render charging block and died drones arrow
        _chargedDroneStartPos = new Vector3(0.5f * spacing, -1.5f * spacing, -0.5f * spacing);
        _chargedDroneFaceDirection = (Vector3.zero - _chargedDroneStartPos).normalized;
        _chargedDroneFaceDirection.y = 0.0f; // don't want rotation on y axis;
        
        // csvLogic = new CSVLogic();
        
        // NEW STUFF
        InitializeDrones(spacing);
        Debug.Log("finish start");
    }

    void Update()
    {   
        // Debug.Log("enter update");
        // pause or no pause
        if (Input.GetKeyDown(KeyCode.Space)){
            // _isSimulating = !_isSimulating;
            _userPause = !_userPause;
        }
        
        // take precendence for stopping animation 
        if (_userPause)
        {
            _isSimulating = false;
        }
        else if (_droneRotatePriority && !cameraManager.GetAutoCameraActive())
        {
            _isSimulating = false;
            _FinishRotateDrone(_trackDroneId);
        }
        else if (cameraManager.GetCameraMovementPriority())
        {
            _isSimulating = false;
        }
        else
        {
            _isSimulating = true;
        }
        
        // continue mode or not
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     _continueMode = !_continueMode;
        // }
        
        if (_isSimulating)
        {   
            // drones reached the end position
            bool reachEnd = InterpolateToEndPosition();
            if (reachEnd && cameraManager.GetAutoCameraActive())
            {   
                /*// Debug.Log(cameraManager.GetAutoCameraActive());
                // reset _t for next CSV lerp
                _lerpVal = 0.0f;
                FetchCsv();
                // _StoreDroneRotation(_trackDroneId);
                ResetChargingDrones();
                // if (!_continueMode)
                // {
                //     _isSimulating = !_isSimulating;
                // }*/
                
                
                // Claire refactoring
                /*
                _lerpVal = 0.0f;
                var result = csvLogic.UpdateStreaming();
                _startDroneId = result.Item1;
                _exitDroneId = result.Item2;
                _droneInfo = csvLogic.ReadStreaming();
                // assign the drained drone end position for interpolation to go to charging block
                _droneInfo[_exitDroneId] = new Tuple<Vector3, string, Vector3, Vector3, string>(_droneInfo[_exitDroneId].Item1, _droneInfo[_exitDroneId].Item2,
                    _droneInfo[_exitDroneId].Item3, _chargedDroneStartPos, _droneInfo[_exitDroneId].Item5); 
                GenerateChargingDrones(); // for some reason, execute this before UpdateFrones() so 997 can exist
                UpdateDrones();
                // GenerateChargingDrones();
                ResetChargingDrones();
                // Debug.Log("the auto camera is active");
                */
                
                _lerpVal = 0.0f;
                var result = csvLogic.UpdateStreaming();
                
                // assign the drained drone end position for interpolation to go to charging block
                _droneInfo = csvLogic.ReadStreaming();
                // csvLogic has default -2 if no update for a new starting drone
                if (result.Item1 != -2)
                {
                    _startDroneId = result.Item1;
                }
                // csvLogic has default -2 if no drone is exiting for this CSV
                if (result.Item2 != -2)
                {   
                    // Debug.Log(_exitDroneId);
                    _exitDroneId = result.Item2;
                    _droneInfo[_exitDroneId] = new Tuple<Vector3, string, Vector3, Vector3, string>(_droneInfo[_exitDroneId].Item1, _droneInfo[_exitDroneId].Item2,
                        _droneInfo[_exitDroneId].Item3, _chargedDroneStartPos, _droneInfo[_exitDroneId].Item5); 
                }
                
                // get a key for the current mode
                string mode = "run";
                int i = 0;
                Debug.Log(_droneInfo.Count);
                foreach (var key in _droneInfo.Keys)
                {
                    if (i == 1)
                    {
                        break;
                    }
                    i++;
                    mode = _droneInfo[key].Item5;
                    Debug.Log("getting from _droneinfo: " + mode);
                }
                GenerateChargingDrones(); // add in the 997 and 998  // for some reason, execute this before UpdateFrones() so 997 can exist
                UpdateDrones();
                
                // string _currCubeCommand = csvLogic.UpdateChanges();
                // string[] values = _currCubeCommand.Split(',');
                // string mode = values[0];
                
                if (_currMode == null || (mode != _currMode && mode != "secondary")) 
                {
                    Debug.Log("enter" + mode);
                    UpdateCube();
                    // Debug.Log("enter" + mode);
                    _currMode = mode;
                }
                // Debug.Log(_currAllPossibleMode);
                if (_currAllPossibleMode == null || mode != _currAllPossibleMode)
                {   
                    Debug.Log("change all possible mode to: " + mode);
                    _currAllPossibleMode = mode;
                    // Debug.Log(_currAllPossibleMode);
                }
                // GenerateChargingDrones(); // add in the 997 and 998
                // _StoreDroneRotation(_trackDroneId);
                ResetChargingDrones();
            }
            
            else if (reachEnd && !cameraManager.GetAutoCameraActive())
            {
                /*// reset _t for next CSV lerp
                _lerpVal = 0.0f;
                FetchCsv(); // updateDrones() --> GenerateCharging
                
                // GameObject drone = GetDroneObject(_startDroneId).Item2;
                // cameraManager.FollowDrone(drone);
                    
                _StoreDroneRotation(_trackDroneId);
                ResetChargingDrones();
                // if (!_continueMode)
                // {
                //     _isSimulating = !_isSimulating;
                // }*/
                
                _lerpVal = 0.0f;
                var result = csvLogic.UpdateStreaming();
                
                // assign the drained drone end position for interpolation to go to charging block
                _droneInfo = csvLogic.ReadStreaming();
                // csvLogic has default -2 if no update for a new starting drone
                if (result.Item1 != -2)
                {
                    _startDroneId = result.Item1;
                }
                // csvLogic has default -2 if no drone is exiting for this CSV
                if (result.Item2 != -2)
                {   
                    Debug.Log(_exitDroneId);
                    _exitDroneId = result.Item2;
                    _droneInfo[_exitDroneId] = new Tuple<Vector3, string, Vector3, Vector3, string>(_droneInfo[_exitDroneId].Item1, _droneInfo[_exitDroneId].Item2,
                        _droneInfo[_exitDroneId].Item3, _chargedDroneStartPos, _droneInfo[_exitDroneId].Item5); 
                }
                
                // get a key for the current mode
                string mode = "run";
                int i = 0;
                foreach (var key in _droneInfo.Keys)
                {
                    if (i == 1)
                    {
                        break;
                    }
                    i++;
                    mode = _droneInfo[key].Item5;
                }
                GenerateChargingDrones(); // add in the 997 and 998  // for some reason, execute this before UpdateFrones() so 997 can exist
                UpdateDrones();
                
                // string _currCubeCommand = csvLogic.UpdateChanges();
                // string[] values = _currCubeCommand.Split(',');
                // string mode = values[0];
                
                if (_currMode == null || (mode != _currMode && mode != "secondary")) 
                {
                    UpdateCube();
                    
                    _currMode = mode;
                }

                if (_currAllPossibleMode == null || mode != _currAllPossibleMode)
                {   
                    _currAllPossibleMode = mode;
                    // Debug.Log(_currAllPossibleMode);
                }
                // GenerateChargingDrones(); // add in the 997 and 998
                _StoreDroneRotation(_trackDroneId);
                ResetChargingDrones();

                // Debug.Log("the auto camera is not active");
            }
            // interpolate drones from start to end position
        }
    }
    
    public void InitializeDrones(float setSpacing)
    {
        
        /*
        // scaling the space between drones
        spacing = setSpacing;

        // Start processing CSV updates
        // StartCoroutine(PlayFrames());
        FetchCsv();
        MakeChargingBlock();
        */
        spacing = setSpacing;
        _droneInfo = new Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>>(csvLogic.FetchStreaming());
        // Debug.Log(_droneInfo.Count);
        
        
        // NEW STUFF STARTS
        _droneInfo = csvLogic.ReadStreaming();
        // // csvLogic has default -2 if no update for a new starting drone
        // if (result.Item1 != -2)
        // {
        //     _startDroneId = result.Item1;
        // }
        // // csvLogic has default -2 if no drone is exiting for this CSV
        // if (result.Item2 != -2)
        // {   
        //     Debug.Log(_exitDroneId);
        //     _exitDroneId = result.Item2;
        //     _droneInfo[_exitDroneId] = new Tuple<Vector3, string, Vector3, Vector3, string>(_droneInfo[_exitDroneId].Item1, _droneInfo[_exitDroneId].Item2,
        //         _droneInfo[_exitDroneId].Item3, _chargedDroneStartPos, _droneInfo[_exitDroneId].Item5); 
        // }
        
        // get a key for the current mode
        string mode = "run";
        // int i = 0;
        // foreach (var key in _droneInfo.Keys)
        // {
        //     if (i == 1)
        //     {
        //         break;
        //     }
        //     i++;
        //     mode = _droneInfo[key].Item5;
        // }
        
        if (_currMode == null || (mode != _currMode && mode != "secondary")) 
        {
            UpdateCube();
            Debug.Log("enter" + mode);
            _currMode = mode;
        }
        // Debug.Log(_currAllPossibleMode);
        if (_currAllPossibleMode == null || mode != _currAllPossibleMode)
        {   
            _currAllPossibleMode = mode;
            // Debug.Log(_currAllPossibleMode);
        }
        /// NEW STUFF END ///
        GenerateChargingDrones();
        UpdateDrones();
        MakeChargingBlock();
        
    }

    /*
    // handle the end and start droneId 
    void FetchCsv()
    {
      
        // https://learn.microsoft.com/en-us/dotnet/api/system.int32.tostring?view=netframework-4.7.2#system-int32-tostring(system-string)
        string filePath = Path.Combine(Application.streamingAssetsPath, $"zzz_{_currentFrame.ToString("D10")}.csv");

        if (File.Exists(filePath))
        {
            ReadCSV(filePath);
            UpdateDrones();
        }
        // no more CSV
        else
        {
            return;
        }
        _currentFrame++;
    }
    */

    /*
    void ReadCSV(string filePath)
    {
        // recalculate the dictionary for CSV data
        _droneInfo.Clear();
        // Debug.Log(filePath);
        string[] lines = File.ReadAllLines(filePath);
        string mode = "run";
        
        
        foreach (string line in lines)
        {
            string[] values = line.Split(',');
            // sotre current drone position
            int droneId = int.Parse(values[0]);
            float x = float.Parse(values[1]) * spacing;
            float y = float.Parse(values[2]) * spacing;
            float z = float.Parse(values[3]) * spacing;

            string color = values[4];

            Vector3 curr_pos = Utils.rotateYZ(new Vector3(x, y, z));
            if (curr_pos == Vector3.zero)
            {
                _startDroneId = droneId; // keep track of start drone 
            }
            // stagnent data (drones not moving or outdated)
            if (values.Length == 6 && values[0] != "drone_id") // Skip header
            {

                _droneInfo.Add(droneId,
                    new Tuple<Vector3, string, Vector3, Vector3, string>(curr_pos, color, curr_pos, curr_pos,
                        String.Empty));

            }
            else
            {

                // store start position 
                float start_x = float.Parse(values[5]) * spacing;
                float start_y = float.Parse(values[6]) * spacing;
                float start_z = float.Parse(values[7]) * spacing;

                // store end position
                float end_x = float.Parse(values[8]) * spacing;
                float end_y = float.Parse(values[9]) * spacing;
                float end_z = float.Parse(values[10]) * spacing;

                mode = values[11];

                Vector3 start_pos = Utils.rotateYZ(new Vector3(start_x, start_y, start_z));
                Vector3 end_pos;
                // the condition indicates dying drone exiting the cube
                if (float.Parse(values[10]) == -1.0f)
                {
                    end_pos = _chargedDroneStartPos;
                    // store the facing direction of exit drone
                    _exitDroneFaceDirection = (end_pos - start_pos).normalized;
                    _exitDroneFaceDirection.y = 0.0f; // don't want rotation on y axis
                }
                else
                {
                    end_pos = Utils.rotateYZ(new Vector3(end_x, end_y, end_z));
                }

                _droneInfo.Add(droneId, new Tuple<Vector3, string, Vector3, Vector3, string>
                    (curr_pos, color, start_pos, end_pos, mode));
            }

        }
        // when subtracting cubes, it will be in file -> secondary mode,
        // adding cubes, it will be in fill -> secondary mode
        // ignore the mode change for both add & sub when switching to secondary
        
        // switch camera position if subtract command line has more than 4 arguments 
        // move the camera before the next drone movement
        // string cubeLine = _cubeCommandLines[0];
        // string[] cubeLineValues = cubeLine.Split(',');
        // if (mode != _currMode && mode == "file")
        // {
        //     _currMode = mode;
        // }
        // if (mode != _currMode && mode == "run" && cubeLineValues.Length > 4)
        // {
        //     UpdateCube();
        //     UpdateCube();
        //     _currMode = mode;
        // }
        // else if (_currMode == null || (mode != _currMode && mode != "secondary" && mode != "file")) 
        // {
        //     UpdateCube();
        //     _currMode = mode;
        // }
        // else if (mode != _currMode && mode == "run" && cubeLineValues.Length <= 4)
        // {
        //     UpdateCube();
        //     UpdateCube();
        //     _currMode = mode;
        // }
        if (_currMode == null || (mode != _currMode && mode != "secondary")) 
        {
            UpdateCube();
            _currMode = mode;
        }

        if (_currAllPossibleMode == null || mode != _currAllPossibleMode)
        {   
            _currAllPossibleMode = mode;
            // Debug.Log(_currAllPossibleMode);
        }
        GenerateChargingDrones(); // add in the 997 and 998 
    }
    */

    void UpdateDrones()
    {   
        // bool to delete drone 999 for _droneInfo dict if exists
        bool drone999 = false;
        // remaining id on list gets removed
        List<int> allDronesID = new List<int>(_drones.Keys);

        foreach (var kvp in _droneInfo)
        {   
            int droneId = kvp.Key;
            // retrieve data
            Vector3 position = kvp.Value.Item1;
            Vector3 startPos = kvp.Value.Item3;
            Vector3 endPos = kvp.Value.Item4;
            // update min/max position of lattice cube (need adjustment for subtraction) 
            // minMaxPos.Item1 = Vector3.Min(minMaxPos.Item1, position);
            // minMaxPos.Item2 = Vector3.Max(minMaxPos.Item2, position);


            // prepare color
            string color = kvp.Value.Item2;
            // use Sytem.Drawing.Color to render color with string
            SystemDrawingColor myDrawingColor = SystemDrawingColor.FromName(color);
            // convert to UnityEngine.Color
            UnityEngine.Color myUnityColor = new UnityEngine.Color(myDrawingColor.R / 255f, 
                                                                myDrawingColor.G / 255f, 
                                                                myDrawingColor.B / 255f, 
                                                                myDrawingColor.A / 255f);
            
            if (!_drones.ContainsKey(droneId))
            {   
                if (droneId == 997)
                {
                    // Debug.Log("making 997 drone");
                }
                // Create new drone if it doesn't exist
                GameObject newDrone = Instantiate(dronePrefab, position, Quaternion.identity);
                // make it visible on scene
                newDrone.GetComponentInChildren<MeshRenderer>().enabled = true;
                // assign color 
                // https://discussions.unity.com/t/change-color-of-an-individual-game-object/415513/5
                newDrone.GetComponentInChildren<MeshRenderer>().material.color = myUnityColor;
                newDrone.name = "Drone_" + droneId;
                _drones[droneId] = newDrone;
                allDronesID.Remove(droneId);
                // make an arrow associated to the drone
                CreateNewArrow(droneId, startPos, endPos, color);
                // if (droneId == 999)
                // {
                //     Debug.Log("instantiating 999 drone in _drones");
                // }
                // Debug.Log("if the droneId is not in _drones: ");
                // Debug.Log(droneId);
            }
            else
            {
                if (droneId == 997)
                {
                    // Debug.Log("we only update not destroying");
                }
                // Move existing drone
                _drones[droneId].transform.position = position;
                // update color
                _drones[droneId].GetComponentInChildren<MeshRenderer>().material.color = myUnityColor;
                allDronesID.Remove(droneId);
                // update an arrow associated to the drone
                UpdateArrow(droneId, startPos, endPos, color);
            }
            

            // destroy drained drone
            if (droneId == 999 && _drones.ContainsKey(droneId)){
                // Debug.Log("destoying drone999 in _drones");
                // delete game object
                Destroy(_drones[droneId]);
                // delete kvp
                _drones.Remove(droneId);
                drone999 = true;
                // destroy arrow 
                DestroyArrow(droneId);
            }
        }

        foreach (int drone_id in allDronesID)
        {
            // delete game object
            Destroy(_drones[drone_id]);
            if (drone_id == 997)
            {
                // Debug.Log(" you are destroying 997");
            }
            // delete kvp
            _drones.Remove(drone_id);
            
            // Destroy Arrow
            DestroyArrow(drone_id);
        }
        
        // take drone 999 out of dictionary keeping info for updating drone
        if (drone999)
        {
            // Debug.Log("removed 999 drone from _droneinfo");
            _droneInfo.Remove(999);
        }
    }

    /**
     * After each fetched CSV, smooth linear interpolation from start to end position for drones
     * Assuming all data entering this state is prepared else where*
     */
    bool 
    InterpolateToEndPosition()
    {   
        bool nextCsv = false;
        _lerpVal += Time.deltaTime * interpolationSpeed;
        // clip between 0 and 1 
        _lerpVal = Mathf.Clamp01(_lerpVal);
        
        if (_lerpVal == 1)
        {
            nextCsv = true;
        }
        foreach (var kvp in _droneInfo)
        {   
            int droneId = kvp.Key;
            if (droneId == 999)
            {
                // Debug.Log("drone 999 still in _droneInfo");
            }
            Vector3 startPos = _droneInfo[droneId].Item3;
            Vector3 endPos = kvp.Value.Item4;
            // if end position is charged Drone start position from csv, it's exiting simulation (base on ReadCsv update already changed the exiting drone to the
            // charged drone start position
            if (endPos == _chargedDroneStartPos)
            {  
                // CHANGE -- moved the _exitDroneFaceDirection to here.
                _exitDroneFaceDirection = (endPos - startPos).normalized;
                _exitDroneFaceDirection.y = 0.0f;
                
                
                // rotate the drone exiting to end up facing the direction like charging drone faces
                Quaternion exitDroneRotation = Quaternion.LookRotation(_exitDroneFaceDirection);
                Quaternion chargingDroneRotation = Quaternion.LookRotation(_chargedDroneFaceDirection);
                _drones[droneId].transform.rotation = Quaternion.Slerp(exitDroneRotation, chargingDroneRotation, _lerpVal);
                // Debug.Log(exitDroneRotation);
                // Debug.Log(chargingDroneRotation);
            }
            Vector3 currPos = Vector3.Lerp(startPos, endPos, _lerpVal);
            string color = kvp.Value.Item2;
            
            _drones[droneId].transform.position = currPos;
            
            // make arrow the whole length if autoCam view
            // if (cameraManager.GetAutoCameraActive())
            // {
            UpdateArrow(droneId, startPos, endPos, color);
            // }
            // else
            // {
            //     UpdateArrow(droneId, currPos, endPos, color);
            // }
            // // make arrow length follow the drone position if drone view
            // UpdateArrow(droneId, startPos, endPos, color);
        }
        
        return nextCsv;
    }

    // new arrows name matches the droneID 
    void CreateNewArrow(int droneId, Vector3 startPos, Vector3 endPos, string color)
    {
        GameObject newDroneArrow = Instantiate(droneArrowPrefab, startPos, Quaternion.identity);
        ArrowDrawer arrowDrawer = newDroneArrow.GetComponent<ArrowDrawer>();
        arrowDrawer.CreateArrow(droneId, startPos, endPos, color);
        newDroneArrow.name = "Arrow_" + droneId;
        _droneArrows[droneId] = newDroneArrow;
    }

    void UpdateArrow(int droneId, Vector3 startPos, Vector3 endPos, string color)
    {
       // ArrowDrawer arrowDrawer = _droneArrows[droneId].GetComponent<ArrowDrawer>();
       // arrowDrawer.UpdateArrow(startPos, endPos);
       _droneArrows[droneId].GetComponent<ArrowDrawer>().UpdateArrow(startPos, endPos, color);
    }

    void DestroyArrow(int droneId)
    {   
        _droneArrows[droneId].GetComponent<ArrowDrawer>().DestroyArrow();
        // delete game object
        Destroy(_droneArrows[droneId]);
        // delete kvp
        _droneArrows.Remove(droneId);
    }

    void MakeChargingBlock()
    {   
        Vector3 expectStartPos = Vector3.zero;
        Vector3 expectEndPos = new Vector3(1.0f * spacing, 0.0f, 0.0f);
        
        GameObject chargingBlock = Instantiate(chargingBlockPrefab, expectStartPos, Quaternion.identity);
        chargingBlock.GetComponentInChildren<MeshRenderer>().enabled = true;
        chargingBlock.name = "ChargingBlock";
        ChargingBlock cb = chargingBlock.GetComponent<ChargingBlock>(); // access script of Prefab to set block for rendering
        cb.setLocation(expectStartPos, expectEndPos, _chargedDroneStartPos);
    }

    void GenerateChargingDrones()
    {
        // Debug.Log("does this function ever get called??");
        bool update;
        if (_droneInfo[_startDroneId].Item3 == _droneInfo[_startDroneId].Item4)
        {
            update = false;
        }
        else
        {
            update = true;
        }
        // use ID 997 & 998 for these always occuring drones
        // Vector3 startPos997 = new Vector3(1.0f * spacing, -1.0f * spacing, -0.5f * spacing); // the drone thats charged
        Vector3 startPos997 = _chargedDroneStartPos;
        Vector3 endPos997 = Vector3.zero;
        
        // Vector3 startPos998 = new Vector3(1.0f * spacing, 0.0f, -1.0f * spacing); // the drone that died
        // Vector3 endPos998 = new Vector3(0.0f, 0.0f, -1.0f * spacing);
        
        // check if two drones are already instantiated
        // if (GameObject.Find("Drone_998") == null && GameObject.Find("Drone_997") == null)
        if (GameObject.Find("Drone_997") == null)
        {
            // Debug.Log("no 997 drone");
            _MakeNewDrone(997, "Orange", startPos997);
            // _MakeNewDrone(998, "Red", startPos998);
            CreateNewArrow(997, startPos997, endPos997, "Orange");
            // CreateNewArrow(998, startPos998, endPos998, "Red");
        }

        if (!_droneInfo.ContainsKey(998) || !_droneInfo.ContainsKey(997))
        {
            if (update)
            {
                Debug.Log("add 997 into _droneInfo");
                // _droneInfo.Add(998, new Tuple<Vector3, string, Vector3, Vector3, string>
                //     (startPos998, "Red", startPos998, endPos998, "run"));
                _droneInfo.Add(997, new Tuple<Vector3, string, Vector3, Vector3, string>
                    (startPos997, "Orange", startPos997, endPos997, "run"));
            }
            else
            {
                // Debug.Log("made it here else case!");
                // _droneInfo.Add(998, new Tuple<Vector3, string, Vector3, Vector3, string>
                //     (startPos998, "Red", startPos998, startPos998, "run"));
                _droneInfo.Add(997, new Tuple<Vector3, string, Vector3, Vector3, string>
                    (startPos997, "Orange", startPos997, startPos997, "run"));
            }
        }
    }

    void ResetChargingDrones()
    {   
        // Vector3 startPos997 = new Vector3(1.0f * spacing, -1.0f * spacing, -0.5f * spacing); // the drone thats charged
        // Vector3 startPos998 = new Vector3(1.0f * spacing, 0.0f, -1.0f * spacing); // the drone that died
        // _drones[998].transform.position = startPos998;
        // Vector3 startPos997 = _chargingDronePos;
        _drones[997].transform.position = _chargedDroneStartPos;
    }
    
    /**
     * update cube logic 
     */
    void UpdateCube(bool baseCube=false)
    {   
        /*if (_cubeCommandLines == null)
        {
            return;
        }*/
        
        // render for the original first eight drone's cube
        if (baseCube)
        {
            GameObject newCube = Instantiate(defaultCubePrefab, CubeHelper.cubeCenter(Vector3.zero, spacing), Quaternion.identity);
            newCube.GetComponentInChildren<MeshRenderer>().enabled = true; // render with intial load, toggle event can't detect
            newCube.name = "Cube_0_0_0";
            newCube.transform.localScale = new Vector3(2 * spacing, 2 * spacing, 2 * spacing); // spacing is for drone, and cube is 2 times the size of drone distance
            _cubes.Add(newCube);
            CubeHelper.UpdateCornerPosAddCube(CubeHelper.cubeCenter(Vector3.zero, spacing), 
                                        2 * spacing, 
                                            ref _uprightfrontPos, 
                                            ref _upleftbackPos,
                                            ref _botcenterPos,
                                            ref _boxCenter);
            return;
        }
        
        /*
        string cubeLine = _cubeCommandLines[0];
        _currCubeCommand = cubeLine; // attach the current command line for camera to use
        _cubeCommandLines.RemoveAt(0);
        string[] values = cubeLine.Split(',');
        string mode = values[0];
        // the cube commands (i.e. sub, 2, 0, 0, etc. only longer than four elements if x, y are added and x, z or y, z, specifying the plane to cut out)
        */

        _currCubeCommand = csvLogic.ReadChanges(); // same thing as string cubeLine = _cubeCommandLines[0];
        csvLogic.UpdateChanges(); // same thing as _cubeCommandLines.RemoveAt(0)
        string[] values = _currCubeCommand.Split(',');
        string mode = values[0];
        
        
        bool camPlaneDeduct;
        if (values.Length > 4)
        {
            camPlaneDeduct = true;
        }
        else
        {
            camPlaneDeduct = false;
        }
        _camTrackMode = mode;
        if (mode == "add") // when adding cubes, it will be in fill -> secondary mode (for drones' CSV)
        {   
            var x = float.Parse(values[1]);
            var y = float.Parse(values[2]);
            var z = float.Parse(values[3]);
            var bottomLeftBackCornerDronePos = Utils.rotateYZ(new Vector3(x, y, z));
            GameObject newCube;
            // switch between two different cube prefabs to optimize efficiency (rather than changing material of the prefab)
            if (CubeHelper.DefaultSaddleCube(x, z, y))
            {
                newCube = Instantiate(defaultCubePrefab, CubeHelper.cubeCenter(bottomLeftBackCornerDronePos, spacing), Quaternion.identity);
            }
            else
            {
                newCube = Instantiate(invertCubePrefab, CubeHelper.cubeCenter(bottomLeftBackCornerDronePos, spacing), Quaternion.identity);
            }
            // GameObject newCube = Instantiate(defaultCubePrefab, CubeHelper.cubeCenter(new Vector3(x, y, z), spacing), Quaternion.identity);
            newCube.GetComponentInChildren<MeshRenderer>().enabled = _cubeToggleState;
            newCube.name = "Cube_" + values[1] + "_" + values[2] + "_" + values[3];
            newCube.transform.localScale = new Vector3(2 * spacing, 2 * spacing, 2 * spacing); // spacing is for drone, and cube is 2 times the size of drone distance
            _cubes.Add(newCube);
            // update cube corners for camera to use
            CubeHelper.UpdateCornerPosAddCube(CubeHelper.cubeCenter(bottomLeftBackCornerDronePos, spacing), 
                                        2 * spacing, 
                                        ref _uprightfrontPos,
                                        ref _upleftbackPos,
                                        ref _botcenterPos,
                                        ref _boxCenter);
            
        }
        else if (mode == "sub") 
        {   
            // Debug.Log("Cube_" + values[1] + "_" + values[2] + "_" + values[3] + " Cube to destroy");
            _cubeToRemove = GameObject.Find("Cube_" + values[1] + "_" + values[2] + "_" + values[3]);
            if (camPlaneDeduct)
            {
                CubeHelper.UpdateCornerPosSubCube(_cubeToRemove.transform.position,
                    2 * spacing,
                    ref _uprightfrontPos,
                    ref _upleftbackPos,
                    ref _botcenterPos,
                    ref _boxCenter,
                    values[4]);
            }

            _cubeToRemove.GetComponentInChildren<MeshRenderer>().enabled = false;
            Destroy(_cubeToRemove); // destroy GameObject
            var index = _cubes.IndexOf(_cubeToRemove);
            _cubes.RemoveAt(index); // remove from list 
            
        }
        
        // else if (mode == "run" && camPlaneDeduct)
        // {
        //     CubeHelper.UpdateCornerPosSubCube(_cubeToRemove.transform.position,
        //         2 *spacing,
        //         ref _uprightfrontPos,
        //         ref _upleftbackPos,
        //         ref _botcenterPos,
        //         ref _boxCenter,
        //         values[4]);
        //     Destroy(_cubeToRemove); // destroy GameObject
        //     var index = _cubes.IndexOf(_cubeToRemove);
        //     _cubes.RemoveAt(index); // remove from list 
        // }

    }
    
    /**
     * update cube render
     */
    private void _updateCubeRender(bool isOn)
    {   
        _cubeToggleState = isOn;
        if (isOn)
        {
            foreach (GameObject cube in _cubes)
            {
                cube.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        else
        {
            foreach (GameObject cube in _cubes)
            {
                cube.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    private void _MakeNewDrone(int droneId, string color, Vector3 position)
    {
        Color myUnityColor = Utils.CreateColorFromCsvName(color);
        // Create new drone if it doesn't exist
        GameObject newDrone = Instantiate(dronePrefab, position, Quaternion.identity);
        // make it visible on scene
        newDrone.GetComponentInChildren<MeshRenderer>().enabled = true;
        // assign color 
        // https://discussions.unity.com/t/change-color-of-an-individual-game-object/415513/5
        newDrone.GetComponentInChildren<MeshRenderer>().material.color = myUnityColor;
        newDrone.name = "Drone_" + droneId;
        _drones[droneId] = newDrone;
    }

    private void _StoreDroneRotation(int droneId)
    {   
        _droneRotations.Clear();
        foreach (var droneIdKey in _droneInfo.Keys)
        {
            Quaternion startRotation = _drones[droneIdKey].transform.rotation;
            Vector3 targetForward = (_droneInfo[droneIdKey].Item4 - _droneInfo[droneIdKey].Item3).normalized;
            targetForward.y = 0.0f;
            Quaternion targetRotation = startRotation;
            if (targetForward != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(targetForward);
            }

            // Debug.Log(startRotation + " " + targetRotation);
            _droneRotations.Add(droneIdKey, new Tuple<Quaternion, Quaternion>(startRotation, targetRotation));
            _droneRotatePriority = true;
        }
    }
    private void _FinishRotateDrone(int droneId)
    {   
        // Debug.Log(droneId);
        rotationTimer += Time.deltaTime;
        var t = Mathf.Clamp01(rotationTimer / rotationDuration);
        // targetForward = newDir;
        // for those when the drone keeps the same arrow direction
        foreach (var i in _droneRotations.Keys)
        {
            float angleDiff = Quaternion.Angle(_droneRotations[i].Item1, _droneRotations[i].Item2);
            if (angleDiff < 0.1f) // threshold in degrees
            {
                // Debug.Log("don't have to rotate for drone");
                _drones[i].transform.rotation = _droneRotations[i].Item2;
                // _droneRotatePriority = false;
            }
            // Debug.Log(_droneRotations[i].Item1 + " " + _droneRotations[i].Item2 + " " + angleDiff);
            // Debug.Log("droneId:" + i);
            _drones[i].transform.rotation = Quaternion.Slerp(
                _droneRotations[i].Item1,
                _droneRotations[i].Item2,
                t
            );
            if (t >= 1.0f)
            {
                rotationTimer = 0.0f;
                // _currForward = targetForward;
                _drones[i].transform.rotation = _droneRotations[i].Item2;
                _droneRotatePriority = false;
            }
        }
    }
    
}