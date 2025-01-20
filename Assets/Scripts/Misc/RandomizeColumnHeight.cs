using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeColumnHeight : MonoBehaviour
{
    public float amount = 0.1f;
    public float width = 3f;
    const float CONST_StartHeight = 17.55f;
    public Material[] materials;

    [ContextMenu("RandomizeMe")]
    void RandomizeMe()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
           transform.GetChild(i).localScale = new Vector3(width, CONST_StartHeight * Random.Range(1-amount, 1+amount), width);
           transform.GetChild(i).GetChild(1).GetComponent<Renderer>().material = materials[Random.Range(0, materials.Length)];
        }
    }
    [ContextMenu("ResetMe")]
    void ResetMe()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localScale = new Vector3(width, CONST_StartHeight, width);
        }
    }
}
