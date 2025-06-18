using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightIntensityText : MonoBehaviour
{
    private Slider _slider;
    private Text _text;

    void Awake()
    {
        _slider = GetComponentInParent<Slider>();
        _text = GetComponent<Text>();
    }
    // Start is called before the first frame update
    void Start()
    {
        UpdateText(_slider.value);
        _slider.onValueChanged.AddListener(UpdateText);
    }

    // Update is called once per frame
    void UpdateText(float val)
    {
        var finalVal = Mathf.Round(_slider.value * 100f) / 100f;
        _text.text = finalVal.ToString();
    }
}
