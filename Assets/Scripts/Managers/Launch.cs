using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour
{
    public static Launch Instance;
    public MyLobbyManager myLobbyManager;
    public MySceneManager mySceneManager;
    public DatabaseManager myDatabaseManager;
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }
}

