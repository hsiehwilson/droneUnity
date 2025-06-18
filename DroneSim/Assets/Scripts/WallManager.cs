using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// compile this after CameraManger in Project Structure

public class WallManager : MonoBehaviour
{   
    public GameObject brickPrefab;
    public CameraManager camManger; 
    public int bricksPerSide = 10000;
    public float brickLength = 10f;  // Along wall (X or Z)
    public float brickThickness = 0.5f;  // Wall thickness (how far it extends outward)
    public float offsetFromCenter = 1000f; // Distance from center to inner edge of the wall
    public int bricksHigh = 80;            // number of vertical layers
    public float brickHeight = 20f;      // height of each brick
    private float _floorHeight = -10f;
    
    void Start()
    {   
        // brickPrefab.GetComponentInChildren<MeshRenderer>().enabled = false; // don't render the foundational brick\
        if (camManger.autoCameraActive)
        {
            return;
        }
        float halfWallLength = bricksPerSide * brickLength * 0.5f;
        float halfBrick = brickThickness * 0.5f;

        for (int h = 0; h < bricksHigh; h++)
        {
            float y = h * brickHeight;
            // Side 1: Front (Z+)
            BuildWall(
                center: new Vector3(0, _floorHeight + y, offsetFromCenter + halfBrick),
                direction: Vector3.right,
                length: halfWallLength,
                facing: Vector3.forward
            );

            // Side 2: Right (X+)
            BuildWall(
                center: new Vector3(offsetFromCenter + halfBrick, _floorHeight + y, 0),
                direction: Vector3.back,
                length: halfWallLength,
                facing: Vector3.right
            );

            // Side 3: Back (Z-)
            BuildWall(
                center: new Vector3(0, _floorHeight + y, -(offsetFromCenter + halfBrick)),
                direction: Vector3.left,
                length: halfWallLength,
                facing: Vector3.back
            );

            // Side 4: Left (X-)
            BuildWall(
                center: new Vector3(-(offsetFromCenter + halfBrick), _floorHeight + y, 0),
                direction: Vector3.forward,
                length: halfWallLength,
                facing: Vector3.left
            );
        }
    }

    void BuildWall(Vector3 center, Vector3 direction, float length, Vector3 facing)
    {
        for (int i = 0; i < bricksPerSide; i++)
        {
            float offset = (i - bricksPerSide / 2f + 0.5f) * brickLength;
            Vector3 pos = center + direction.normalized * offset;
            Quaternion rot = Quaternion.LookRotation(facing);
            brickPrefab.transform.localScale = new Vector3(brickLength, brickHeight, brickThickness);
            Instantiate(brickPrefab, pos, rot);
        }
    }
}