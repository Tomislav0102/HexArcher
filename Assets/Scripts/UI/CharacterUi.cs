using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CharacterUi : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI coinsText, totalText, winsText, defeatsText, scoresText, lbText, xpText, levelText, 
        leagueText, equipmentText;
    [SerializeField] GameObject  window;

    DatabaseManager _dm;
    void Awake()
    {
        _dm = Launch.Instance.myDatabaseManager;
    }

    public void Refresh()
    {
        coinsText.text = $"Gold: {_dm.observableData[MyData.Coins]}";
        totalText.text = $"Total matches: {_dm.observableData[MyData.TotalMatches]}";
        winsText.text = $"Wins: {_dm.observableData[MyData.Wins]}";
        defeatsText.text = $"Defeats: {_dm.observableData[MyData.Defeats]}";
        scoresText.text = $"Score : {_dm.observableData[MyData.LeaderboardScore]}";
        lbText.text = $"Leaderboard: {_dm.observableData[MyData.LeaderboardRank]}";
        xpText.text = $"Xp : {_dm.observableData[MyData.Xp]}";
        PlayerLeveling.CalculateLevelFromXp(out int level, out _);
        levelText.text = $"Level : {level}";
        leagueText.text = $"League : {System.Enum.Parse<League>(_dm.observableData[MyData.League])}";
        SoBow[] bows = Resources.LoadAll<SoBow>("SoBow");
        SoItem[] head = Resources.LoadAll<SoItem>("SoHead");
        SoItem[] hands = Resources.LoadAll<SoItem>("SoHands");
        equipmentText.text = $"Bow : {bows[_dm.GetValAndCastTo<int>(MyData.BowIndex)].itemName}\n" +
                             $"Head : {head[_dm.GetValAndCastTo<int>(MyData.HeadIndex)].itemName}\n" +
                             $"Hands : {hands[_dm.GetValAndCastTo<int>(MyData.HandsIndex)].itemName}\n";
    }
}
