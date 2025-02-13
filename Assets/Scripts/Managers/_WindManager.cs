using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class WindManager
{
    public Vector3 gravityVector;
    [Sirenix.OdinInspector.ReadOnly] public Vector3 windVector;
    const int CONST_WindScale = 20;
    [SerializeField] TextMeshProUGUI displayWindValue;
    [SerializeField] Transform windIcon;
    [SerializeField] Cloth flagCloth;

    
    public void WindChange(float previousValue, float newValue)
    {
        if (Mathf.Abs(newValue) < 0.1f)
        {
            windIcon.localEulerAngles = Vector3.zero;
            flagCloth.damping = 0.3f;
        }
        else
        {
            int a = newValue < 0f ? -1 : 1;
            windIcon.localEulerAngles = -a * 90f * Vector3.forward;
            flagCloth.damping = 0f;
        }
        windVector = new Vector3(newValue * CONST_WindScale * 2f, 0f, 0f);
        displayWindValue.text = $"{Mathf.Abs(windVector.x).ToString("F0")} km/h";
        flagCloth.externalAcceleration = windVector;
        Physics.gravity = gravityVector + windVector;
    }

}