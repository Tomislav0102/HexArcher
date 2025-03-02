using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public bool MyTurn() => playerTurnNet.Value == playerDatas[NetworkManager.Singleton.IsHost ? 0 : 1].playerColor;
    int _counterMisses;

    [Title("References", TitleAlignment = TitleAlignments.Centered)]
    [SerializeField] Camera camMain;
    [HideInInspector] public Transform camMainTransform;
    public SoPlayerData[] playerDatas;
    public UiManager uImanager;
    public AudioManager audioManager;
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
    public LayerMask layBow;
    public LayerMask layHands;

    Material[] _matsSkyboxSp;
    NetworkVariable<byte> _skyboxIndexNet = new NetworkVariable<byte>(byte.MaxValue);

    [PropertySpace(SpaceAfter = 0, SpaceBefore = 10)]
    [Title("Players...", TitleAlignment = TitleAlignments.Centered)]
    [SerializeField] GameObject prefabPlayer;
    public GameObject prefabArrowReal;
    public GameObject prefabArrowShadow;
    [Sirenix.OdinInspector.ReadOnly] public ArrowMain arrowReal;
    [Sirenix.OdinInspector.ReadOnly] public ArrowMain arrowShadow;
    [Sirenix.OdinInspector.ReadOnly] public float forceArrow;
    public NetworkObject[] bowTablesNet;
    [SerializeField] MeshRenderer[] playerMeshMarker;
    [SerializeField] GameObject[] scoreVisualMarker;
    public PlayerRegistration playerRegistration = new PlayerRegistration();

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
    public NetworkList<uint> levelsNet;
    public NetworkList<int> leaderboardNet;
    public NetworkList<byte> leagueNet;
    public NetworkList<FixedString128Bytes> nameNet;
    NetworkList<FixedString128Bytes> _authIdNet;

    private void Awake()
    {
        Instance = this;
        poolManager.Init();
        Physics.gravity = windManager.gravityVector;

        _matsSkyboxSp = Resources.LoadAll<Material>("Skybox materials SP");

        gridTileStatesNet = new NetworkList<byte>(new List<byte>());
        gridValuesNet = new NetworkList<sbyte>(new List<sbyte>());
        camMainTransform = camMain.transform;

        scoresNet = new NetworkList<uint>(new List<uint>() { 0, 0 });
        levelsNet = new NetworkList<uint>(new List<uint>() { 0, 0 });
        leaderboardNet = new NetworkList<int>(new List<int>() { 0, 0 });
        leagueNet = new NetworkList<byte>(new List<byte>() { 0, 0 });
        nameNet = new NetworkList<FixedString128Bytes>(new List<FixedString128Bytes>() { new FixedString128Bytes(), new FixedString128Bytes() });
        _authIdNet = new NetworkList<FixedString128Bytes>(new List<FixedString128Bytes>() { new FixedString128Bytes(), new FixedString128Bytes() });

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
            _skyboxIndexNet.Value = (byte)Random.Range(0, _matsSkyboxSp.Length);
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
            ChangeVisualMarkers_EveryoneRpc(playerTurnNet.Value);
            Utils.Activation(botManager.gameObject, false);
        }
        Utils.FadeOut?.Invoke(true);
        audioManager.PlaySFX(audioManager.gameStarted);

        Material chosenSkybox = _matsSkyboxSp[_skyboxIndexNet.Value];
        RenderSettings.skybox = chosenSkybox;
        RenderSettings.customReflectionTexture = chosenSkybox.GetTexture("_Tex");

        windManager.WindChange(float.MinValue, windAmountNet.Value);
        windAmountNet.OnValueChanged += windManager.WindChange;
        drawTrajectory.showTrajectory = trajectoryVisible.Value;
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
        while (seconds > 1)
        {
            seconds--;
            uImanager.SetDisplays(UiDisplays.InfoStart, $"Bot will spawn in {seconds} seconds");
            yield return new WaitForSeconds(1);
        }
        Utils.GameStarted?.Invoke();
        uImanager.SetDisplays(UiDisplays.InfoStart, st);
    }




    #region ARROWS
    [Rpc(SendTo.Everyone)]
    void ClearAllArrows_EveryoneRpc()
    {
        if (arrowReal != null) Destroy(arrowReal.gameObject);
        if (arrowShadow != null) Destroy(arrowShadow.gameObject);
    }

    public void SpawnRealArrow(Vector3 pos, Quaternion rot)
    {
        arrowReal = Instantiate(prefabArrowReal, pos, rot).GetComponent<ArrowMain>();
        SpawnShadowArrow_NotMeRpc(pos, rot);
    }

    [Rpc(SendTo.NotMe)]
    void SpawnShadowArrow_NotMeRpc(Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(prefabArrowShadow, pos, rot);
        arrowShadow = go.GetComponent<ArrowMain>();
    }

    [Rpc(SendTo.NotMe)]
    public void ShadowArrow_NotMeRpc(Vector3 pos, Quaternion rot)
    {
        if (arrowShadow == null) return;
        arrowShadow.myTransform.SetPositionAndRotation(pos, rot);
        arrowShadow.SetTrail();
    }
    #endregion

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
        playerRegistration.GameOver();
        if (Utils.GameType == MainGameType.Multiplayer && NetworkManager.Singleton.ConnectedClients.Count > 1) PlayerVictorious_EveryoneRpc(newValue);
        
        botManager.EndBot();
    }

    [Rpc(SendTo.Everyone)]
    void PlayerVictorious_EveryoneRpc(PlayerColor newValue)
    {
        switch (newValue)
        {
            case PlayerColor.Blue:
                PlayerVictoriousContinue(IsServer ? GenResult.Win : GenResult.Lose);
                break;
            case PlayerColor.Red:
                PlayerVictoriousContinue(!IsServer ? GenResult.Win : GenResult.Lose);
                break;
            case PlayerColor.None:
                PlayerVictoriousContinue(GenResult.Draw);
                break;
        }
        
        void PlayerVictoriousContinue(GenResult gameResult)
        {
            PlayerPrefs.SetInt(Utils.PlMatches_Int, PlayerPrefs.GetInt(Utils.PlMatches_Int) + 1);
            int currentXp = PlayerPrefs.GetInt(Utils.PlXp_Int);

            switch (gameResult)
            {
                case GenResult.Win:
                    audioManager.PlaySFX(audioManager.win);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreLeaderboardGlobalValues.x;
                    PlayerPrefs.SetInt(Utils.PlWins_Int, PlayerPrefs.GetInt(Utils.PlWins_Int) + 1);
                    PlayerLeveling.AddToXp(GenResult.Win);
                    break;
                
                case GenResult.Lose:
                    audioManager.PlaySFX(audioManager.loose);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreLeaderboardGlobalValues.y;
                    PlayerPrefs.SetInt(Utils.PlDefeats_Int, PlayerPrefs.GetInt(Utils.PlDefeats_Int) + 1);
                    PlayerLeveling.AddToXp(GenResult.Lose);
                    break;
                case GenResult.Draw:
                    audioManager.PlaySFX(audioManager.draw);
                    PlayerLeveling.AddToXp(GenResult.Draw);
                    break;
            }
            Launch.Instance.myDatabaseManager.UploadMyScore();
            int xpEarned = PlayerPrefs.GetInt(Utils.PlXp_Int) - currentXp;
            uImanager.SetDisplays(UiDisplays.XpEarned, $"You've earned {xpEarned} XP!");

            int myLeague = PlayerPrefs.GetInt(Utils.PlLeague_Int);
            if (PlayerPrefs.GetInt(Utils.PlWins_Int) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].z
                && PlayerPrefs.GetInt(Utils.PlMatches_Int) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].x) LeagueChange(GenChange.Increase);
            else if (PlayerPrefs.GetInt(Utils.PlDefeats_Int) > Utils.LeaguesTotalDefeatWinsTable[(League)myLeague].y) LeagueChange(GenChange.Decrease);

            void LeagueChange(GenChange change)
            {
                int prevLeague = myLeague; //debug only
                PlayerPrefs.SetInt(Utils.PlWins_Int, 0);
                PlayerPrefs.SetInt(Utils.PlDefeats_Int, 0);
                if (change != GenChange.Increase) myLeague++;
                else myLeague--;
                if (myLeague < 0) myLeague = 0;
                
                PlayerPrefs.SetInt(Utils.PlLeague_Int, myLeague);
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
    public void RegisterLevel_ServerRpc(uint level, int index) =>  levelsNet[index] = level;
    [Rpc(SendTo.Server)]
    public void RegisterLeaderboardRank_ServerRpc(int rank, int index) =>  leaderboardNet[index] = rank;
    [Rpc(SendTo.Server)]
    public void RegisterLeague_ServerRpc(int league, int index) =>  leagueNet[index] = (byte)league;
    [Rpc(SendTo.Server)]
    public void RegisterName_ServerRpc(FixedString128Bytes playerName,  int index) =>  nameNet[index] = playerName;
    [Rpc(SendTo.Server)]
    public void RegisterAuthenticationId_ServerRpc(FixedString128Bytes authId,  int index) =>  _authIdNet[index] = authId;


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
        for (int i = 0; i < 2; i++)
        {
            Utils.Activation(scoreVisualMarker[i], false);
            playerMeshMarker[i].material = playerDatas[(int)newValue].matMain;
        }
        Utils.Activation(scoreVisualMarker[(int)newValue], true);
    }

    [Rpc(SendTo.Server)]
    public void NextPlayer_ServerRpc(bool countGridMisses = true, string caller = "")
    {
        if (playerVictoriousNet.Value != PlayerColor.Undefined) return;
        ClearAllArrows_EveryoneRpc();
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
    [ContextMenu("Name blue")]
    void Metoda13()
    {
        print(nameNet[0].Value);
    }
    [ContextMenu("test net collections")]
    void Metoda14()
    {
        print(leagueNet[0]);
        print(leagueNet[1]);
    }

    #endregion
}


