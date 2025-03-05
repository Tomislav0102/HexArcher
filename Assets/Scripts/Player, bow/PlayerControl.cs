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
    Transform _bowTransform;
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

    int Index() => NetworkObject.IsOwnedByServer ? 0 : 1;

    void Awake()
    {
        gm = GameManager.Instance;
        
        _bowTransform  = parBows.GetChild(0);//to prevent error from Lateupdate
        _handsTransformCurrent = new Transform[2]; 
        for (int i = 0; i < 2; i++)
        {
            _handsTransformCurrent[i] = parHands.GetChild(0).GetChild(i);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        DatabaseManager dm = Launch.Instance.myDatabaseManager;
        gm.bowNet.OnListChanged += NetVar_Bow;
        gm.headNet.OnListChanged += NetVar_Head;
        gm.handsNet.OnListChanged += NetVar_Hands;

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

            PlayerLeveling.CalculateLevelFromXp(out int level, out _);
            gm.RegisterLevel_ServerRpc((uint)level, Index());
            gm.RegisterName_ServerRpc(dm.myData[MyData.Name], Index());
            gm.RegisterLeague_ServerRpc(dm.GetValFromKeyEnum<byte>(MyData.League), Index());
            gm.RegisterLeaderboardRank_ServerRpc(dm.GetValFromKeyEnum<int>(MyData.LeaderboardRank), Index());
            gm.RegisterBow_ServerRpc(dm.GetValFromKeyEnum<byte>(MyData.BowIndex), Index());
            gm.RegisterHead_ServerRpc(dm.GetValFromKeyEnum<byte>(MyData.HeadIndex), Index());
            gm.RegisterHands_ServerRpc(dm.GetValFromKeyEnum<byte>(MyData.HandsIndex), Index());
            if (Index() == 1) gm.ChangeOwnershipOfBowTable_ServerRpc(NetworkManager.Singleton.LocalClientId);
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
        gm.bowNet.OnListChanged -= NetVar_Bow;
        gm.headNet.OnListChanged -= NetVar_Head;
        gm.handsNet.OnListChanged -= NetVar_Hands;

        base.OnNetworkDespawn();
    }

    private void Start() 
    {
        gm.playerRegistration.AddPlayer(this, NetworkObject.IsOwnedByServer);
        if (Utils.GameType == MainGameType.Multiplayer) ChangeName_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    void ChangeName_EveryoneRpc() => gm.playerRegistration.FillDisplay();

    [Rpc(SendTo.Everyone)]
    public void LineRendererLength_EveryoneRpc(Vector3 pos) => _lrShooting.SetPosition(1, pos);

    [Rpc(SendTo.NotMe)]
    void SyncBow_NotMeRpc(Vector3 pos, Quaternion rot) => _bowTransform.SetPositionAndRotation(pos, rot);
    [Rpc(SendTo.NotMe)]
    void SyncHands_NotMeRpc(Vector3[] pos, Quaternion[] rot)
    {
        for (int i = 0; i < 2; i++)
        {
            _handsTransformCurrent[i].SetPositionAndRotation(pos[i], rot[i]);
        }
    }

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
        
        SyncBow_NotMeRpc(_bowTransform.position, _bowTransform.rotation);
        Vector3[] pos = new Vector3[2]{ _handsTransformCurrent[0].position, _handsTransformCurrent[1].position };
        Quaternion[] rot = new Quaternion[2]{ _handsTransformCurrent[0].rotation, _handsTransformCurrent[1].rotation };
        SyncHands_NotMeRpc(pos, rot);
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
    void NetVar_Hands(NetworkListEvent<byte> changeevent)
    {
        int handIndex = gm.handsNet[Index()];
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
            for (int i = 0; i < 2; i++)
            {
                handsInteractorCurrent[i].playerControl = this;
            }
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                handsInteractorCurrent[i].enabled = false;
            }

        }

    }

    void NetVar_Head(NetworkListEvent<byte> changeevent)
    {
        if (!IsOwner)
        {
            int headIndex = gm.headNet[Index()];
            Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parHeads), headIndex);
            for (int i = 0; i < 2; i++)
            {
                parHeads.GetChild(headIndex).GetChild(0).GetChild(i).GetComponent<MeshRenderer>().material = gm.playerDatas[Index()].matMain;
            }
        }
    }

    void NetVar_Bow(NetworkListEvent<byte> changeevent)
    {
        int bowIndex = gm.bowNet[Index()];
        Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parBows), bowIndex);
        bowCurrent = parBows.GetChild(bowIndex).GetComponent<BowControl>();
        _bowTransform = bowCurrent.transform;
        shootingCurrent = bowCurrent.transform.GetChild(0).GetComponent<BowShooting>();
        _lrShooting = shootingCurrent.GetComponent<LineRenderer>();

        if (IsOwner)
        {
            bowCurrent.InitializeMe(this);
            shootingCurrent.InitializeMe(this);
        }
        else
        {
            bowCurrent.enabled = false;
            shootingCurrent.enabled = false;
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
