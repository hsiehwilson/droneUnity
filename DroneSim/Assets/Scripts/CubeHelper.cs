using System.Collections;
using System.Collections.Generic;
using UnityEngine;  
using System.IO;
using System.Linq;
using UnityEditor;

public static class CubeHelper
{
    public static List<string> FetchCubeCsv(string filePath)
    {   
        // Debug.Log(filePath);
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            List<string> listStrings = lines.ToList();
            return listStrings;
        }
        else
        {
            Debug.Log("File does not exist");
            return null;
        }
    }
    

    public static Vector3 cubeCenter(Vector3 bottomLeftBackCornerDronePos, float spacing, float roll, float pitch, float yaw)
    {
        // defualt cube is 1 unit distance in all x, y, z, direction. 
        var leftX = (bottomLeftBackCornerDronePos.x - 0.5f) * spacing;
        var rightX = (bottomLeftBackCornerDronePos.x + 1.5f) * spacing;
        var downY = (bottomLeftBackCornerDronePos.y - 0.5f) * spacing;
        var upY = (bottomLeftBackCornerDronePos.y + 1.5f) * spacing;
        var frontZ = (bottomLeftBackCornerDronePos.z - 0.5f) * spacing;
        var backZ = (bottomLeftBackCornerDronePos.z + 1.5f) * spacing;
        var centerX = leftX + (rightX - leftX) * 0.5f;
        var centerY = downY + (upY - downY) * 0.5f;
        var centerZ = frontZ + (backZ - frontZ) * 0.5f;
        Vector3 originPos = new Vector3(centerX, centerY, centerZ);
        return Utils.rotateCustom(originPos, roll, pitch, yaw);
    }
    
    /*
     * given center of cube and scaling of the cube, calculate the diagonal corners of the face with greatest y
       __________ B       Point A & B 
       |        |  
       |________|
       A
       Params: cubeCenter, scaling, 
     */
    public static void UpdateCornerPosAddCube(Vector3 cubeCenter, float scaling, ref Vector3 uprightfrontPos,
        ref Vector3 upleftbackPos, ref Vector3 botCenterPos, ref Vector3 boxCenter, float roll, float pitch, float yaw)
    {
        var distToEdge = scaling / 2.0f;
        Vector3 cubeUpRightFrontPos = new Vector3(cubeCenter.x + distToEdge, cubeCenter.y + distToEdge, cubeCenter.z + distToEdge);
        Vector3 cubeUpLeftBackPos = new Vector3(cubeCenter.x - distToEdge, cubeCenter.y + distToEdge, cubeCenter.z - distToEdge);
        Vector3 cubeBotCenterPos = new Vector3(cubeCenter.x, cubeCenter.y - distToEdge, cubeCenter.z);
        // cubeUpRightFrontPos = Utils.rotateCustom(cubeUpRightFrontPos, roll, pitch, yaw);
        // cubeUpLeftBackPos = Utils.rotateCustom(cubeUpLeftBackPos, roll, pitch, yaw);
        // cubeBotCenterPos = Utils.rotateCustom(cubeBotCenterPos, roll, pitch, yaw);
        
        // update bottom y position
        if (cubeBotCenterPos.y < botCenterPos.y)
        {
            botCenterPos.y = cubeBotCenterPos.y;
        }
        // update up y position
        if (cubeUpRightFrontPos.y > uprightfrontPos.y)
        {
            upleftbackPos.y = cubeUpLeftBackPos.y;
            // Debug.Log(cubeUpLeftBackPos);
            uprightfrontPos.y = cubeUpRightFrontPos.y;
        }
         
        if (cubeUpRightFrontPos.x > uprightfrontPos.x)
        {
            uprightfrontPos.x = cubeUpRightFrontPos.x;
        }

        if (cubeUpRightFrontPos.z > uprightfrontPos.z)
        {
            uprightfrontPos.z = cubeUpRightFrontPos.z;
        }
        if (cubeUpLeftBackPos.x < upleftbackPos.x)
        {
            upleftbackPos.x = cubeUpLeftBackPos.x;
        }
        if (cubeUpLeftBackPos.z < upleftbackPos.z)
        {
            upleftbackPos.z = cubeUpLeftBackPos.z;
        }
        
        
        // recalculate the box Center
        var x = upleftbackPos.x + (uprightfrontPos.x - upleftbackPos.x) / 2.0f;
        var y = botCenterPos.y + (uprightfrontPos.y - botCenterPos.y) / 2.0f;
        var z = upleftbackPos.z + (uprightfrontPos.z - upleftbackPos.z) / 2.0f;
        
        boxCenter = new Vector3(x, y, z);
    }
    
    public static void UpdateCornerPosSubCube(Vector3 cubeCenter, float scaling, ref Vector3 uprightfrontPos,
        ref Vector3 upleftbackPos, ref Vector3 botCenterPos, ref Vector3 boxCenter, string axis)
    {
        var distToEdge = scaling / 2.0f;

        switch (axis)
        {   
            // shrinking plane direction is the x axis direction and so on, (the direction vector)
            case "x":
                Vector3 cubeUpRightFrontPos = new Vector3(cubeCenter.x + distToEdge, cubeCenter.y + distToEdge, cubeCenter.z + distToEdge);
                upleftbackPos.x = cubeUpRightFrontPos.x;
                break;
            case "-x":
                // Vector3 cubeUpLeftBackPos = new Vector3(cubeCenter.x - distToEdge, cubeCenter.y + distToEdge, cubeCenter.z - distToEdge);
                // uprightfrontPos.x = cubeUpLeftBackPos.x;
                uprightfrontPos.x = cubeCenter.x - distToEdge;
                break;
            // for y and z, Unity coordinate system has y and z flipped in comparison to passed in command 
            case "y":
                // Vector3 cubeUpLeftBackPos = new Vector3(cubeCenter.x - distToEdge, cubeCenter.y + distToEdge, cubeCenter.z + distToEdge);
                // uprightfrontPos.z = cubeUpLeftBackPos.z;
                upleftbackPos.z = cubeCenter.z + distToEdge;
                break;
            case "-y":
                // Vector3 cubeUpLeftBackPos = new Vector3(cubeCenter.x - distToEdge, cubeCenter.y + distToEdge, cubeCenter.z - distToEdge);
                uprightfrontPos.z = cubeCenter.z - distToEdge;
                break;
            case "z":
                // Vector3 cubeBotCenterPos = new Vector3(cubeCenter.x, cubeCenter.y + distToEdge, cubeCenter.z);
                uprightfrontPos.y = cubeCenter.y + distToEdge;
                upleftbackPos.y = cubeCenter.y + distToEdge;
                break;
            case "-z":
                // Vector3 cubeBotCenterPos = new Vector3(cubeCenter.x, cubeCenter.y - distToEdge, cubeCenter.z);
                uprightfrontPos.y = cubeCenter.y - distToEdge;
                upleftbackPos.y = cubeCenter.y - distToEdge;
                break;
            default:
                break;
        }
        // Vector3 cubeUpRightFrontPos = new Vector3(cubeCenter.x + distToEdge, cubeCenter.y + distToEdge, cubeCenter.z + distToEdge);
        // Vector3 cubeUpLeftBackPos = new Vector3(cubeCenter.x - distToEdge, cubeCenter.y + distToEdge, cubeCenter.z - distToEdge);
        // Vector3 cubeBotCenterPos = new Vector3(cubeCenter.x, cubeCenter.y - distToEdge, cubeCenter.z);
        
        // // update bottom y position
        // if (cubeBotCenterPos.y < botCenterPos.y)
        // {
        //     botCenterPos.y = cubeBotCenterPos.y;
        // }
        // // update up y position
        // if (cubeUpRightFrontPos.y > uprightfrontPos.y)
        // {
        //     upleftbackPos.y = cubeUpLeftBackPos.y;
        //     // Debug.Log(cubeUpLeftBackPos);
        //     uprightfrontPos.y = cubeUpRightFrontPos.y;
        // }
        //  
        // if (cubeUpRightFrontPos.x > uprightfrontPos.x)
        // {
        //     uprightfrontPos.x = cubeUpRightFrontPos.x;
        // }
        //
        // if (cubeUpRightFrontPos.z > uprightfrontPos.z)
        // {
        //     uprightfrontPos.z = cubeUpRightFrontPos.z;
        // }
        // if (cubeUpLeftBackPos.x < upleftbackPos.x)
        // {
        //     upleftbackPos.x = cubeUpLeftBackPos.x;
        // }
        // if (cubeUpLeftBackPos.z < upleftbackPos.z)
        // {
        //     upleftbackPos.z = cubeUpLeftBackPos.z;
        // }
        
        
        // recalculate the box Center
        var x = upleftbackPos.x + (uprightfrontPos.x - upleftbackPos.x) / 2.0f;
        var y = botCenterPos.y + (uprightfrontPos.y - botCenterPos.y) / 2.0f;
        var z = upleftbackPos.z + (uprightfrontPos.z - upleftbackPos.z) / 2.0f;
        boxCenter = new Vector3(x, y, z);
        // Debug.Log(boxCenter);
    }
    
    // true as the default saddle cube color, false as the inverted saddle cube color (checkerboard pattern)
    public static bool DefaultSaddleCube(float xPos, float yPos, float zPos)
    {   
        // the cube is added with increment of 2 in all directions for new cube
        return ((xPos + yPos + zPos) % 4 == 0);
    }
}
