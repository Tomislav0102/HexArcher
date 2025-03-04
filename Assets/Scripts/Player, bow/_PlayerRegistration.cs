using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerRegistration
{
    PlayerControl[] _players= new PlayerControl[2];
    TextMeshPro[] _displays= new TextMeshPro[2];

    public void AddPlayer(PlayerControl player, bool isHost)
    {
        int index = isHost ? 0 : 1;
        _players[index] = player;
        _displays[index] = _players[index].displayNameTr.GetComponent<TextMeshPro>();
    }
    public void RemovePlayer(PlayerControl player)
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == player) _players[i] = null;
        }
    }


    public void FillDisplay()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == null) continue;
            
            string myName = "";
            string levelDisplay = "Level - ";
            string rankDisplay = "Rank - ";
            string lbDisplay = "Leaderboard - ";
            
            myName = GameManager.Instance.nameNet[i].Value;
            levelDisplay += GameManager.Instance.levelsNet[i].ToString();
            rankDisplay += ((League)GameManager.Instance.leagueNet[i]).ToString();
            if ((League)GameManager.Instance.leagueNet[i] != League.Challenger) rankDisplay = rankDisplay.Remove(rankDisplay.Length - 1, 1);
            if (GameManager.Instance.leaderboardNet[i] < 0) lbDisplay = string.Empty;
            else
            {
                int rank = GameManager.Instance.leaderboardNet[i];
                lbDisplay += (rank + 1).ToString();
            }
            
            _displays[i].text = myName + "\n" + levelDisplay + "\n" + rankDisplay + "\n" + lbDisplay;
            _players[i].name = $"Igrach {_players[i].GetComponent<NetworkObject>().OwnerClientId}";
        }
    }
    public bool HasSecondPlayer() => _players[1] != null;

    public void GameOver()
    {
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] != null)
            {
                Utils.Activation(_players[i].bowCurrent.gameObject, false);
            }
        }
    }
    int TargetPlayerIndex(PlayerControl playerOpposing)
    {
        int targetPlayerIndex = 0;
        for (int i = 0; i < 2; i++)
        {
            if (_players[i] == playerOpposing) targetPlayerIndex = (i + 1) % 2;
        }
        return targetPlayerIndex;
    }
}