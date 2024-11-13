using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Authentication;


public class MySceneManager : NetworkBehaviour
{
    public static MySceneManager Instance;
    string _sceneMainMenu = "MainMenu";
    string _sceneGame = "Game";


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        DontDestroyOnLoad(this);
        SceneManager.LoadScene(_sceneMainMenu);
    }

    public IEnumerator NewSceneAfterFadeIn(MainGameType gameType, bool canReloadSameScene = false, bool useFade = true)
    {
        if (!canReloadSameScene && Utils.GameType == gameType) yield break;
        Utils.GameType = gameType;
        
        if (/*false &&*/ useFade)
        {
            Utils.FadeOut?.Invoke(false);
            yield return new WaitForSeconds(2);
        }

        switch (Utils.GameType)
        {
            case MainGameType.MainMenu:
                SceneManager.LoadScene(_sceneMainMenu);
                break;
            case MainGameType.Singleplayer:
                NetworkManager.Singleton.SceneManager.LoadScene(_sceneGame, LoadSceneMode.Single);
                break;
            case MainGameType.Multiplayer:
                NetworkManager.Singleton.SceneManager.LoadScene(_sceneGame, LoadSceneMode.Single);
                break;
        }
    }


    public void SubscribeAll()
    {
        //NetworkManager.Singleton.SceneManager.OnLoad += M0;
        //NetworkManager.Singleton.SceneManager.OnLoadComplete += M1;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += CallEv_LoadEventCompleted;
        //NetworkManager.Singleton.SceneManager.OnUnload += M3;
        //NetworkManager.Singleton.SceneManager.OnUnloadComplete += M4;
       // NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted += M5;
     //  NetworkManager.Singleton.SceneManager.OnSceneEvent += M6;
      //  NetworkManager.Singleton.SceneManager.OnSynchronize += M7;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += CallEv_SynchronizeComplete;
    }
    void UnSubscribeAll()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= CallEv_LoadEventCompleted;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= CallEv_SynchronizeComplete;
    }



    public override void OnNetworkDespawn()
    {
        if (IsServer) //host leaves or disconnects
        {
            MyLobbyManager.Instance.DeleteLobby();
        }
        else
        {
            MyLobbyManager.Instance.ClientDisconnectingRpc(AuthenticationService.Instance.PlayerId);
        }
        StartCoroutine(NewSceneAfterFadeIn(MainGameType.MainMenu));
        base.OnNetworkDespawn();
        UnSubscribeAll();
    }


    private void CallEv_SynchronizeComplete(ulong clientId)
    {
        print($"OnSynchronizeComplete {clientId}");
        /*if(!IsServer)*/ GameManager.Instance.SpawnPlayersRpc(clientId);
    }
    private void CallEv_LoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        print($"OnLoadEventCompleted, scene name is {sceneName}");
        GameManager.Instance.SpawnPlayersRpc(NetworkManager.Singleton.LocalClientId);
    }

    #region
    private void M7(ulong clientId)
    {
        print($"OnSynchronize {clientId}");
    }

    private void M6(SceneEvent sceneEvent)
    {
        print($"OnSceneEvent, scene event is {sceneEvent.SceneEventType}");
    }

    private void M5(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        print($"OnUnloadEventCompleted, scene name is {sceneName}");
    }
    private void M4(ulong clientId, string sceneName)
    {
        print($"OnUnloadComplete {clientId}, scene name is {sceneName}");
    }


    private void M3(ulong clientId, string sceneName, AsyncOperation asyncOperation)
    {
        print($"OnUnload {clientId}, scene name is {sceneName}");
    }


    private void M1(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        print($"OnLoadComplete {clientId}, scene name is {sceneName}");
    }

    private void M0(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        print($"OnLoad {clientId}, scene name is {sceneName}");
    }
    #endregion

}
