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
    public string sceneName = "TestScene";
    public static int NumPublic;
    static int numPrivate;


    [Button("Restart scene")]
    void M1() => SceneManager.LoadScene(sceneName);

    [Button("Increase public var")]
    void M2() => NumPublic++;
    [Button("Increase private var")]
    void M3() => numPrivate++;

    [Button("Display")]
    void M4()
    {
        Debug.Log(NumPublic);
        Debug.Log(numPrivate);
    }
}