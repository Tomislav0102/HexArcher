using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine.Serialization;

public class PlayerControl : NetworkBehaviour
{
    GameManager gm;
    [SerializeField] Transform parBows, parHeads, parHands;
    [Sirenix.OdinInspector.ReadOnly] public BowControl bowCurrent;
    [Sirenix.OdinInspector.ReadOnly] public BowShooting shootingCurrent;
    public Transform displayNameTr;
    [SerializeField] Transform head;
    Transform[] _handsTransformCurrent;
    [Sirenix.OdinInspector.ReadOnly] public Animator[] handsAnimCurrent;
    [Sirenix.OdinInspector.ReadOnly] public PlayerInteractor[] handsInteractorCurrent;
    LineRenderer _lrShooting;


    public GenSide SideThatHoldsBow() //GenSide.Center means no bow is being held
    {
        if (bowCurrent.interactor == handsInteractorCurrent[0]) return GenSide.Left;
        if (bowCurrent.interactor == handsInteractorCurrent[1]) return GenSide.Right;

        return GenSide.Center;
    }


    void Awake()
    {
        gm = GameManager.Instance;


        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        MyLobbyManager mlm = Launch.Instance.myLobbyManager;

        int index = NetworkObject.IsOwnedByServer ? 0 : 1;
        if (IsOwner)
        {
            gm.playerVictoriousNet.OnValueChanged += NetVarEv_End;

            VrRigRef.instance.playerControl = this;
            VrRigRef.instance.leftController.selectAction.action.performed += InputSelectLeftOn;
            VrRigRef.instance.leftController.selectAction.action.canceled += InputSelectLeftOff;
            VrRigRef.instance.rightController.selectAction.action.performed += InputSelectRightOn;
            VrRigRef.instance.rightController.selectAction.action.canceled += InputSelectRightOff;
            VrRigRef.instance.leftController.activateAction.action.performed += InputActivateLeftOn;
            VrRigRef.instance.leftController.activateAction.action.canceled += InputActivateLeftOff;
            VrRigRef.instance.rightController.activateAction.action.performed += InputActivateRightOn;
            VrRigRef.instance.rightController.activateAction.action.canceled += InputActivateRightOff;
            VrRigRef.instance.root.SetPositionAndRotation(gm.bowTablesNet[NetworkManager.Singleton.IsHost ? 0 : 1].transform.position,
                gm.bowTablesNet[NetworkManager.Singleton.IsHost ? 0 : 1].transform.rotation);

            CallEv_FadeMethod(true);
            Utils.FadeOut += CallEv_FadeMethod;

            Utils.Activation(displayNameTr.gameObject, false);

            if (Utils.GameType == MainGameType.Multiplayer)
            {
                PlayerLeveling.CalculateLevelFromXp(out int level, out _);
                gm.RegisterLevel_ServerRpc((uint)level, index);

                int leaderboardRankIndexBuffer = PlayerPrefs.GetInt(Utils.PlLeaderBoardRank_Int);
                gm.RegisterLeaderboardRank_ServerRpc(leaderboardRankIndexBuffer, index);
                
                int leagueIndexBuffer = int.Parse(mlm.GetPlayerData(PlayerDataType.League, index));
                gm.RegisterLeague_ServerRpc(leagueIndexBuffer, index);
                
                FixedString128Bytes plNameIndexBuffer = mlm.GetPlayerName(index);
                gm.RegisterName_ServerRpc(plNameIndexBuffer, index);
                
                byte bowIndexBuffer = byte.Parse(mlm.GetPlayerData(PlayerDataType.BowIndex, index));
                gm.RegisterBow_ServerRpc(bowIndexBuffer, index);
                
                byte headIndexBuffer = byte.Parse(mlm.GetPlayerData(PlayerDataType.HeadIndex, index));
                gm.RegisterHead_ServerRpc(headIndexBuffer, index);
                
                byte handsIndexBuffer = byte.Parse(mlm.GetPlayerData(PlayerDataType.HandsIndex, index));
                gm.RegisterHands_ServerRpc(handsIndexBuffer, index);
                
                if (index == 1) gm.ChangeOwnershipOfBowTable_ServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        if (!IsServer)
        {
            Utils.GameStarted?.Invoke();
            gm.botManager.EndBot();
        }

        int bowIndex = gm.bowNet[index];
        Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parBows), bowIndex);
        bowCurrent = parBows.GetChild(bowIndex).GetComponent<BowControl>();
        shootingCurrent = bowCurrent.transform.GetChild(0).GetComponent<BowShooting>();
        _lrShooting = shootingCurrent.GetComponent<LineRenderer>();

        int handIndex = gm.handsNet[index];
        Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parHands), handIndex);
        _handsTransformCurrent = new Transform[2];
        handsAnimCurrent = new Animator[2];
        handsInteractorCurrent = new PlayerInteractor[2];
        for (int i = 0; i < 2; i++)
        {
            _handsTransformCurrent[i] = parHands.GetChild(handIndex).GetChild(i);
            handsAnimCurrent[i] = _handsTransformCurrent[i].GetChild(0).GetComponent<Animator>();
            handsInteractorCurrent[i] = _handsTransformCurrent[i].GetChild(1).GetComponent<PlayerInteractor>();
        }

        if (IsOwner)
        {
            bowCurrent.InitializeMe(this);
            shootingCurrent.InitializeMe(this);
            for (int i = 0; i < 2; i++)
            {
                handsInteractorCurrent[i].playerControl = this;
            }
        }
        else
        {
            bowCurrent.enabled = false;
            shootingCurrent.enabled = false;
            
            int headIndex = gm.headNet[index];
            Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parHeads), headIndex);
            for (int i = 0; i < 2; i++)
            {
                parHeads.GetChild(headIndex).GetChild(0).GetChild(i).GetComponent<MeshRenderer>().material = gm.playerDatas[index].matMain;
            }
            
            for (int i = 0; i < 2; i++)
            {
                handsInteractorCurrent[i].enabled = false;
            }

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

    private void Start() //consider moving to OnNetworkSpawn()
    {
        gm.playerRegistration.AddPlayer(this, NetworkObject.IsOwnedByServer);
        if (Utils.GameType == MainGameType.Multiplayer) ChangeName_EveryoneRpc();

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
        _handsTransformCurrent[0].SetPositionAndRotation(VrRigRef.instance.leftHand.position, VrRigRef.instance.leftHand.rotation);
        _handsTransformCurrent[1].SetPositionAndRotation(VrRigRef.instance.rightHand.position, VrRigRef.instance.rightHand.rotation);
    }


    #region CALL EVENTS & EVENTS

    private void NetVarEv_End(PlayerColor previousValue, PlayerColor newValue)
    {
        if (newValue != PlayerColor.Undefined)
        {
            handsAnimCurrent[0].SetFloat("Fist", 0);
            handsAnimCurrent[1].SetFloat("Fist", 0);
        }
    }

    private void InputSelectLeftOn(InputAction.CallbackContext context) => handsInteractorCurrent[0].Selected = true;
    private void InputSelectLeftOff(InputAction.CallbackContext context) => handsInteractorCurrent[0].Selected = false;
    private void InputSelectRightOn(InputAction.CallbackContext context) => handsInteractorCurrent[1].Selected = true;
    private void InputSelectRightOff(InputAction.CallbackContext context) => handsInteractorCurrent[1].Selected = false;
    private void InputActivateLeftOn(InputAction.CallbackContext context) => handsInteractorCurrent[0].Activated = true;
    private void InputActivateLeftOff(InputAction.CallbackContext context) => handsInteractorCurrent[0].Activated = false;
    private void InputActivateRightOn(InputAction.CallbackContext context) => handsInteractorCurrent[1].Activated = true;
    private void InputActivateRightOff(InputAction.CallbackContext context) => handsInteractorCurrent[1].Activated = false;


    void CallEv_FadeMethod(bool fadeout)
    {
        Utils.Activation(VrRigRef.instance.fadeSprite.gameObject, true);
        Color from = fadeout ? Color.black : Color.clear;
        Color to = fadeout ? Color.clear : Color.black;
        VrRigRef.instance.fadeSprite.DOColor(to, 2f)
            .From(from)
            .OnComplete(() =>
            {
                if (fadeout)
                {
                    Utils.Activation(VrRigRef.instance.fadeSprite.gameObject, false);
                }
            });
    }
    #endregion
}
