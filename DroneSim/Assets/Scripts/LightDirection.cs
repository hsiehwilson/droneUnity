using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightDirection : MonoBehaviour
{
    public CameraManager camManger;

    // Start is called before the first frame update
    void Start()
    {
        if (camManger.autoCameraActive)
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(70f, 30f, 0f);
        }

    }
}
