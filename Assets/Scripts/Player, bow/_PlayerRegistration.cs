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
        // string myName = "";
        // string levelDisplay = "Level - ";
        // string rankDisplay = "Leaderboard - ";
        // if (isHost)
        // {
        //     myName = GameManager.Instance.nameBlueNet.Value.ToString();
        //     levelDisplay += GameManager.Instance.leveBlueNet.Value.ToString();
        //     if (GameManager.Instance.leaderboardBlueNet.Value < 0) rankDisplay = string.Empty;
        //     else
        //     {
        //         int rank = GameManager.Instance.leaderboardBlueNet.Value;
        //         rankDisplay += (rank + 1).ToString();
        //     }
        // }
        // else
        // {
        //     myName = GameManager.Instance.nameRedNet.Value.ToString();
        //     levelDisplay += GameManager.Instance.leveRedNet.Value.ToString();
        //     if (GameManager.Instance.leaderboardRedNet.Value < 0) rankDisplay = string.Empty;
        //     else
        //     {
        //         int rank = GameManager.Instance.leaderboardRedNet.Value;
        //         rankDisplay += (rank + 1).ToString();
        //     }
        //
        // }
        // if(_displays[index] != null) _displays[index].text = myName + "\n" + levelDisplay+ "\n" + rankDisplay;
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
            if (i == 0)
            {
                myName = GameManager.Instance.nameBlueNet.Value.ToString();
                levelDisplay += GameManager.Instance.leveBlueNet.Value.ToString();
                rankDisplay += GameManager.Instance.rankingBlueNet.Value;
                if (GameManager.Instance.leaderboardBlueNet.Value < 0) lbDisplay = string.Empty;
                else
                {
                    int rank = GameManager.Instance.leaderboardBlueNet.Value;
                    lbDisplay += (rank + 1).ToString();
                }
            }
            else
            {
                myName = GameManager.Instance.nameRedNet.Value.ToString();
                levelDisplay += GameManager.Instance.leveRedNet.Value.ToString();
                rankDisplay += GameManager.Instance.rankingRedNet.Value;
                if (GameManager.Instance.leaderboardRedNet.Value < 0) lbDisplay = string.Empty;
                else
                {
                    int rank = GameManager.Instance.leaderboardRedNet.Value;
                    lbDisplay += (rank + 1).ToString();
                }

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
                Utils.DeActivateGo(_players[i].bowControl.gameObject);
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