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

    public void Init()
    {
        gm = GameManager.Instance;
        gm.playerVictoriousNet.OnValueChanged += NetVar_PlayerVictorious;
        if (Utils.CampLevel >= levels.Length) Utils.CampLevel = 0;
        gm.uImanager.displayCampaignInfoStart.text = $"Campaign\nLevel {Utils.CampLevel + 1}";
    }

    void NetVar_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
        string st = string.Empty;
        gm.uImanager.displayCampaignInfoEnd.enabled = true;
        switch (newValue)
        {
            case PlayerColor.Blue:
                
                Utils.CampLevel++;
                if (Utils.CampLevel >= levels.Length)
                {
                    //end campaign
                    st = "Well done! You've completed the campaign!";
                }
                else st = $"Level {Utils.CampLevel} finished successfully! {levels.Length - Utils.CampLevel} levels remain...";
                break;
            default:
                st = "Not good enough... Try again?";
                break;
        }
        gm.uImanager.displayCampaignInfoEnd.text = st;
    }

    public GameObject NextLevel() => levels[Utils.CampLevel];
}
