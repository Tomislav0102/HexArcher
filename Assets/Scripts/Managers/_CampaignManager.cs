using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class CampaignManager
{
    GameManager gm;
    [SerializeField] GameObject[] levels;

    string UiStartText()
    {
        return $"Campaign\nLevel {Utils.CampLevel + 1}";
    }
    public void Init()
    {
        gm = GameManager.Instance;
        gm.playerVictoriousNet.OnValueChanged += NetVar_PlayerVictorious;
        gm.uImanager.displayCampaignInfo.text = UiStartText();
    }

    void NetVar_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
        switch (newValue)
        {
            case PlayerColor.Blue:
                Utils.CampLevel++;
                if (Utils.CampLevel >= levels.Length)
                {
                    //end campaign
                }
                gm.uImanager.displayCampaignInfo.text = UiStartText();
                break;
            case PlayerColor.Red:
                break;
        }
    }

    public GameObject NextLevel() => levels[Utils.CampLevel];
}
