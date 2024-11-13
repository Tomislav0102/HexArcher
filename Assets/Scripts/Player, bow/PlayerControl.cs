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
            VrRigRef.instance.leftController.selectAction.action.performed += InputSelectLeftON;
            VrRigRef.instance.leftController.selectAction.action.canceled += InputSelectLeftOFF;
            VrRigRef.instance.rightController.selectAction.action.performed += InputSelectRightON;
            VrRigRef.instance.rightController.selectAction.action.canceled += InputSelectRightOFF;
            VrRigRef.instance.leftController.activateAction.action.performed += InputActivateLeftON;
            VrRigRef.instance.leftController.activateAction.action.canceled += InputActivateLeftOFF;
            VrRigRef.instance.rightController.activateAction.action.performed += InputActivateRightON;
            VrRigRef.instance.rightController.activateAction.action.canceled += InputActivateRightOFF;

            VrRigRef.instance.root.SetPositionAndRotation(gm.bowTablesNet[gm.indexInSo].transform.position, 
                gm.bowTablesNet[gm.indexInSo].transform.rotation);

            Utils.FadeOut += CallEv_FadeMethod;

            Utils.DeActivateGo(displayNameTr.gameObject);
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
        gm.RegisterPlayerRpc(NetworkObject);

    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            gm.playerVictoriousNet.OnValueChanged -= NetVarEv_End;
            VrRigRef.instance.leftController.selectAction.action.performed -= InputSelectLeftON;
            VrRigRef.instance.leftController.selectAction.action.canceled -= InputSelectLeftOFF;
            VrRigRef.instance.rightController.selectAction.action.performed -= InputSelectRightON;
            VrRigRef.instance.rightController.selectAction.action.canceled -= InputSelectRightOFF;
            VrRigRef.instance.leftController.activateAction.action.performed -= InputActivateLeftON;
            VrRigRef.instance.leftController.activateAction.action.canceled -= InputActivateLeftOFF;
            VrRigRef.instance.rightController.activateAction.action.performed -= InputActivateRightON;
            VrRigRef.instance.rightController.activateAction.action.canceled -= InputActivateRightOFF;

            Utils.FadeOut -= CallEv_FadeMethod;
        }
        base.OnNetworkDespawn();
    }

    private void Start()
    {
        if (gm.playerDatas[1].playerControl != null)
        {
            ChangeNameRpc();
        }
        for (int i = 0; i < headMeshes.Length; i++)
        {
            headMeshes[i].material = gm.playerDatas[gm.indexInSo].matMain;
        }

        if (Utils.GameType == MainGameType.Singleplayer && !Utils.PracticeSp)
        {
            Utils.GameStarted?.Invoke();
        }
    }
    [Rpc(SendTo.Everyone)]
    void ChangeNameRpc()
    {
        for (int i = 0; i < 2; i++)
        {
            string levelDisplay = string.IsNullOrEmpty(gm.playerDatas[i].myLevel) ? "" : "Level - " + gm.playerDatas[i].myLevel.ToString();
            string rankDisplay = gm.playerDatas[i].myLeaderboardRank > 0 ? "Leaderboard - " + gm.playerDatas[i].myLeaderboardRank.ToString() : "";
            gm.playerDatas[i].playerControl.displayNameTr.GetComponent<TextMeshPro>().text = gm.playerDatas[i].myName + "\n" + levelDisplay + "\n" + rankDisplay;
        }
    }
    [Rpc(SendTo.Everyone)]
    public void LineRendererLengthRpc(Vector3 pos) => _lrShooting.SetPosition(1, pos);

    private void Update()
    {
        if (!IsOwner)
        {
            NameDisplaying();
            return;
        }
        transform.SetPositionAndRotation(VrRigRef.instance.root.position, VrRigRef.instance.root.rotation);
        head.SetPositionAndRotation(VrRigRef.instance.head.position, VrRigRef.instance.head.rotation);
        leftHand.SetPositionAndRotation(VrRigRef.instance.leftHand.position, VrRigRef.instance.leftHand.rotation);
        rightHand.SetPositionAndRotation(VrRigRef.instance.rightHand.position, VrRigRef.instance.rightHand.rotation);


        //if (Input.GetKeyDown(KeyCode.Return))
        //{
        //    gm.NextPlayerRpc(false);
        //}
    }

    void NameDisplaying()
    {
        if (gm.playerDatas[1].playerControl == null) return;
        Vector3 pos = gm.playerDatas[gm.indexInSo].playerControl.transform.position;
        displayNameTr.LookAt(new Vector3(pos.x, displayNameTr.position.y, pos.z));
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

    private void InputSelectLeftON(InputAction.CallbackContext context) => handInteractors[0].Selected = true;
    private void InputSelectLeftOFF(InputAction.CallbackContext context) => handInteractors[0].Selected = false;
    private void InputSelectRightON(InputAction.CallbackContext context) => handInteractors[1].Selected = true;
    private void InputSelectRightOFF(InputAction.CallbackContext context) => handInteractors[1].Selected = false;
    private void InputActivateLeftON(InputAction.CallbackContext context) => handInteractors[0].Activated = true;
    private void InputActivateLeftOFF(InputAction.CallbackContext context) => handInteractors[0].Activated = false;
    private void InputActivateRightON(InputAction.CallbackContext context) => handInteractors[1].Activated = true;
    private void InputActivateRightOFF(InputAction.CallbackContext context) => handInteractors[1].Activated = false;


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
