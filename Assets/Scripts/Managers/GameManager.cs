using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public bool MyTurn() => playerTurnNet.Value == playerDatas[indexInSo].playerColor;
    int _counterMisses;

    [Title("References", TitleAlignment = TitleAlignments.Centered)]
    public int indexInSo;

    public SoPlayerData[] playerDatas;
    public UiManager uImanager;
    public AudioManager audioManager;
    public BowRack[] bowRacks;
    public BotManager botManager;
    public GridManager gridManager;
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
    public ArrowMain arrowReal;
    public ArrowMain arrowShadow;
    public float forceArrow;
    [SerializeField] GameObject prefabBow;
    public NetworkObject[] bowTablesNet;
    [SerializeField] MeshRenderer[] playerMeshMarker;
    [SerializeField] GameObject[] scoreVisualMarker;

    [Title("Public network variables", TitleAlignment = TitleAlignments.Centered)]
    public NetworkVariable<GenLevel> difficultyNet = new NetworkVariable<GenLevel>();

    public NetworkVariable<PlayerColor> playerTurnNet = new NetworkVariable<PlayerColor>();
    public NetworkVariable<PlayerColor> playerVictoriousNet = new NetworkVariable<PlayerColor>();
    public NetworkList<int> scoreNet; //can't initialize here (unity bug)
    public NetworkList<byte> gridTileStatesNet;
    public NetworkList<sbyte> gridValuesNet;
    public NetworkVariable<float> windAmountNet = new NetworkVariable<float>();
    public NetworkVariable<bool> trajectoryVisible = new NetworkVariable<bool>();

    private void Awake()
    {
        Instance = this;
        poolManager.Init();
        Physics.gravity = windManager.gravityVector;

        _matsSkyboxSp = Resources.LoadAll<Material>("Skybox materials SP");

        scoreNet = new NetworkList<int>(new List<int>()); //must be initialized in Awake
        gridTileStatesNet = new NetworkList<byte>(new List<byte>());
        gridValuesNet = new NetworkList<sbyte>(new List<sbyte>());
        indexInSo = NetworkManager.Singleton.IsHost ? 0 : 1;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gridManager.Init();
        if (IsServer)
        {
            gridManager.ChooseGrid();
            difficultyNet.Value = (GenLevel)PlayerPrefs.GetInt(Utils.Difficulty_Int);
            for (int i = 0; i < 2; i++)
            {
                scoreNet.Add(0);
            }
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
            Utils.DeActivateGo(botManager.gameObject);
        }
        Utils.FadeOut?.Invoke(true);
        audioManager.PlaySFX(audioManager.gameStarted);

        Material chosenSkybox = _matsSkyboxSp[_skyboxIndexNet.Value];
        RenderSettings.skybox = chosenSkybox;
        RenderSettings.customReflectionTexture = chosenSkybox.GetTexture("_Tex");

        windManager.WindChange(float.MinValue, windAmountNet.Value);
        windAmountNet.OnValueChanged += windManager.WindChange;
        drawTrajectory.showTrajectory = trajectoryVisible.Value;

        if (Utils.GameType == MainGameType.Singleplayer && !Utils.PracticeSp)
        {
            Utils.GameStarted?.Invoke();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= CallEv_ClientDisconnected;
        }
    }

    IEnumerator Countdown()
    {
        int seconds = 0;
        string st = "";
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
            uImanager.displayStartInfo.text = $"Bot will spawn in {seconds} seconds";
            yield return new WaitForSeconds(1);
        }
        Utils.GameStarted?.Invoke();
        uImanager.displayStartInfo.text = st;
    }




    #region NEW THING
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
       // print("ShadowArrow_NotMeRpc spawned");
        //  ClearAllArrows_EveryoneRpc();
        GameObject go = Instantiate(prefabArrowShadow, pos, rot);
        arrowShadow = go.GetComponent<ArrowMain>();

    }

    [Rpc(SendTo.NotMe)]
    public void ShadowArrow_NotMeRpc(Vector3 pos, Quaternion rot)
    {
        if (arrowShadow == null)
        {
           // print("ShadowArrow_NotMeRpc is null");
            return;
        }
        arrowShadow.myTransform.SetPositionAndRotation(pos, rot);
        arrowShadow.SetTrail();
    }
    #endregion



    #region CALL EVENTS
    private void CallEv_ClientDisconnected(ulong obj)
    {
        //  print($"client {obj} disconnected, num of clients is {NetworkManager.Singleton.ConnectedClients.Count}");
        if (obj == NetworkManager.Singleton.LocalClientId) return;
        StartCoroutine(Countdown());
        bowTablesNet[1].ChangeOwnership(OwnerClientId);
        bowRacks[1].HideRack();
    }

    private void NetVarEv_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
        {
            Utils.DeActivateGo(playerDatas[i].playerControl.bowControl.gameObject);
        }
        if (Utils.GameType == MainGameType.Multiplayer && NetworkManager.Singleton.ConnectedClients.Count > 1) PlayerVictorious_EveryoneRpc(newValue);
    }

    [Rpc(SendTo.Everyone)]
    void PlayerVictorious_EveryoneRpc(PlayerColor newValue)
    {
        switch (newValue)
        {
            case PlayerColor.Blue:
                if (IsServer)
                {
                    audioManager.PlaySFX(audioManager.win);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreGlobalValues.x;
                }
                else
                {
                    audioManager.PlaySFX(audioManager.loose);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreGlobalValues.y;
                }
                break;
            case PlayerColor.Red:
                if (IsServer)
                {
                    audioManager.PlaySFX(audioManager.loose);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreGlobalValues.y;
                }
                else
                {
                    audioManager.PlaySFX(audioManager.win);
                    Launch.Instance.myDatabaseManager.LocalScore += Utils.ScoreGlobalValues.x;
                }
                break;
            case PlayerColor.None:
                audioManager.PlaySFX(audioManager.draw);
                break;
        }
        Launch.Instance.myDatabaseManager.UploadMyScore();

    }

    private void NetVarEv_PlayerTurnChange(PlayerColor previousValue, PlayerColor newValue)
    {
        ChangeVisualMarkers_EveryoneRpc(newValue);
    }
    #endregion

    #region REGISTRATIONS
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
            StartCoroutine(Countdown());
        }

        GameObject go = Instantiate(prefabPlayer);
        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj, true);
    }

    [Rpc(SendTo.Everyone)]
    public void RegisterPlayer_EveryoneRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject no))
        {
            int index = no.IsOwnedByServer ? 0 : 1;

            string nam = string.Empty;
            string level = string.Empty;
            string id = string.Empty;
            int leaderboard = -1;
            switch (Utils.GameType)
            {
                case MainGameType.Singleplayer:
                    nam = "my name";
                    id = "my id";
                    break;
                case MainGameType.Multiplayer:
                    int ind = no.IsOwnedByServer ? 0 : 1;
                    nam = Launch.Instance.myLobbyManager.GetPlayerName(ind);
                    level = Launch.Instance.myLobbyManager.GetPlayerLevel(ind);
                    id = Launch.Instance.myLobbyManager.GetPlayerId(ind);
                    leaderboard = PlayerPrefs.GetInt(Utils.LbRank_Int);
                    if (leaderboard > 0) leaderboard++;
                    break;
            }
            playerDatas[index].myName = nam;
            playerDatas[index].myLevel = level;
            playerDatas[index].myAutheticationId = id;
            playerDatas[index].myLeaderboardRank = leaderboard;

            no.name = $"Igrach {no.OwnerClientId}";
            playerDatas[index].netObj = no;
            playerDatas[index].playerId = no.OwnerClientId;
            playerDatas[index].playerControl = no.GetComponent<PlayerControl>();

            if (index == 1) ChangeOwnershipOfBowTable_ServerRpc(no.OwnerClientId);
        }

    }

    [Rpc(SendTo.Server)]
    void ChangeOwnershipOfBowTable_ServerRpc(ulong obj) => bowTablesNet[1].ChangeOwnership(obj);
    #endregion

    #region GAME FLOW
    [Rpc(SendTo.NotMe)]
    public void SetGridTileStateNet_NotMeRpc(byte ord, byte value)
    {
        gridManager.AssignTileState(ord, value);
        SetGridTileStateNet_ServerRpc(ord, value);
    }
    [Rpc(SendTo.Server)]
    void SetGridTileStateNet_ServerRpc(byte ord, byte value) => gridTileStatesNet[ord] = value;

    [Rpc(SendTo.Server)]
    public void SetGridValuesNet_ServerRpc(byte ord, sbyte value) => gridValuesNet[ord] = value;

    [Rpc(SendTo.Everyone)]
    void ChangeVisualMarkers_EveryoneRpc(PlayerColor newValue)
    {
        for (int i = 0; i < 2; i++)
        {
            Utils.DeActivateGo(scoreVisualMarker[i]);
            playerMeshMarker[i].material = playerDatas[(int)newValue].matMain;
        }
        Utils.ActivateGo(scoreVisualMarker[(int)newValue]);
    }

    [Rpc(SendTo.Server)]
    public void NextPlayer_ServerRpc(bool countGridMisses = true, string caller = "")
    {
        if (playerVictoriousNet.Value != PlayerColor.Undefined) return;
        ClearAllArrows_EveryoneRpc();
        // if (countGridMisses)
        // {
        //     _counterMisses++;
        //     if (_counterMisses >= 4)
        //     {
        //         print("4 misses in a row, match is over");
        //         DecideVictor();
        //         return;
        //     }
        // }
        // else _counterMisses = 0;
        int val = (int)playerTurnNet.Value;
        val = (1 + val) % 2;
        playerTurnNet.Value = (PlayerColor)val;
        // print($"next player is { playerTurnNet.Value }, called by {caller}");
    }

    [Rpc(SendTo.Server)]
    public void Scoring_ServerRpc()
    {
       if ((int)(playerVictoriousNet.Value) < 2) return; //is gameover
        bool useConsole = false;
        List<int> scoresTemp = new List<int>() { 0, 0 };
        List<ParentHex> takenHex = gridManager.AllTilesByType(TileState.Taken);
        if(useConsole) print($"taken hex {takenHex.Count}");
        foreach (ParentHex item in takenHex)
        {
            switch (item.CurrentValue)
            {
                case > 0:
                    scoresTemp[0] += item.CurrentValue;
                    if(useConsole) print("plus");
                    break;
                case < 0:
                    scoresTemp[1] -= item.CurrentValue;
                    if(useConsole) print("minus");
                    break;
            }
        }

        for (int i = 0; i < scoreNet.Count; i++)
        { 
            if(useConsole) print(scoresTemp[i]);
            scoreNet[i] = scoresTemp[i];
        }

        //check game over
        int freePoints = gridManager.NumOfTilesByType(TileState.Free) * 10;
        if (freePoints == 0)
        {
            print("all tiles are taken");
            DecideVictor();
        }
        else if (scoreNet[0] > scoreNet[1] + freePoints || scoreNet[1] > scoreNet[0] + freePoints)
        {
            print("impossible to win");
            DecideVictor();
        }

    }

    void DecideVictor()
    {
        if (scoreNet[0] > scoreNet[1])
        {
            playerVictoriousNet.Value = PlayerColor.Blue;
        }
        else if (scoreNet[0] < scoreNet[1])
        {
            playerVictoriousNet.Value = PlayerColor.Red;
        }
        else playerVictoriousNet.Value = PlayerColor.None;
    }
    #endregion

    #region DEBUGS

    public void NextPlayerDebug() => NextPlayer_ServerRpc(false, "from UI debug");

    [ContextMenu("Utils.GameType")]
    void Metoda1() => print(Utils.GameType);

    [ContextMenu("ConnectedClients.Count")]
    void Metoda2() => print(NetworkManager.Singleton.ConnectedClients.Count);

    [ContextMenu("Print all playerprefs")]
    void Metoda3() => Utils.DisplayAllPlayerPrefs();

    [ContextMenu("Change wind")]
    void Metoda4()
    {
        windAmountNet.Value = Random.Range(-0.5f, 0.5f);
        // print($"wind is {windAmmountNet.Value * CONST_WINDSCALE}");
        print($"wind is {windAmountNet.Value * 20}");
    }

    [ContextMenu("Next player")]
    void Metoda5()
    {
        NextPlayer_ServerRpc(false);
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
            print(scoreNet[i]);
        }
    }

    #endregion
}


