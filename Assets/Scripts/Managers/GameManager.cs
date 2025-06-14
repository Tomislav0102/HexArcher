using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public bool MyTurn() => playerTurnNet.Value == playerDatas[NetworkManager.Singleton.IsHost ? 0 : 1].playerColor;
    int _counterMisses;

    [Title("References", TitleAlignment = TitleAlignments.Centered)]
    [SerializeField] Camera camMain;
    [HideInInspector] public Transform camMainTransform;
    [HideInInspector] public SoFactionData[] playerDatas;
    public UiManager uImanager;
    public SkyboxHeightFogManager skyboxHeightFogManager;
    public AudioManager audioManager;
    public ArrowManager arrowManager;
    public BowRack[] bowRacks;
    public BotManager botManager;
    public GridManager gridManager;
    public CampaignManager campaignManager;
    public PoolManager poolManager;
    public WindManager windManager;
    public DrawTrajectory drawTrajectory;

    [Title("Layers", TitleAlignment = TitleAlignments.Centered)]
    public LayerMask layTargets;
    public LayerMask layForTrajectory;

    public NetworkVariable<byte> _skyboxIndexNet = new NetworkVariable<byte>(byte.MaxValue);

    [PropertySpace(SpaceAfter = 0, SpaceBefore = 10)]
    [Title("Players...", TitleAlignment = TitleAlignments.Centered)]
    [SerializeField] GameObject prefabPlayer;
    public NetworkObject[] bowTablesNet;
    [SerializeField] MeshRenderer[] playerMeshMarker;
    [SerializeField] GameObject[] scoreVisualMarker;

    [Title("Public network variables", TitleAlignment = TitleAlignments.Centered)]
    public NetworkVariable<GenDifficulty> difficultyNet = new NetworkVariable<GenDifficulty>();
    public NetworkVariable<PlayerColor> playerTurnNet = new NetworkVariable<PlayerColor>();
    public NetworkVariable<PlayerColor> playerVictoriousNet = new NetworkVariable<PlayerColor>();
    public NetworkList<byte> gridTileStatesNet;
    public NetworkList<sbyte> gridValuesNet;
    public NetworkVariable<float> windAmountNet = new NetworkVariable<float>();
    public NetworkVariable<bool> trajectoryVisible = new NetworkVariable<bool>();
    
    [Title("Public network variables - players", TitleAlignment = TitleAlignments.Centered)]
    public NetworkList<uint> scoresNet;
    public NetworkList<NetPlayerEquipment> equipmentNet;
    public NetworkList<NetPlayerDisplay> playerDisplayNet;
    
    private void Awake()
    {
        Instance = this;
        playerDatas = Resources.LoadAll<SoFactionData>("SoFactionData");
        poolManager.Init();
        Physics.gravity = windManager.gravityVector;

        gridTileStatesNet = new NetworkList<byte>(new List<byte>());
        gridValuesNet = new NetworkList<sbyte>(new List<sbyte>());
        camMainTransform = camMain.transform;

        scoresNet = new NetworkList<uint>(new List<uint>() { 0, 0 });
        equipmentNet = new NetworkList<NetPlayerEquipment>(new List<NetPlayerEquipment>() { new NetPlayerEquipment(), new NetPlayerEquipment() });
        playerDisplayNet = new NetworkList<NetPlayerDisplay>(new List<NetPlayerDisplay>() { new NetPlayerDisplay(), new NetPlayerDisplay() });
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gridManager.Init();

        if (IsServer)
        {
            GameObject level = null;
            if (Utils.GameType == MainGameType.Singleplayer)
            {
                switch (Utils.SinglePlayerType)
                {
                    case SpType.Endless:
                        Utils.GameStarted?.Invoke();
                        break;
                    case SpType.Campaign:
                        Utils.GameStarted?.Invoke();
                        campaignManager.Init();
                        level = campaignManager.NextLevel();
                        break;
                    case SpType.Practice:
                        break;
                }
            }
            gridManager.ChooseGrid(level);
            
            difficultyNet.Value = (GenDifficulty)PlayerPrefs.GetInt(Utils.Difficulty_Int);
            _skyboxIndexNet.Value = (byte)Random.Range(0, skyboxHeightFogManager.Mats().Length);

            playerVictoriousNet.Value = PlayerColor.Undefined;
            playerTurnNet.OnValueChanged += NetVarEv_PlayerTurnChange;
            playerVictoriousNet.OnValueChanged += NetVarEv_PlayerVictorious;
            windAmountNet.Value = PlayerPrefs.GetFloat(Utils.WindAmount_Fl);
            trajectoryVisible.Value = PlayerPrefs.GetInt(Utils.TrajectoryVisible_Int) == 1;

            NetworkManager.Singleton.OnClientDisconnectCallback += CallEv_ClientDisconnected;
        }
        else
        {
            gridManager.GridUseNetworkVariables();
            Utils.Activation(botManager.gameObject, false);
            ChangeVisualMarkers_EveryoneRpc(playerTurnNet.Value);
            
        }
        Utils.FadeOut?.Invoke(true);
        audioManager.PlaySFX(audioManager.gameStarted);

        windManager.WindChange(float.MinValue, windAmountNet.Value);
        windAmountNet.OnValueChanged += windManager.WindChange;
        drawTrajectory.showTrajectory = trajectoryVisible.Value;
        skyboxHeightFogManager.InitSkybox(_skyboxIndexNet.Value);
    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= CallEv_ClientDisconnected;
        }
    }

    IEnumerator CountdownMultiplayer()
    {
        int seconds = 0;
        string st = string.Empty;
        switch (Utils.GameType)
        {
            case MainGameType.Singleplayer:
                break;
            case MainGameType.Multiplayer:
                seconds = Utils.WaitTimeStartGame;
                st = "Bot is active";
                break;
        }
        WaitForSeconds wait = Utils.GetWait(1);
        while (seconds > 1)
        {
            seconds--;
            uImanager.SetDisplays(UiDisplays.InfoStart, $"Bot will spawn in {seconds} seconds");
            yield return wait;
        }
        Utils.GameStarted?.Invoke();
        uImanager.SetDisplays(UiDisplays.InfoStart, st);
    }





    #region CALL EVENTS
    void CallEv_ClientDisconnected(ulong obj)
    {
      //  print($"client {obj} disconnected, num of clients is {NetworkManager.Singleton.ConnectedClients.Count}");
        if (obj == NetworkManager.Singleton.LocalClientId) return;
        StartCoroutine(CountdownMultiplayer());
        bowTablesNet[1].ChangeOwnership(OwnerClientId);
        bowRacks[1].HideRack();
    }

    private void NetVarEv_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
       // if (Utils.GameType == MainGameType.Multiplayer && NetworkManager.Singleton.ConnectedClients.Count > 1) PlayerVictorious_EveryoneRpc(newValue);
        PlayerVictorious_EveryoneRpc(newValue);
        
        botManager.EndBot();
    }

    [Rpc(SendTo.Everyone)]
    void PlayerVictorious_EveryoneRpc(PlayerColor newValue)
    {
        switch (newValue)
        {
            case PlayerColor.Blue:
                Content(IsServer ? GenResult.Win : GenResult.Lose);
                break;
            case PlayerColor.Red:
                Content(!IsServer ? GenResult.Win : GenResult.Lose);
                break;
            case PlayerColor.None:
                Content(GenResult.Draw);
                break;
        }
        
        void Content(GenResult gameResult)
        {
            DatabaseManager dm = Launch.Instance.myDatabaseManager;

            int totalMatches = dm.GetValAndCastTo<int>(MyData.TotalMatches);
            totalMatches++;
            dm.observableData[MyData.TotalMatches] = totalMatches.ToString();
            int currentXp = dm.GetValAndCastTo<int>(MyData.Xp);

            switch (gameResult)
            {
                case GenResult.Win:
                    audioManager.PlaySFX(audioManager.win);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreLeaderboardGlobalValues.x;
                    int wins = dm.GetValAndCastTo<int>(MyData.Wins);
                    wins++;
                    dm.observableData[MyData.Wins] = wins.ToString();
                    PlayerLeveling.AddToXp(GenResult.Win);
                    dm.observableData[MyData.Coins] += Utils.CoinsWin;
                    break;
                
                case GenResult.Lose:
                    audioManager.PlaySFX(audioManager.loose);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreLeaderboardGlobalValues.y;
                    int defeats = dm.GetValAndCastTo<int>(MyData.Defeats);
                    defeats++;
                    print(defeats);
                    dm.observableData[MyData.Defeats] = defeats.ToString();
                    PlayerLeveling.AddToXp(GenResult.Lose);
                    dm.observableData[MyData.Coins] += Utils.CoinsDefeat;
                    break;
                case GenResult.Draw:
                    audioManager.PlaySFX(audioManager.draw);
                    PlayerLeveling.AddToXp(GenResult.Draw);
                    dm.observableData[MyData.Coins] += Utils.CoinsDraw;
                    break;
            }
            dm.UploadMyScore();
            int xpEarned = dm.GetValAndCastTo<int>(MyData.Xp) - currentXp;
            uImanager.SetDisplays(UiDisplays.XpEarned, $"You've earned {xpEarned} XP!");

            int myLeague = dm.GetValAndCastTo<int>(MyData.League);
            if (dm.GetValAndCastTo<int>(MyData.Wins) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].z
                && dm.GetValAndCastTo<int>(MyData.TotalMatches) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].x) LeagueChange(GenChange.Increase);
            else if (dm.GetValAndCastTo<int>(MyData.Defeats) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].y) LeagueChange(GenChange.Decrease);

            void LeagueChange(GenChange change)
            {
                int prevLeague = myLeague; //debug only
                dm.observableData[MyData.Wins] = dm.observableData[MyData.Defeats] = "0";
                if (change != GenChange.Increase) myLeague++;
                else myLeague--;
                if (myLeague < 0) myLeague = 0;
                
                dm.observableData[MyData.League] = myLeague.ToString();
                print($"League changed from {(League)prevLeague} to {(League)myLeague}");
            }
        }


    }

    private void NetVarEv_PlayerTurnChange(PlayerColor previousValue, PlayerColor newValue)
    {
        ChangeVisualMarkers_EveryoneRpc(newValue);
    }
    #endregion

    #region REGISTRATIONS
    [Rpc(SendTo.Server)]
    public void RegisterPlayerDisplay_ServerRpc(int index, FixedString128Bytes playerName, uint level, byte league, int leaderboard)
    {
        NetPlayerDisplay netPlayerDisplay = new NetPlayerDisplay()
        {
            name = playerName,
            level = level,
            league = league,
            leaderboard = leaderboard
        };
        playerDisplayNet[index] = netPlayerDisplay;
    }
    [Rpc(SendTo.Server)]
    public void RegisterPlayerEquipment_ServerRpc(int index, byte[] equipmentIndices)
    {
        NetPlayerEquipment netPlayerEquipment = new NetPlayerEquipment()
        {
            bowIndex = equipmentIndices[0],
            headIndex = equipmentIndices[1],
            handsIndex = equipmentIndices[2],
        };
        equipmentNet[index] = netPlayerEquipment;
    }


    [Rpc(SendTo.Server)]
    public void SpawnPlayers_ServerRpc(ulong obj)
    {
        int numOfPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        StopAllCoroutines();
        botManager.EndBot();
        NetworkObject no = NetworkManager.Singleton.ConnectedClientsList[numOfPlayers - 1].PlayerObject;
        if (no != null)
        {
            return;
        }
        
        if (numOfPlayers > 1)
        {
            Utils.GameStarted?.Invoke();
            botManager.EndBot();
        }
        else
        {
            StartCoroutine(CountdownMultiplayer());
        }

        GameObject go = Instantiate(prefabPlayer);
        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj, true);
    }

    [Rpc(SendTo.Server)]
    public void ChangeOwnershipOfBowTable_ServerRpc(ulong obj) => bowTablesNet[1].ChangeOwnership(obj);
    #endregion

    #region GAME FLOW
    [Rpc(SendTo.Server)]
    public void SetGridTileStateNet_ServerRpc(byte ord, byte value) => gridTileStatesNet[ord] = value;

    [Rpc(SendTo.Server)]
    public void SetGridValuesNet_ServerRpc(byte ord, sbyte value) => gridValuesNet[ord] = value;

    [Rpc(SendTo.Everyone)]
    void ChangeVisualMarkers_EveryoneRpc(PlayerColor newValue)
    {
        Utils.ActivateOneArrayElement(scoreVisualMarker,(int)newValue);
        
        for (int i = 0; i < 4; i++)
        {
            playerMeshMarker[i].material = playerDatas[(int)newValue].matMain;
        }
    }

    [Rpc(SendTo.Server)]
    public void NextPlayer_ServerRpc(bool countGridMisses = true, string caller = "")
    {
        if (playerVictoriousNet.Value != PlayerColor.Undefined) return;
        arrowManager.ClearAllArrows_EveryoneRpc();
        if (countGridMisses)
        {
            _counterMisses++;
            if (_counterMisses >= 4)
            {
                print("4 misses in a row, match is over");
                DecideVictor();
                return;
            }
        }
        else _counterMisses = 0;
        int val = (int)playerTurnNet.Value;
        val = (1 + val) % 2;
        playerTurnNet.Value = (PlayerColor)val;
        //  print($"next player is { playerTurnNet.Value }, called by {caller}");
    }

    [Rpc(SendTo.Server)]
    public void Scoring_ServerRpc()
    {
        if ((int)(playerVictoriousNet.Value) < 2) return; //is gameover
        bool useConsole = false;
        int[] tempScores = new int[2];
        List<ParentHex> takenHex = gridManager.AllTilesByType(TileState.Taken);
        if (useConsole) print($"taken hex {takenHex.Count}");
        foreach (ParentHex item in takenHex)
        {
            switch (item.CurrentValue)
            {
                case > 0:
                    tempScores[0] += item.CurrentValue;
                    if (useConsole) print("plus");
                    break;
                case < 0:
                    tempScores[1] -= item.CurrentValue;
                    if (useConsole) print("minus");
                    break;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            scoresNet[i] = (uint)Mathf.Abs(tempScores[i]);
        }

        //check game over
        int freePoints = gridManager.NumOfTilesByType(TileState.Free) * 10;
        if (freePoints == 0)
        {
            print("all tiles are taken");
            DecideVictor();
        }
        else if (scoresNet[0] > scoresNet[1] + freePoints || scoresNet[1] > scoresNet[0] + freePoints)
        {
            print("impossible to win");
            DecideVictor();
        }
    }

    void DecideVictor()
    {
        if (scoresNet[0] > scoresNet[1])
        {
            playerVictoriousNet.Value = PlayerColor.Blue;
        }
        else if (scoresNet[0] < scoresNet[1])
        {
            playerVictoriousNet.Value = PlayerColor.Red;
        }
        else playerVictoriousNet.Value = PlayerColor.None;
    }

    #endregion

    #region DEBUGS

    [ContextMenu("GameType")]
    void Metoda1()
    {
        print(Utils.GameType);
        if (Utils.GameType == MainGameType.Singleplayer)
        {
            print(Utils.SinglePlayerType);
            print($"Campaign level is {Utils.CampLevel}");
        }
    }

    [ContextMenu("ConnectedClients.Count")]
    void Metoda2() => print(NetworkManager.Singleton.ConnectedClients.Count);

    [ContextMenu("Print all playerprefs")]
    void Metoda3() => Utils.DisplayAllPlayerPrefs();

    [ContextMenu("Change wind")]
    void Metoda4()
    {
        windAmountNet.Value = Random.Range(-0.5f, 0.5f);
        // print($"wind is {windAmountNet.Value * CONST_WINDSCALE}");
        print($"wind is {windAmountNet.Value * 20}");
    }

    [ContextMenu("Next player")]
    void Metoda5()
    {
        NextPlayer_ServerRpc(false, "context menu");
    }

    [ContextMenu("SpawnPlayerAndRegisterRpc")]
    void Metoda6() => SpawnPlayers_ServerRpc(NetworkManager.Singleton.LocalClientId);

    [ContextMenu("Blue wins")]
    void Metoda7() => playerVictoriousNet.Value = PlayerColor.Blue;

    [ContextMenu("Red wins")]
    void Metoda8() => playerVictoriousNet.Value = PlayerColor.Red;

    [ContextMenu("hex data")]
    void Metoda11()
    {
        string st = "all states and values:\n";
        for (int i = 0; i < 100; i++)
        {
            if (gridTileStatesNet[i] == 0 && gridValuesNet[i] == 0) continue;
            st += $"state is {(TileState)gridTileStatesNet[i]}, value is {gridValuesNet[i]}, ordinal is {i}\n";
        }
        print(st);
    }

    [ContextMenu("Scores")]
    void Metoda12()
    {
        for (int i = 0; i < 2; i++)
        {
            print(scoresNet[i]);
        }
    }

    [ContextMenu("test upload")]
    void Metoda15()
    {
        int index = NetworkManager.Singleton.IsHost ? 0 : 1;
        RegisterPlayerDisplay_ServerRpc(index, index == 0 ? "host":"client", 
            (uint)(index == 0 ? 1 : 10), 
            (byte)(index == 0 ? 2 : 20), 
            index == 0 ? 3 : 30);
    }
    [ContextMenu("test display")]
    void Metoda16()
    {
        for (int i = 0; i < playerDisplayNet.Count; i++)
        {
            print($"{i} - {playerDisplayNet[i].name}");
            print($"{i} - {playerDisplayNet[i].level}");
            print($"{i} - {playerDisplayNet[i].league}");
            print($"{i} - {playerDisplayNet[i].leaderboard}");
        }
    }


    #endregion
}


