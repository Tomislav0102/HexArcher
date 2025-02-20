using System;
using System.Collections;
using System.Collections.Generic;
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
    enum CanType { Main, End, Practice_Tut, Camp };
    [SerializeField] GameObject[] canvasElements;
    Dictionary<CanType?, GameObject> _elements;
    
    public TextMeshProUGUI displayStartInfo;
    [SerializeField] TextMeshProUGUI displayJoinCode, displayEarnedXp;
    [SerializeField] GameObject endInfos;
    bool _oneHitExitScene;
    [Title("Tutorial")]
    [SerializeField] Button[] buttonsTutorialDone;
    [Title("Campaign")]
    public TextMeshProUGUI displayCampaignInfoStart;
    public TextMeshProUGUI displayCampaignInfoEnd;
    [SerializeField] Button buttonCampStart;

    private void Awake()
    {
        gm = GameManager.Instance;
        _elements = new Dictionary<CanType?, GameObject>()
        {
            { CanType.Main, canvasElements[0] },
            { CanType.End, canvasElements[1] },
            { CanType.Practice_Tut, canvasElements[2] },
            { CanType.Camp, canvasElements[3] }
        };
        ActivateDictionary();
    }


    public override void OnNetworkSpawn()
    {
        ActivateDictionary(CanType.Main);
        switch (Utils.SinglePlayerType)
        {
            case SpType.Endless:
                break;
            case SpType.Campaign:
                ActivateDictionary(CanType.Camp);
                break;
            case SpType.Practice:
                ActivateDictionary(CanType.Practice_Tut);
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
        gm.scoreBlueNet.OnValueChanged += NetVarEv_ScoreChange;
        gm.scoreRedNet.OnValueChanged += NetVarEv_ScoreChange;

        displayJoinCode.enabled = false;
        if (!IsHost)
        {
            for (int i = 0; i < buttonsRestart.Length; i++)
            {
                buttonsRestart[i].gameObject.SetActive(false);
            }

            Invoke(nameof(ScoreDisplaying), 0.3f); //unity bug
            displayStartInfo.enabled = false;
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



    void CallEv_ClientConnected(ulong obj)
    {
        displayJoinCode.text = "";
    }
    void CallEv_ClientDisconnected(ulong obj)
    {
        displayJoinCode.text = $"Join code: {Launch.Instance.myLobbyManager.relayCode}";
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
        gm.scoreBlueNet.OnValueChanged -= NetVarEv_ScoreChange;
        gm.scoreRedNet.OnValueChanged -= NetVarEv_ScoreChange;

        if (IsHost && Utils.GameType == MainGameType.Multiplayer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= CallEv_ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= CallEv_ClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    void ActivateDictionary(CanType? canType = null)
    {
        foreach (KeyValuePair<CanType?, GameObject> item in _elements)
        {
            Utils.DeActivateGo(item.Value);
        }
        if (canType == null) return;
        Utils.ActivateGo(_elements[canType]);
    }
    
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
        ActivateDictionary(CanType.Main);
        Utils.GameStarted?.Invoke();
    }

    
    private void NetVarEv_ScoreChange(uint previousvalue, uint newvalue) => ScoreDisplaying();

    void ScoreDisplaying()
    {
        displayScores[0].text = gm.scoreBlueNet.Value.ToString();
        displayScores[1].text = gm.scoreRedNet.Value.ToString();
        displayEndScores[0].text = gm.scoreBlueNet.Value.ToString();
        displayEndScores[1].text = gm.scoreRedNet.Value.ToString();
    }


    private void NetVarEv_PlayerVictorious(PlayerColor previousValue, PlayerColor newValue)
    {
        if (newValue == PlayerColor.Undefined) return;
        
        ActivateDictionary(CanType.End);
        Transform tr = _elements[CanType.End].transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            Utils.DeActivateGo(tr.GetChild(i).gameObject);
        }
        int currentXp = PlayerPrefs.GetInt(Utils.Xp_Int);
        switch (newValue)
        {
            case PlayerColor.Blue:
                if (IsServer)
                {
                    tr.GetChild(0).gameObject.SetActive(true);
                    PlayerLeveling.AddToXp(GenFinish.Win);
                }
                else
                {
                    tr.GetChild(3).gameObject.SetActive(true);
                    PlayerLeveling.AddToXp(GenFinish.Lose);
                }
                break;
            case PlayerColor.Red:
                if (IsServer)
                {
                    tr.GetChild(1).gameObject.SetActive(true);
                    PlayerLeveling.AddToXp(GenFinish.Lose);
                }
                else
                {
                    tr.GetChild(2).gameObject.SetActive(true);
                    PlayerLeveling.AddToXp(GenFinish.Win);
                }
                break;
            case PlayerColor.None:
                tr.GetChild(4).gameObject.SetActive(true);
                PlayerLeveling.AddToXp(GenFinish.Draw);
                break;
        }
        Utils.ActivateGo(endInfos);
        int xpEarned = PlayerPrefs.GetInt(Utils.Xp_Int) - currentXp;
        displayEarnedXp.text = $"You've earned {xpEarned} XP!";
    }



}
