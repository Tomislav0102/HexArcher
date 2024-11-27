using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleInUI : MonoBehaviour
{
    [SerializeField] bool showInfo;
    [SerializeField] Transform parConsole;
    TextMeshProUGUI[] _consoleLines;
    int _counterConsoleLine;
    Canvas _canvas;
    private void Awake()
    {
        _consoleLines = Utils.AllChildren<TextMeshProUGUI>(parConsole);
        CleaerConsole();
        _canvas = GetComponent<Canvas>();
        if(_canvas.renderMode == RenderMode.WorldSpace && _canvas.worldCamera == null) _canvas.worldCamera = Camera.main;
    }
    private void Start()
    {
        parConsole.gameObject.SetActive(showInfo);
    }

    public void CleaerConsole()
    {
        for (int i = 0; i < _consoleLines.Length; i++)
        {
            _consoleLines[i].text = " ";
        }
    }
    public void Toggle()
    {
        showInfo = !showInfo;
        parConsole.gameObject.SetActive(showInfo);
    }

    void OnEnable()
    {
        Application.logMessageReceived += LogCallback;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
    }


    void LogCallback(string logString, string stackTrace, LogType type)
    {
        Color col = Color.white;
        switch (type)
        {
            case LogType.Error:
                col = Color.red;
                break;
            case LogType.Assert:
                break;
            case LogType.Warning:
                col = Color.yellow;
                break;
            case LogType.Log:
                break;
            case LogType.Exception:
                col = Color.cyan;
                break;
        }
        NewLineInConsole(/*"LogCallback - " + */logString, col);
    }

    void NewLineInConsole(string st, Color col)
    {
        for (int i = _consoleLines.Length - 1; i > 0; i--)
        {
            _consoleLines[i].text = _consoleLines[i - 1].text;
        }
       // _consoleLines[0].text = $"{_counterConsoleLine} - {st} -{System.DateTime.Now.TimeOfDay}";
        _consoleLines[0].text = $"{_counterConsoleLine} - {st}";
        _consoleLines[0].color = col;
        _counterConsoleLine++;
    }

}
