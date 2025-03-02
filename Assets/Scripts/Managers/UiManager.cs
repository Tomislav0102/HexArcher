using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine.Serialization;

public class UiManager : NetworkBehaviour
{
    GameManager gm;
    [SerializeField] TextMeshProUGUI[] displayScores, displayEndScores;
    [SerializeField] Button[] buttonsExit, buttonsRestart;
    enum PanelState { Main, End, Practice_Tut, Camp };
    [SerializeField] GameObject[] canvasElements;
    Dictionary<PanelState?, GameObject> _elements;

    [SerializeField] TextMeshProUGUI displayStartInfo;
    [SerializeField] TextMeshProUGUI displayJoinCode, displayEarnedXp;
    [SerializeField] GameObject endInfos;
    bool _oneHitExitScene;
    [Title("Tutorial")]
    [SerializeField] Button[] buttonsTutorialDone;

    [Title("Campaign")]
    [SerializeField] TextMeshProUGUI displayCampaignInfoStart;
    [SerializeField] TextMeshProUGUI displayCampaignInfoEnd;
    [SerializeField] Button buttonCampStart;

    private void Awake()
    {
        gm = GameManager.Instance;
    }


    public override void OnNetworkSpawn()
    {
        ActivateDictionary(PanelState.Main);
        switch (Utils.SinglePlayerType)
        {
            case SpType.Endless:
                break;
            case SpType.Campaign:
                ActivateDictionary(PanelState.Camp);
                break;
            case SpType.Practice:
                ActivateDictionary(PanelState.Practice_Tut);
                break;
        }

        base.OnNetworkSpawn();
        for (int i = 0; i < buttonsExit.Length; i++)
        {
            buttonsExit[i].onClick.AddListener(BtnMethodExit);
        }
        for (int i = 0; i < buttonsRestart.Length; i++)
        {
            buttonsRestart[i].onClick.AddListener(BtnMethodRestart);
        }
        for (int i = 0; i < buttonsTutorialDone.Length; i++)
        {
            buttonsTutorialDone[i].onClick.AddListener(BtnMethodSpCanStart);
        }
        buttonCampStart.onClick.AddListener(BtnMethodSpCanStart);

        gm.playerVictoriousNet.OnValueChanged += NetVarEv_PlayerVictorious;
        gm.scoresNet.OnListChanged += NetVarEv_ScoreChange;

        displayJoinCode.enabled = false;
        if (!IsHost)
        {
            for (int i = 0; i < buttonsRestart.Length; i++)
            {
                buttonsRestart[i].gameObject.SetActive(false);
            }

            Invoke(nameof(ScoreDisplaying), 0.3f);//unity bug
            SetDisplays(UiDisplays.CampStart, string.Empty);
        }
        else if (Utils.GameType == MainGameType.Multiplayer)
        {
            displayJoinCode.enabled = true;
            displayJoinCode.text = $"Join code: {Launch.Instance.myLobbyManager.relayCode}";
            NetworkManager.Singleton.OnClientConnectedCallback += CallEv_ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += CallEv_ClientDisconnected;

            if (NetworkManager.Singleton.ConnectedClients.Count > 1)
            {
                CallEv_ClientConnected(0);
            }
        }

    }

    public override void OnNetworkDespawn()
    {
        for (int i = 0; i < buttonsExit.Length; i++)
        {
            buttonsExit[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < buttonsRestart.Length; i++)
        {
            buttonsRestart[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < buttonsTutorialDone.Length; i++)
        {
            buttonsTutorialDone[i].onClick.RemoveAllListeners();
        }
        buttonCampStart.onClick.RemoveAllListeners();

        gm.playerVictoriousNet.OnValueChanged -= NetVarEv_PlayerVictorious;
        gm.scoresNet.OnListChanged -= NetVarEv_ScoreChange;
        
        if (IsHost && Utils.GameType == MainGameType.Multiplayer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= CallEv_ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= CallEv_ClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    void ActivateDictionary(PanelState? canType = null)
    {
        _elements ??= new Dictionary<PanelState?, GameObject>()
        {
            { PanelState.Main, canvasElements[0] },
            { PanelState.End, canvasElements[1] },
            { PanelState.Practice_Tut, canvasElements[2] },
            { PanelState.Camp, canvasElements[3] }
        };
        
        foreach (KeyValuePair<PanelState?, GameObject> item in _elements)
        {
            Utils.Activation(item.Value, false);
        }
        if (canType == null) return;
        Utils.Activation(_elements[canType], true);
    }
    void ScoreDisplaying()
    {
        for (int i = 0; i < 2; i++)
        {
            displayScores[i].text = gm.scoresNet[i].ToString();
        }
    }

    public void SetDisplays(UiDisplays displays, string message)
    {
        TextMeshProUGUI textMesh;
        switch (displays)
        {
            case UiDisplays.InfoStart:
                textMesh = displayStartInfo;
                break;
            case UiDisplays.CampStart:
                textMesh = displayCampaignInfoStart;
                break;
            case UiDisplays.CampEnd:
                textMesh = displayCampaignInfoEnd;
                break;
            case UiDisplays.XpEarned:
                textMesh = displayEarnedXp;
                break;
            default:
                return;
        }
        
        textMesh.text = message;
    }
    

    #region BUTTONS/CALLS
    
    [ContextMenu("BtnMethodExit")]
    void BtnMethodExit()
    {
        print("exiting...");
        if (_oneHitExitScene) return;
        _oneHitExitScene = true;

        gm.audioManager.PlaySFX(gm.audioManager.uiButton);
        NetworkManager.Singleton.Shutdown();
    }

    [ContextMenu("BtnMethodRestart")]
    void BtnMethodRestart()
    {
        print("restarting...");
        if (_oneHitExitScene) return;
        _oneHitExitScene = true;
        gm.audioManager.PlaySFX(gm.audioManager.uiButton);
        
        MainGameType mainGameType = NetworkManager.Singleton.ConnectedClients.Count > 1 ? MainGameType.Multiplayer : MainGameType.Singleplayer;
        StartCoroutine(Launch.Instance.mySceneManager.NewSceneAfterFadeIn(mainGameType, true));
    }

    void BtnMethodSpCanStart()
    {
        ActivateDictionary(PanelState.Main);
        Utils.GameStarted?.Invoke();
    }

    void CallEv_ClientConnected(ulong obj) => displayJoinCode.text = "";
    void CallEv_ClientDisconnected(ulong obj) => displayJoinCode.text = $"Join code: {Launch.Instance.myLobbyManager.relayCode}";

    private void NetVarEv_ScoreChange(NetworkListEvent<uint> changeevent) => ScoreDisplaying();

    private void NetVarEv_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
        if (newValue == PlayerColor.Undefined) return;
        
        ActivateDictionary(PanelState.End);
        Transform tr = _elements[PanelState.End].transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            Utils.Activation(tr.GetChild(i).gameObject, false);
        }
        switch (newValue)
        {
            case PlayerColor.Blue:
                Utils.Activation(tr.GetChild(IsServer ? 0 : 3).gameObject, true);
                break;
            case PlayerColor.Red:
                Utils.Activation(tr.GetChild(IsServer ? 1 : 2).gameObject, true);
                break;
            case PlayerColor.None:
                Utils.Activation(tr.GetChild(4).gameObject, true);
                break;
        }
        Utils.Activation(endInfos, true);
        for (int i = 0; i < 2; i++)
        {
            displayEndScores[i].text = gm.scoresNet[i].ToString();
        }

    }
    #endregion



}
