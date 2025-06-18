using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightIntensity : MonoBehaviour
{   
    public Slider slider;
    private Light _light;
    void Awake()
    {
        // _slider = GetComponentInParent<Slider>();
        _light = GetComponent<Light>();
    }
    // Start is called before the first frame update
    void Start()
    {   
        UpdateLight(slider.value);
        slider.onValueChanged.AddListener(UpdateLight);
    }

    // Update is called once per frame
    void UpdateLight(float val)
    {
        _light.intensity = val;
    }
}
