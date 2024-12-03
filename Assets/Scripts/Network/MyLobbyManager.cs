using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;


public class MyLobbyManager : MonoBehaviour
{
    [SerializeField] string env = "dev";
    [SerializeField] string lobbyName = "Lobby";
    [SerializeField] int maxPlayers = 4;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;
    
    RelayManager _relayManager;
    public string relayCode;
    string _playerId, _playerLeaderboardId, _playerName, _playerLevel;

    Lobby _currentLobby;

    string ConnectionType()
    {
        switch (encryption)
        {
            case EncryptionType.DTLS:
                return "dtls";
            case EncryptionType.UDP:
                return "udp";
            case EncryptionType.WSS:
                return "wss";
            default:
                return "dtls";
        }
    }

    float _timerHeartbeat, _timerUpdateLobbyData;
    const string CONST_KeyJoinCode = "RelayJoinCode";

    [Header("UI")]
    [SerializeField] Button btnCreate;
    [SerializeField] Button btnJoinByCode;
    [SerializeField] Button btnReady;
    [SerializeField] TMP_InputField inputPlName, inputJoinCode;

    bool _activeUpdates = true;
    bool _oneHitBtnCreate, _oneHitBtnJoin, _oneHitBtnReady;


    void Start()
    {
        _relayManager = new RelayManager(maxPlayers);
        DontDestroyOnLoad(this);

// #if (UNITY_EDITOR)
//         Invoke(nameof(EditorStartQuick), 0.3f);
// #endif
    }

    void EditorStartQuick() => Btn_Ready();
    public void Init()
    {
        btnCreate = MainMenuManager.Instance.btnCreate;
        btnJoinByCode = MainMenuManager.Instance.btnJoin;
        btnReady = MainMenuManager.Instance.btnReady;
        inputPlName = MainMenuManager.Instance.inputPlName;
        inputJoinCode = MainMenuManager.Instance.inputJoinCode;

        btnCreate.onClick.AddListener(Btn_Create);
        btnJoinByCode.onClick.AddListener(Btn_Join);
        btnReady.onClick.AddListener(Btn_Ready);

        if (PlayerPrefs.GetString(Utils.PlName_Str) == string.Empty) PlayerPrefs.SetString(Utils.PlName_Str, $"Player{Random.Range(0, 100)}");
        inputPlName.text = PlayerPrefs.GetString(Utils.PlName_Str);
        inputPlName.onValueChanged.AddListener(x => InField_PlayerName());

        _oneHitBtnCreate = _oneHitBtnJoin = _oneHitBtnReady = false;
    }
    async Task Authenticate(string playerName)
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions().SetEnvironmentName(env);

            string myProfile = System.Guid.NewGuid().ToString();
            myProfile = myProfile.Replace("-", "");
            myProfile = myProfile.Substring(0, 29);

            options.SetProfile(myProfile);

