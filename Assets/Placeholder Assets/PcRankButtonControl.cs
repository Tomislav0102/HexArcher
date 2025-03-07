using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PcRankButtonControl : MonoBehaviour
{
    [SerializeField] Transform parButtonsRank;
    Button[] _rankButtons;
    Outline[] _rankOutlines;

    void Awake()
    {
        _rankButtons = Utils.AllChildren<Button>(parButtonsRank);
        _rankOutlines = Utils.AllChildren<Outline>(parButtonsRank);
    }

    void Start()
    {
        for (int i = 0; i < _rankOutlines.Length; i++)
        {
            _rankOutlines[i].enabled = false;
        }
        ButtonMethodRank(Launch.Instance.myDatabaseManager.GetValFromKeyEnum<int>(MyData.League));
    }

    void OnEnable()
    {
        for (int i = 0; i < _rankButtons.Length; i++)
        {
            int index = i;
            _rankButtons[i].onClick.AddListener(() => ButtonMethodRank(index));
        }
    }    
    void OnDisable()
    {
        for (int i = 0; i < _rankButtons.Length; i++)
        {
            _rankButtons[i].onClick.RemoveAllListeners();
        }
    }



    void ButtonMethodRank(int index)
    {
        Launch.Instance.myDatabaseManager.observableData[MyData.League] = index.ToString();
        for (int i = 0; i < _rankOutlines.Length; i++)
        {
            _rankOutlines[i].enabled = false;
        }
        _rankOutlines[index].enabled = true;
    }
}
