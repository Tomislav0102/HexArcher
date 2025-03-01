using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class test : MonoBehaviour
{
    public string first, second;


    [Button]
    void M()
    {
        second = first.Remove(first.Length - 1, 1);
        print("done");
    }
}