            await UnityServices.InitializeAsync(options);
        }

        AuthenticationService.Instance.SignedIn += () => { print($"Signed in as {AuthenticationService.Instance.PlayerId} with {playerName} name"); };

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _playerId = AuthenticationService.Instance.PlayerId;
            _playerLeaderboardId = Utils.MyIdLeaderboard();
            _playerName = playerName;
            PlayerLeveling.CalculateLevelFromXp(out int lv, out int xp);
            _playerLevel = lv.ToString();
        }

    }

    #region UI CONTROLS
    private void InField_PlayerName()
    {
        string str = inputPlName.text.Replace(" ", "");
        PlayerPrefs.SetString(Utils.PlName_Str, str);
    }

    async void Btn_Create()
    {
        if (_oneHitBtnCreate) return;
        _oneHitBtnCreate = true;

        await Authenticate(PlayerPrefs.GetString(Utils.PlName_Str));
        await CreateLobby();
        StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(MainGameType.Multiplayer));
    }

    async void Btn_QuickJoin()
    {
        if (_oneHitBtnJoin) return;
        _oneHitBtnJoin = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate(PlayerPrefs.GetString(Utils.PlName_Str));
        await QuickJoinLobby();
    }

    async void Btn_Join()
    {
        if (_oneHitBtnJoin) return;
        _oneHitBtnJoin = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate(PlayerPrefs.GetString(Utils.PlName_Str));
        await JoinWithRelayCode(inputJoinCode.text);
    }

    async void Btn_Ready()
    {
        if (_oneHitBtnReady) return;
        _oneHitBtnReady = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate(PlayerPrefs.GetString(Utils.PlName_Str));
        await QuickJoinLobby();
        if (_currentLobby == null)
        {
            await CreateLobby();
            StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(MainGameType.Multiplayer, true, false));
        }
    }
    #endregion

    #region HEARTBEAT AND UPDATES
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Btn_Create();
        if (Input.GetKeyDown(KeyCode.Alpha2)) Btn_QuickJoin();
        if (Input.GetKeyDown(KeyCode.Alpha3)) Btn_Ready();

        if (!_activeUpdates) return;
        HandleHeartbeat();
        HandleLobbyUpdates();

    }


    async void HandleHeartbeat()
    {
        if (_currentLobby == null) return;

        _timerHeartbeat += Time.deltaTime;
        if (_timerHeartbeat > 15)
        {
            _timerHeartbeat = 0f;
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
    }

    async void HandleLobbyUpdates() //changes in lobby dont sync automatically, this method updates all changes
    {
        if (_currentLobby == null) return;

        _timerUpdateLobbyData += Time.deltaTime;
        if (_timerUpdateLobbyData > 1.1f)
        {
            _timerUpdateLobbyData = 0f;
            _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
        }
    }
    #endregion

    #region LOBBY CONTROLS
    async Task CreateLobby()
    {
        try
        {
            Allocation allocation = await _relayManager.AllocateRelay();
            relayCode = await _relayManager.GetRelayJoinCode(allocation);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer()
            };
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { CONST_KeyJoinCode, new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                }
            });

            RelayHostData hostData = new RelayHostData
            {
                JoinCode = relayCode,
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                IPv4Address = allocation.RelayServer.IpV4,
                ConnectionData = allocation.ConnectionData,
            };
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(hostData.IPv4Address, hostData.Port, hostData.AllocationIDBytes, hostData.Key, hostData.ConnectionData);
            NetworkManager.Singleton.StartHost();
            Launch.Instance.mySceneManager.SubscribeAll();
            _activeUpdates = true;
            print($"Created lobby: {_currentLobby.Name}  with  relay code: {relayCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
            _oneHitBtnJoin = false;
        }
    }

    async Task QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions()
            {
                Player = GetPlayer()
            };
            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            relayCode = _currentLobby.Data[CONST_KeyJoinCode].Value;
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(relayCode);
            RelayJoinData joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                IPv4Address = allocation.RelayServer.IpV4,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                JoinCode = relayCode
            };
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinData.IPv4Address, joinData.Port, joinData.AllocationIDBytes, joinData.Key, joinData.ConnectionData, joinData.HostConnectionData);
            NetworkManager.Singleton.StartClient();
            Launch.Instance.mySceneManager.SubscribeAll();
            _activeUpdates = false;
            print($"starting client with {relayCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
            _oneHitBtnJoin = false;
        }
    }

    async Task JoinWithRelayCode(string codeFromRelay)
    {
        try
        {
            relayCode = codeFromRelay;
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()
            };
            QueryResponse qr = await LobbyService.Instance.QueryLobbiesAsync();
            foreach (Lobby item in qr.Results)
            {
                if (item.Data[CONST_KeyJoinCode].Value == relayCode)
                {
                    _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(item.Id, options);
                    break;
                }
            }

            JoinAllocation joinAllocation = await _relayManager.JoinRelay(this.relayCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, ConnectionType()));
            NetworkManager.Singleton.StartClient();
            Launch.Instance.mySceneManager.SubscribeAll();
            _activeUpdates = false;
            print($"starting client with relay code {relayCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            MainMenuManager.Instance.joinCodeInfo.text = "No lobby with that code found...";
            _oneHitBtnJoin = false;
        }
    }
    
    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
            _currentLobby = null;
            _activeUpdates = false;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }
    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            _currentLobby = null;
            _activeUpdates = false;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }
    #endregion

    #region MISC
    [Rpc(SendTo.Server)]
    public async void ClientDisconnectingRpc(string clientID)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, clientID);
            _currentLobby = null;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public string GetPlayerName(int index) => _currentLobby.Players[index].Data["PlayerName"].Value;
    public string GetPlayerLeaderboardId(int index) => _currentLobby.Players[index].Data["LeaderboardId"].Value;
    public string GetPlayerLevel(int index) => _currentLobby.Players[index].Data["PlayerLevel"].Value;
    public string GetPlayerId(int index) => _currentLobby.Players[index].Id;

    Player GetPlayer()
    {
        return new Player()
        {
            Data = new Dictionary<string, PlayerDataObject>()
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) },
                { "PlayerLevel", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerLevel) },
                { "LeaderboardId", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Private, _playerLeaderboardId) }
            }
        };
    }
    #endregion

    #region DEBUGS
    [ContextMenu("PrintPlayers")]
    void PrintPlayers() => PrintPlayers(_currentLobby);

    void PrintPlayers(Lobby lobby)
    {
        string numPlayers = lobby.Players.Count == 1 ? "" : "s";
        string textToDisplay = $"{lobby.Players.Count} player{numPlayers} in lobby {lobby.Name}" + "\n";
        foreach (Player item in lobby.Players)
        {
            print($"{item.Data["PlayerName"].Value} with id {item.Id}");
            textToDisplay += $"{item.Data["PlayerName"].Value} with id {item.Id}" + "\n";
        }
        print(textToDisplay);
    }

    [ContextMenu("Lobby data")]
    async void M1()
    {
        try
        {
            await Authenticate(PlayerPrefs.GetString(Utils.PlName_Str));
            QueryLobbiesOptions options = new QueryLobbiesOptions()
            {
                Count = 5,
            };
            QueryResponse qr = await LobbyService.Instance.QueryLobbiesAsync();
            print(qr.Results.Count);
            foreach (Lobby item in qr.Results)
            {
                print(item.Id);
                print(item.LobbyCode);
                print(item.Players.Count);
                print(item.Name);
                print(item.EnvironmentId);
                print(item.Created);
                print(item.Data);
                print(item.Data[CONST_KeyJoinCode].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    #endregion

}

    // async Task CreateLobby()
    // {
    //     try
    //     {
    //         Allocation allocation = await AllocateRelay();
    //         joinCode = await GetRelayJoinCode(allocation);
    //         CreateLobbyOptions options = new CreateLobbyOptions
    //         {
    //             IsPrivate = false,
    //             Player = GetPlayer()
    //         };
    //
    //         currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
    //         print($"Created lobby: {currentLobby.Name}, with code {joinCode}");
    //
    //         await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
    //         {
    //             Data = new Dictionary<string, DataObject> 
    //                 {
    //                     {CONST_KEY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode)}
    //                 }
    //         });
    //         NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, ConnectionType()));
    //         NetworkManager.Singleton.StartHost();
    //         MySceneManager.Instance.SubscribeAll();
    //         _activeUpdates = true;
    //     }
    //     catch (LobbyServiceException e)
    //     {
    //         Debug.LogError("Failed to create lobby: " + e.Message);
    //         _oneHitBtnJoin = false;
    //     }
    // }
    //
    // async Task QuickJoinLobby()
    // {
    //     try
    //     {
    //         QuickJoinLobbyOptions options = new QuickJoinLobbyOptions()
    //         {
    //             Player = GetPlayer()
    //         };
    //         currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
    //         joinCode = currentLobby.Data[CONST_KEY_JOIN_CODE].Value;
    //         JoinAllocation joinAllocation = await JoinRelay(joinCode);
    //         NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, ConnectionType()));
    //         NetworkManager.Singleton.StartClient();
    //         MySceneManager.Instance.SubscribeAll();
    //         print($"starting quickjoin client with {joinCode}");
    //         _activeUpdates = false;
    //     }
    //     catch (LobbyServiceException e)
    //     {
    //         Debug.LogError("Failed to quick join lobby: " + e.Message);
    //         _oneHitBtnJoin = false;
    //     }
    // }