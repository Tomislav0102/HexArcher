using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
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
    League _myLeague; 
    [SerializeField] string env = "dev";
    [SerializeField] string lobbyName = "Lobby";
    [SerializeField] int maxPlayers = 4;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;

    DatabaseManager _db;
    RelayManager _relayManager;
    public string relayCode;
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
    const string CONST_RankingOperation = "RankingOperation";

    [Header("UI")]
    [SerializeField] Button btnCreate;
    [SerializeField] Button btnJoinByCode;
    [SerializeField] Button btnReady;
    [SerializeField] TMP_InputField inputPlName, inputJoinCode;

    bool _activeUpdates = true;
    bool _oneHitBtnCreate, _oneHitBtnJoin, _oneHitBtnReady;

    #region INITIALIZATION
    void Awake()
    {
        _db = Launch.Instance.myDatabaseManager;
    }

    void Start()
    {
        _relayManager = new RelayManager(maxPlayers);
        DontDestroyOnLoad(this);

#if (UNITY_EDITOR)
        Invoke(nameof(Btn_Ready), 0.3f);
#endif
    }

    public void Init()
    {
        btnCreate = MainMenuManager.Instance.buttonCreate;
        btnJoinByCode = MainMenuManager.Instance.buttonJoin;
        btnReady = MainMenuManager.Instance.buttonReady;
        inputPlName = MainMenuManager.Instance.inputPlName;
        inputJoinCode = MainMenuManager.Instance.inputJoinCode;

        btnCreate.onClick.AddListener(Btn_Create);
        btnJoinByCode.onClick.AddListener(Btn_Join);
        btnReady.onClick.AddListener(Btn_Ready);

        if (_db._myData[MyData.Name] == null || _db._myData[MyData.Name].Length == 0) _db._myData[MyData.Name] = $"Player{Random.Range(0, 100)}";
        inputPlName.text = _db._myData[MyData.Name];
        inputPlName.onValueChanged.AddListener(x => InField_PlayerName());

        _oneHitBtnCreate = _oneHitBtnJoin = _oneHitBtnReady = false;
    }
    async Task Authenticate()
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

        AuthenticationService.Instance.SignedIn += () => { print($"Signed in as {AuthenticationService.Instance.PlayerId} with {_db._myData[MyData.Name]} name"); };

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _db._myData[MyData.Id] = AuthenticationService.Instance.PlayerId;
        }

    }
    #endregion

    #region UI CONTROLS
    private void InField_PlayerName()
    {
        _db._myData[MyData.Name] = inputPlName.text.Replace(" ", "");
    }

    async void Btn_Create()
    {
        if (_oneHitBtnCreate) return;
        _oneHitBtnCreate = true;

        await Authenticate();
        await CreateLobby();
        StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(MainGameType.Multiplayer));
    }

    async void Btn_QuickJoin()
    {
        if (_oneHitBtnJoin) return;
        _oneHitBtnJoin = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate();
        await QuickJoinLobby();
    }

    async void Btn_Join()
    {
        if (_oneHitBtnJoin) return;
        _oneHitBtnJoin = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate();
        await JoinWithRelayCode(inputJoinCode.text);
    }

    async void Btn_Ready()
    {
        if (_oneHitBtnReady) return;
        _oneHitBtnReady = true;

        Utils.FadeOut?.Invoke(false);
        Utils.GameType = MainGameType.Multiplayer;
        await Authenticate();
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
            _myLeague = System.Enum.Parse<League>(_db._myData[MyData.League]);
            await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { CONST_KeyJoinCode, new DataObject(DataObject.VisibilityOptions.Public, relayCode) },
                    { CONST_RankingOperation, new DataObject(DataObject.VisibilityOptions.Public, ((int)_myLeague).ToString()) },
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
                Player = GetPlayer(),
            };
            Lobby bufferLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            bool CanJoinThisLobby()
            {
                int myLeague = _db.GetValFromKeyEnum<int>(MyData.League);
                int hostLeague = int.Parse(bufferLobby.Data[CONST_RankingOperation].Value);
                
                if (hostLeague == System.Enum.GetNames(typeof(League)).Length - 1 && hostLeague != myLeague) return false;
                if (Mathf.Abs(hostLeague - myLeague) > 2)
                {
                    if ((League)hostLeague == League.Silver1 && (League)myLeague == League.Bronze1) return true;
                    if ((League)myLeague == League.Silver1 && (League)hostLeague == League.Bronze1) return true;
                    
                    if ((League)hostLeague == League.Platinum3 && (League)myLeague == League.Diamond3) return true;
                    if ((League)myLeague == League.Platinum3 && (League)hostLeague == League.Diamond3) return true;
                    
                    return false;
                }

                return true;
            }

            if (CanJoinThisLobby())
            {
                _currentLobby = bufferLobby;
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
                print($"starting client with {relayCode}");
            }
            
            _activeUpdates = false;
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

    Player GetPlayer()
    {
        return new Player()
        {
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
            await Authenticate();
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


