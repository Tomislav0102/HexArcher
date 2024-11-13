using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class ChangeResolution : MonoBehaviour
{
    [SerializeField] UniversalRenderPipelineAsset urpAsset;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI display;
    string DisplayString()
    {
        string val = urpAsset.renderScale.ToString("0.00");
        return $"Texture detail is {val}";
    }

    private void Start()
    {
        slider.value = urpAsset.renderScale;
        display.text = DisplayString();
    }

    public void SliderChange()
    {
        urpAsset.renderScale = slider.value;
        display.text = DisplayString();
    }

}
