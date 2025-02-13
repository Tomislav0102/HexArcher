using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Unity.Netcode;
using TMPro;

public class PlayerControl : NetworkBehaviour
{
    GameManager gm;
    public BowControl bowControl;
    public BowShooting shooting;
    public Transform displayNameTr;
    [SerializeField] Transform head, leftHand, rightHand;
    [SerializeField] MeshRenderer[] headMeshes;
    public Animator[] animatedHands;
    public PlayerInteractor[] handInteractors;
    LineRenderer _lrShooting;


    public GenSide SideThatHoldsBow() //GenSide.Center means no bow is being held
    {
        if (bowControl.interactor == handInteractors[0]) return GenSide.Left;
        if (bowControl.interactor ==  handInteractors[1]) return GenSide.Right;

        return GenSide.Center;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gm = GameManager.Instance;
        _lrShooting = shooting.GetComponent<LineRenderer>();
        if (IsOwner)
        {
            gm.playerVictoriousNet.OnValueChanged += NetVarEv_End;

            VrRigRef.instance.playerControl = this;
            for (int i = 0; i < 2; i++)
            {
                handInteractors[i].playerControl = this;
            }
            VrRigRef.instance.leftController.selectAction.action.performed += InputSelectLeftOn;
            VrRigRef.instance.leftController.selectAction.action.canceled += InputSelectLeftOff;
            VrRigRef.instance.rightController.selectAction.action.performed += InputSelectRightOn;
            VrRigRef.instance.rightController.selectAction.action.canceled += InputSelectRightOff;
            VrRigRef.instance.leftController.activateAction.action.performed += InputActivateLeftOn;
            VrRigRef.instance.leftController.activateAction.action.canceled += InputActivateLeftOff;
            VrRigRef.instance.rightController.activateAction.action.performed += InputActivateRightOn;
            VrRigRef.instance.rightController.activateAction.action.canceled += InputActivateRightOff;
            VrRigRef.instance.root.SetPositionAndRotation(gm.bowTablesNet[gm.indexInSo].transform.position, 
                gm.bowTablesNet[gm.indexInSo].transform.rotation);
            
            CallEv_FadeMethod(true);
            Utils.FadeOut += CallEv_FadeMethod;

            Utils.DeActivateGo(displayNameTr.gameObject);

            if (Utils.GameType == MainGameType.Multiplayer)
            {
                int index = NetworkObject.IsOwnedByServer ? 0 : 1;
                gm.RegisterLevel_ServerRpc(uint.Parse(Launch.Instance.myLobbyManager.GetPlayerLevel(index)), index == 0);
                gm.RegisterLeaderboardRank_ServerRpc(PlayerPrefs.GetInt(Utils.LbRank_Int), index == 0);
                gm.RegisterName_ServerRpc(Launch.Instance.myLobbyManager.GetPlayerName(index), index == 0);
                gm.RegisterAuthenticationId_ServerRpc(Launch.Instance.myLobbyManager.GetPlayerId(index), index == 0);
                if (index == 1) gm.ChangeOwnershipOfBowTable_ServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }
        else
        {
            bowControl.enabled = false;
            shooting.enabled = false;
            for (int i = 0; i < 2; i++)
            {
                handInteractors[i].enabled = false;
            }
        }
        
        if (!IsServer)
        {
            Utils.GameStarted?.Invoke();
            gm.botManager.EndBot();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            gm.playerVictoriousNet.OnValueChanged -= NetVarEv_End;
            VrRigRef.instance.leftController.selectAction.action.performed -= InputSelectLeftOn;
            VrRigRef.instance.leftController.selectAction.action.canceled -= InputSelectLeftOff;
            VrRigRef.instance.rightController.selectAction.action.performed -= InputSelectRightOn;
            VrRigRef.instance.rightController.selectAction.action.canceled -= InputSelectRightOff;
            VrRigRef.instance.leftController.activateAction.action.performed -= InputActivateLeftOn;
            VrRigRef.instance.leftController.activateAction.action.canceled -= InputActivateLeftOff;
            VrRigRef.instance.rightController.activateAction.action.performed -= InputActivateRightOn;
            VrRigRef.instance.rightController.activateAction.action.canceled -= InputActivateRightOff;

            Utils.FadeOut -= CallEv_FadeMethod;
        }
        gm.playerRegistration.RemovePlayer(this);
        base.OnNetworkDespawn();
    }

    private void Start()
    {
        gm.playerRegistration.AddPlayer(this, NetworkObject.IsOwnedByServer);
        if (Utils.GameType == MainGameType.Multiplayer) ChangeName_EveryoneRpc();

        for (int i = 0; i < headMeshes.Length; i++)
        {
            headMeshes[i].material = gm.playerDatas[gm.indexInSo].matMain;
        }
    }

    [Rpc(SendTo.Everyone)]
    void ChangeName_EveryoneRpc() => gm.playerRegistration.FillDisplay();
    
    [Rpc(SendTo.Everyone)]
    public void LineRendererLength_EveryoneRpc(Vector3 pos) => _lrShooting.SetPosition(1, pos);

    private void Update()
    {
        if (!IsOwner)
        {
            if (gm.playerRegistration.HasSecondPlayer()) displayNameTr.LookAt(gm.camMainTransform.position);
        }
    }

    void LateUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        transform.SetPositionAndRotation(VrRigRef.instance.root.position, VrRigRef.instance.root.rotation);
        head.SetPositionAndRotation(VrRigRef.instance.head.position, VrRigRef.instance.head.rotation);
        leftHand.SetPositionAndRotation(VrRigRef.instance.leftHand.position, VrRigRef.instance.leftHand.rotation);
        rightHand.SetPositionAndRotation(VrRigRef.instance.rightHand.position, VrRigRef.instance.rightHand.rotation);
    }


    #region CALL EVENTS & EVENTS

    private void NetVarEv_End(PlayerColor previousValue, PlayerColor newValue)
    {
        if (newValue != PlayerColor.Undefined)
        {
            animatedHands[0].SetFloat("Fist", 0);
            animatedHands[1].SetFloat("Fist", 0);
        }
    }

    private void InputSelectLeftOn(InputAction.CallbackContext context) => handInteractors[0].Selected = true;
    private void InputSelectLeftOff(InputAction.CallbackContext context) => handInteractors[0].Selected = false;
    private void InputSelectRightOn(InputAction.CallbackContext context) => handInteractors[1].Selected = true;
    private void InputSelectRightOff(InputAction.CallbackContext context) => handInteractors[1].Selected = false;
    private void InputActivateLeftOn(InputAction.CallbackContext context) => handInteractors[0].Activated = true;
    private void InputActivateLeftOff(InputAction.CallbackContext context) => handInteractors[0].Activated = false;
    private void InputActivateRightOn(InputAction.CallbackContext context) => handInteractors[1].Activated = true;
    private void InputActivateRightOff(InputAction.CallbackContext context) => handInteractors[1].Activated = false;


    void CallEv_FadeMethod(bool fadeout)
    {
        Utils.ActivateGo(VrRigRef.instance.fadeSprite.gameObject);
        Color from = fadeout ? Color.black : Color.clear;
        Color to = fadeout ? Color.clear : Color.black;
        VrRigRef.instance.fadeSprite.DOColor(to, 2f)
            .From(from)
            .OnComplete(() =>
            {
                if (fadeout)
                {
                    Utils.DeActivateGo(VrRigRef.instance.fadeSprite.gameObject);
                }
            });
    }
    #endregion


}
