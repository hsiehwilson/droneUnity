using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;
using System;

using UnityEngine.Serialization;
using UnityEngine.UI;

public class CSVLogic:MonoBehaviour
{
    // _currentChangesLine is taking over _cubeCommandLines field in
    // DroneManager for Cube related parsing logic
    private string _cubeRenderFolderPath = "CubeCommandAssets/changes.csv"; // Path to changes.csv
    private List<string> _currentChangesLine; // its the whole line (e.g."add, 0, 0, 0") as the string
    private int _counter = 0;
    private string _streamingFilePath = Path.Combine(Application.streamingAssetsPath, $"zzz_{0.ToString("D10")}.csv");
    private Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>> _currentStreamingFile = new Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>>();
    private int _currentFrame = 0;
    private bool _firstFrame = true;

    public float spacing = 5.0f;
    
    // invoke this function in Start()

    void Start()
    {   
        
    }
    public void FetchChangesCSV()
    {
        Debug.Log(Path.Combine(Application.dataPath,
            _cubeRenderFolderPath));
        if (File.Exists(Path.Combine(Application.dataPath,
                _cubeRenderFolderPath)))
        {
            string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath,
                _cubeRenderFolderPath));
            Debug.Log(lines);
            _currentChangesLine = lines.ToList();
        }
        else
        {
            Debug.Log("File does not exist");
        }
        
    }

    public string ReadChanges()
    {
        Debug.Log(_counter);
        return _currentChangesLine[_counter];
    }

    public string UpdateChanges()
    {
        _counter += 1;
        
        return _currentChangesLine[_counter];
    }

    public Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>> FetchStreaming()
    {
        
        // Debug.Log(filePath);
        if (File.Exists(_streamingFilePath))
        {
            ParseStreaming();
        }
        else
        {
            Debug.Log("File does not exist");
            return null;
        }

        return _currentStreamingFile;
    }
    
    // first return value is the _startDroneId, second return value is the droneId for the exiting drone
    private (int, int) ParseStreaming()
    {
        string[] lines = File.ReadAllLines(_streamingFilePath);
        string mode = "run";
        int _startDroneId = -2;
        int _exitDroneId = -2;
        // only clear when there's elements in dictionary
        if (_currentStreamingFile.Count > 0)
        {
            _currentStreamingFile.Clear();
        }

        
        foreach (string line in lines)
        {
            string[] values = line.Split(',');
            // sotre current drone position
            int droneId = int.Parse(values[0]);
            float x = float.Parse(values[1]) * spacing;
            float y = float.Parse(values[2]) * spacing;
            float z = float.Parse(values[3]) * spacing;

            string color = values[4];

            Vector3 currPos = Utils.rotateYZ(new Vector3(x, y, z));
            if (currPos == Vector3.zero)
            {
                _startDroneId = droneId; // keep track of start drone 
            }
            // stagnant data (drones not moving or outdated)
            if (values.Length == 6 && values[0] != "drone_id") // Skip header
            {

                _currentStreamingFile.Add(droneId,
                    new Tuple<Vector3, string, Vector3, Vector3, string>(currPos, color, currPos, currPos,
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

                Vector3 startPos = Utils.rotateYZ(new Vector3(start_x, start_y, start_z));
                Vector3 endPos = Vector3.zero;
                // the condition indicates dying drone exiting the cube
                if (float.Parse(values[10]) == -1.0f)
                {
                    // Debug.Log(droneId);
                    _exitDroneId = droneId;
                    //endPos = _chargedDroneStartPos;
                    //// store the facing direction of exit drone
                    //_exitDroneFaceDirection = (endPos - startPos).normalized;
                    //_exitDroneFaceDirection.y = 0.0f; // don't want rotation on y axis
                }
                else
                {
                    endPos = Utils.rotateYZ(new Vector3(end_x, end_y, end_z));
                }

                _currentStreamingFile.Add(droneId, new Tuple<Vector3, string, Vector3, Vector3, string>
                    (currPos, color, startPos, endPos, mode));
            }

        }
        return (_startDroneId, _exitDroneId);
    }
    

    public Dictionary<int, Tuple<Vector3, string, Vector3, Vector3, string>> ReadStreaming()
    {
        return _currentStreamingFile;
    }
    
    // intialization also calls this function 
    public (int, int) UpdateStreaming()
    {   
        //Vector3 exitDroneDir;
        //int _startDroneId = 0;
        //int _exitDroneId = 0;
        _currentFrame += 1;
        _streamingFilePath = Path.Combine(Application.streamingAssetsPath, $"zzz_{_currentFrame.ToString("D10")}.csv");
        // Debug.Log(_streamingFilePath);
        return ParseStreaming();
     
    }




}

