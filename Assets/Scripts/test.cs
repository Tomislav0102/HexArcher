using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using Unity.Collections;
using UnityEngine;

public class test : MonoBehaviour
{
    public int prvi;
    public uint rezultat;


    void Update()
    {
        rezultat = (uint)Mathf.Abs(prvi) + 1;
    }
}