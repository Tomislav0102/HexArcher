using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUi : MonoBehaviour
{
    DatabaseManager _db;
    [SerializeField] Button btnPrev, btnNext;
    [SerializeField] Transform parDisplays, parMyEntry;
    GameObject[] _displaysGo;
    TextMeshProUGUI[] _displaysOrd, _displaysNames, _displaysScores;
    int _totalOnSingleScreen, _screenCounter;

    
    private void Awake()
    {
        _db = DatabaseManager.Instance;
        _totalOnSingleScreen = parDisplays.childCount;
        _displaysGo = new GameObject[_totalOnSingleScreen];
        _displaysOrd = new TextMeshProUGUI[_totalOnSingleScreen];
        _displaysNames = new TextMeshProUGUI[_totalOnSingleScreen];
        _displaysScores = new TextMeshProUGUI[_totalOnSingleScreen];
        for (int i = 0; i < _totalOnSingleScreen; i++)
        {
            _displaysGo[i] = parDisplays.GetChild(i).gameObject;
            _displaysOrd[i] = _displaysGo[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            _displaysNames[i] = _displaysGo[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            _displaysScores[i] = _displaysGo[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }
    }


    private void OnEnable()
    {
        btnPrev.onClick.AddListener(() => ButtonMethodNewList(GenSide.Left));
        btnNext.onClick.AddListener(() => ButtonMethodNewList(GenSide.Right));

        btnPrev.interactable = false;
        btnNext.interactable = false;
        Utils.DeActivateGo(parDisplays.gameObject);
        Utils.ActivateOneArrayElement(_displaysGo);
        if (DatabaseManager.Instance.dataLoaded)
        {
            Utils.ActivateGo(parDisplays.gameObject);
            int maxCounter = _totalOnSingleScreen;
            if (_db.names.Count <= _totalOnSingleScreen) maxCounter = _db.names.Count;
            else
            {
                btnPrev.interactable = true;
                btnNext.interactable = true;
            }

            for (int i = 0; i < maxCounter; i++)
            {
                _displaysOrd[i].text = (i + 1).ToString();
                _displaysNames[i].text = _db.names[i];
                _displaysScores[i].text = _db.scores[i].ToString();
                Utils.ActivateGo(_displaysGo[i]);
            }
            _screenCounter = _totalOnSingleScreen;
            
            UpdateMyEntry();
        }

        Utils.DatabaseClientSynced += UpdateMyEntry;
    }
    private void OnDisable()
    {
        btnPrev.onClick.RemoveAllListeners();
        btnNext.onClick.RemoveAllListeners();
        Utils.DatabaseClientSynced -= UpdateMyEntry;
    }
    
    void UpdateMyEntry()
    {
        int myPos = _db.GetMyPositionOnLeaderboard(Utils.MyIdLeaderboard());
        if (myPos > 0)
        {
            Utils.ActivateGo(parMyEntry.gameObject);
            parMyEntry.GetChild(0).GetComponent<TextMeshProUGUI>().text = (myPos + 1).ToString();
            parMyEntry.GetChild(1).GetComponent<TextMeshProUGUI>().text = _db.names[myPos];
            parMyEntry.GetChild(2).GetComponent<TextMeshProUGUI>().text = _db.scores[myPos].ToString();
        }
        else
        {
            Utils.DeActivateGo(parMyEntry.gameObject);
        }
    }

    void ButtonMethodNewList(GenSide side)
    {
        int all = _db.names.Count;
        Utils.ActivateOneArrayElement(_displaysGo);
        switch (side)
        {
            case GenSide.Left:
                _screenCounter -= _totalOnSingleScreen;
                if (_screenCounter < _totalOnSingleScreen)  _screenCounter = GetMaxNumOfScreens();
                break;

            case GenSide.Right:
                if (_screenCounter >= all) _screenCounter = _totalOnSingleScreen;
                else _screenCounter += _totalOnSingleScreen;
                break;
        }

        int prevVal = _screenCounter - _totalOnSingleScreen;
        int nextVal = _screenCounter;
        if (_screenCounter > all)
        {
            int diff = _screenCounter - all;
            nextVal = prevVal + (_totalOnSingleScreen - diff);
        }
        for (int i = prevVal; i < nextVal; i++)
        {
            _displaysOrd[i - prevVal].text = (i + 1).ToString();
            _displaysNames[i - prevVal].text = _db.names[i].ToString();
            _displaysScores[i - prevVal].text = _db.scores[i].ToString();
            Utils.ActivateGo(_displaysGo[i - prevVal]);
        }


        int GetMaxNumOfScreens()
        {
            int val = 0;
            while (val < all)
            {
                val += _totalOnSingleScreen;
            }
            return val;
        }
    }

}


