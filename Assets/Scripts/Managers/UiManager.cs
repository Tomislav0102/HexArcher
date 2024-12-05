using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Unity.Netcode;

public class UiManager : NetworkBehaviour
{
    GameManager gm;
    [SerializeField] GameObject canvasMain;
    [SerializeField] TextMeshProUGUI[] displayScores, displayEndScores;
    [SerializeField] Button[] btnsExit, btnsRestart;
    [SerializeField] GameObject[] canvasElements;
    public TextMeshProUGUI displayStartInfo;
    [SerializeField] TextMeshProUGUI displayJoinCode, displayEarnedXp;
    [SerializeField] GameObject endInfos;
    [Title("Tutorial")]
    [SerializeField] Button[] btnsTutorailDone;
    bool _oneHitExitScene;

    private void Awake()
    {
        gm = GameManager.Instance;
        Utils.ActivateOneArrayElement(canvasElements);
    }

    
    public override void OnNetworkSpawn()
    {
        if (Utils.PracticeSp)
        {
            canvasElements[0].SetActive(false);
            canvasElements[1].SetActive(false);
            canvasElements[2].SetActive(true);
        }
        else
        {
            canvasElements[0].SetActive(true);
            canvasElements[1].SetActive(false);
            canvasElements[2].SetActive(false);
        }

        base.OnNetworkSpawn();
        for (int i = 0; i < btnsExit.Length; i++)
        {
            btnsExit[i].onClick.AddListener(BtnMethodExit);
        }
        for (int i = 0; i < btnsRestart.Length; i++)
        {
            btnsRestart[i].onClick.AddListener(BtnMethodRestart);
        }
        for (int i = 0; i < btnsTutorailDone.Length; i++)
        {
            btnsTutorailDone[i].onClick.AddListener(BtnMethodTutDone);
        }

        gm.playerVictoriousNet.OnValueChanged += NetVarEv_PlayerVictorious;
       gm.scoreBlueNet.OnValueChanged += NetVarEv_ScoreChange;
       gm.scoreRedNet.OnValueChanged += NetVarEv_ScoreChange;

        displayJoinCode.enabled = false;
        if (!IsHost)
        {
            for (int i = 0; i < btnsRestart.Length; i++)
            {
                btnsRestart[i].gameObject.SetActive(false);
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
        for (int i = 0; i < btnsExit.Length; i++)
        {
            btnsExit[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < btnsRestart.Length; i++)
        {
            btnsRestart[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < btnsTutorailDone.Length; i++)
        {
            btnsTutorailDone[i].onClick.RemoveAllListeners();
        }

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

    void BtnMethodTutDone()
    {
        canvasElements[0].SetActive(true);
        canvasElements[1].SetActive(false);
        canvasElements[2].SetActive(false);
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
        
        Utils.DeActivateGo(canvasElements[0]);
        Utils.ActivateGo(canvasElements[1]);
        Utils.DeActivateGo(canvasElements[2]);
        Transform tr = canvasElements[1].transform;
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
