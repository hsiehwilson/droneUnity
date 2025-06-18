using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargingBlock : MonoBehaviour
{
    private float _centerToTop = 0.5f;
    private float _widthX = 1.0f;
    private float _widthZ = 1.0f;
    private float _droneThickness = 0.3f; // estimate of drone thickness to look like drone lands on the charging block
    
   public void setLocation(Vector3 startPos, Vector3 endPos, Vector3 chargeDronePos)
    {
        // displace cylinder center to the middle of startPos and endPos
        transform.position = startPos + (endPos - startPos) / 2.0f;
        float distX = endPos.x - startPos.x; // distance in x direction between the drones
        _widthX = Mathf.Abs(distX); 
        transform.localScale = new Vector3(1.5f * _widthX, 0.5f, 1.2f * _widthZ); // stretch block to be additional 0.25x bigger on each side
        // translate for y & z direction base on charged drone desired location
        var gapOfDroneAndBlockSurface = -_centerToTop - _droneThickness;
        transform.Translate(0, gapOfDroneAndBlockSurface + chargeDronePos.y, chargeDronePos.z, Space.World);
        
    }
}
