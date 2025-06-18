using System;
using System.Collections;
using System.Collections.Generic;
using SystemDrawingColor = System.Drawing.Color;
using System.IO;
using UnityEngine;

// Generic useful functions
public static class Utils
{
    public static UnityEngine.Color CreateColorFromCsvName(string color)
    {
        // use Sytem.Drawing.Color to render color with string
        SystemDrawingColor myDrawingColor = SystemDrawingColor.FromName(color);
        // convert to UnityEngine.Color
        UnityEngine.Color myUnityColor = new UnityEngine.Color(myDrawingColor.R / 255f, 
            myDrawingColor.G / 255f, 
            myDrawingColor.B / 255f, 
            myDrawingColor.A / 255f);
        return myUnityColor;
    }
    
    public static int FindClosestPoint(Vector3 currentPosition, List<Vector3> positions)
    {
        int closestInd = 0;
        float minDistanceSqr = (positions[0] - currentPosition).sqrMagnitude;

        for(int i = 0; i < positions.Count; i++)
        {
            float distanceSqr = (positions[i] - currentPosition).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestInd = i;
            }
        }

        return closestInd;
    }
    
    // swap y & z axis coordinates from streamingCSVs to unity coordinates
    public static Vector3 rotateYZ(Vector3 v)
    {  
        Matrix4x4 matrix = new Matrix4x4();
        // Set the first column (x-axis)
        matrix.SetColumn(0, new Vector4(1, 0, 0, 0));
        // Set the second column (y-axis)
        matrix.SetColumn(1, new Vector4(0, 0, 1, 0));
        // Set the third column (z-axis)
        matrix.SetColumn(2, new Vector4(0, 1, 0, 0));
        // Set the position (translation)
        matrix.SetColumn(3, new Vector4(0, 0, 0, 1));
        
        var newPos = matrix.MultiplyPoint(v);
        return newPos;
    }
    
    // given roll pitch yaw in order (unit: degrees), return new position the given position will end up
    public static Vector3 rotateCustom(Vector3 v, float roll, float pitch, float yaw)
    {
        Quaternion rot = Quaternion.Euler(roll, pitch, yaw);
        return rot * v;
    }
    
}
