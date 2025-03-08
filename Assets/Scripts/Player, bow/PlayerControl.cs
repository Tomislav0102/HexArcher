using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Unity.Netcode;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class PlayerControl : NetworkBehaviour
{
    GameManager gm;
    [SerializeField] Transform parBows, parHeads, parHands;
    [ReadOnly] public BowControl bowCurrent;
    Transform _bowTransform;
    LineRenderer _lrShooting;
    [ReadOnly] public BowShooting shootingCurrent;
    public Transform displayNameTr;
    [SerializeField] Transform head;
    [Title("Hands")]
    [SerializeField] Transform[] handsTransform;
    public Animator[] handsAnim;
    public PlayerInteractor[] handsInteractor;
    [SerializeField] SkinnedMeshRenderer[] handsMeshMat0;
    [SerializeField] SkinnedMeshRenderer[] handsMeshMat1;
    [SerializeField] SoHands[] handsScriptables;

    public GenSide SideThatHoldsBow() //GenSide.Center means no bow is being held
    {
        if (bowCurrent.interactor == handsInteractor[0]) return GenSide.Left;
        if (bowCurrent.interactor == handsInteractor[1]) return GenSide.Right;

        return GenSide.Center;
    }

    int Index() => NetworkObject.IsOwnedByServer ? 0 : 1;

    void Awake()
    {
        gm = GameManager.Instance;
        
        _bowTransform  = parBows.GetChild(0);//to prevent error from Lateupdate
        handsTransform = new Transform[2]; 
        for (int i = 0; i < 2; i++)
        {
            handsTransform[i] = parHands.GetChild(0).GetChild(i);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        DatabaseManager dm = Launch.Instance.myDatabaseManager;
        gm.playerDisplayNet.OnListChanged += NetVar_PlayerData;
        gm.equipmentNet.OnListChanged += NetVar_Equipment;
        
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
            gm.RegisterPlayerDisplay_ServerRpc(Index(), dm.observableData[MyData.Name], 
                (uint)level, 
                dm.GetValFromKeyEnum<byte>(MyData.League),
                dm.GetValFromKeyEnum<int>(MyData.LeaderboardRank));

            gm.RegisterPlayerEquipment_ServerRpc(Index(), new byte[3]
            {
                dm.GetValFromKeyEnum<byte>(MyData.BowIndex),
                dm.GetValFromKeyEnum<byte>(MyData.HeadIndex),
                dm.GetValFromKeyEnum<byte>(MyData.HandsIndex)
            });
            
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
        gm.playerDisplayNet.OnListChanged -= NetVar_PlayerData;
        gm.equipmentNet.OnListChanged -= NetVar_Equipment;
        
        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Everyone)]
    public void LineRendererLength_EveryoneRpc(Vector3 pos) => _lrShooting.SetPosition(1, pos);

    [Rpc(SendTo.NotMe)]
    void SyncBow_NotMeRpc(Vector3 pos, Quaternion rot) => _bowTransform.SetPositionAndRotation(pos, rot);
    [Rpc(SendTo.NotMe)]
    void SyncHands_NotMeRpc(Vector3[] pos, Quaternion[] rot)
    {
        for (int i = 0; i < 2; i++)
        {
            handsTransform[i].SetPositionAndRotation(pos[i], rot[i]);
        }
    }

    private void Update()
    {
        if (IsOwner) return;
        displayNameTr.LookAt(gm.camMainTransform.position);
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        
        transform.SetPositionAndRotation(VrRigRef.instance.root.position, VrRigRef.instance.root.rotation);
        head.SetPositionAndRotation(VrRigRef.instance.head.position, VrRigRef.instance.head.rotation);
        handsTransform[0].SetPositionAndRotation(VrRigRef.instance.leftHand.position, VrRigRef.instance.leftHand.rotation);
        handsTransform[1].SetPositionAndRotation(VrRigRef.instance.rightHand.position, VrRigRef.instance.rightHand.rotation);
        
        SyncBow_NotMeRpc(_bowTransform.position, _bowTransform.rotation);
        Vector3[] pos = new Vector3[2]{ handsTransform[0].position, handsTransform[1].position };
        Quaternion[] rot = new Quaternion[2]{ handsTransform[0].rotation, handsTransform[1].rotation };
        SyncHands_NotMeRpc(pos, rot);
    }


    #region CALL EVENTS & EVENTS
    void NetVar_PlayerData(NetworkListEvent<NetPlayerDisplay> changeevent)
    {
        int index = Index();
        string myName = "";
        string levelDisplay = "Level - ";
        string leagueDisplay = "Rank - ";
        string lbDisplay = "Leaderboard - ";
            
        myName = gm.playerDisplayNet[index].name.ToString();
        levelDisplay += gm.playerDisplayNet[index].level.ToString();
        leagueDisplay += ((League)gm.playerDisplayNet[index].league).ToString();
        if ((League)gm.playerDisplayNet[index].league != League.Challenger) leagueDisplay = leagueDisplay.Remove(leagueDisplay.Length - 1, 1);
        if (gm.playerDisplayNet[index].leaderboard < 0) lbDisplay = string.Empty;
        else
        {
            int rank = gm.playerDisplayNet[index].leaderboard;
            lbDisplay += (rank + 1).ToString();
        }
            
        displayNameTr.GetComponent<TextMeshPro>().text = myName + "\n" + levelDisplay + "\n" + leagueDisplay + "\n" + lbDisplay;
        gameObject.name = $"Igrach {index}";

    }
    void NetVar_Equipment(NetworkListEvent<NetPlayerEquipment> changeevent)
    {
        int bowIndex = gm.equipmentNet[Index()].bowIndex;
        Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parBows), bowIndex);
        bowCurrent = parBows.GetChild(bowIndex).GetComponent<BowControl>();
        _bowTransform = bowCurrent.transform;
        shootingCurrent = bowCurrent.transform.GetChild(0).GetComponent<BowShooting>();
        _lrShooting = shootingCurrent.GetComponent<LineRenderer>();

        int handIndex = gm.equipmentNet[Index()].handsIndex;
        for (int i = 0; i < 2; i++)
        {
            handsMeshMat0[i].material = handsScriptables[handIndex].mats[0];
            handsMeshMat1[i].material = handsScriptables[handIndex].mats[1];
        }
        
        if (IsOwner)
        {
            bowCurrent.InitializeMe(this);
            shootingCurrent.InitializeMe(this);
            
            for (int i = 0; i < 2; i++)
            {
                handsInteractor[i].playerControl = this;
            }
        }
        else
        {
            bowCurrent.enabled = false;
            shootingCurrent.enabled = false;
            
            int headIndex = gm.equipmentNet[Index()].headIndex;
            Utils.ActivateOneArrayElement(Utils.AllChildrenGameObjects(parHeads), headIndex);
            for (int i = 0; i < 2; i++)
            {
                parHeads.GetChild(headIndex).GetChild(0).GetChild(i).GetComponent<MeshRenderer>().material = gm.playerDatas[Index()].matMain;
            }
            
            for (int i = 0; i < 2; i++)
            {
                handsAnim[i].enabled = false;
                handsInteractor[i].enabled = false;
            }
        }
    }
    private void NetVarEv_End(PlayerColor previousValue, PlayerColor newValue)
    {
        if (newValue != PlayerColor.Undefined)
        {
            handsAnim[0].SetFloat("Fist", 0);
            handsAnim[1].SetFloat("Fist", 0);
            Utils.Activation(bowCurrent.gameObject, false);
        }
    }

    
    private void InputSelectLeftOn(InputAction.CallbackContext context) => handsInteractor[0].Selected = true;
    private void InputSelectLeftOff(InputAction.CallbackContext context) => handsInteractor[0].Selected = false;
    private void InputSelectRightOn(InputAction.CallbackContext context) => handsInteractor[1].Selected = true;
    private void InputSelectRightOff(InputAction.CallbackContext context) => handsInteractor[1].Selected = false;
    private void InputActivateLeftOn(InputAction.CallbackContext context) => handsInteractor[0].Activated = true;
    private void InputActivateLeftOff(InputAction.CallbackContext context) => handsInteractor[0].Activated = false;
    private void InputActivateRightOn(InputAction.CallbackContext context) => handsInteractor[1].Activated = true;
    private void InputActivateRightOff(InputAction.CallbackContext context) => handsInteractor[1].Activated = false;


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
