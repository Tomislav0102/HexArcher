//using Sirenix.OdinInspector
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BowControl : MonoBehaviour, ILateInitialization<PlayerControl>
{
    GameManager gm;
    public SoBowData bowData;
    PlayerControl _playerControl;
    public bool IsInitialized { get; set; }
    [SerializeField] GameObject bowBody;
    [SerializeField] Transform myTransform;
    [SerializeField] Rigidbody myRigid;
    [SerializeField] Transform attachPoint;
    [SerializeField] BoxCollider myCollider;
    Transform _rackParent;
    bool _oneHitStartRackMoving;
    Vector3 _diffPos, _lastPosition;
    const float CONST_MinY = -2f;
    const int CONST_ThrowVelocity = 80;

    public BowState Bstate
    {
        get => _bowState;
        set
        {
            myRigid.isKinematic = true;
            _playerControl.shootingCurrent.myCollider.enabled = false;
            myCollider.enabled = true;
            _bowState = value;
            switch (value)
            {
                case BowState.RackMoving:
                    interactor = null;
                    myCollider.enabled = false;
                    myTransform.SetPositionAndRotation(_rackParent.position, _rackParent.rotation);
                    break;

                case BowState.RackDone:
                    break;

                case BowState.InHand:
                    gm.bowRacks[NetworkManager.Singleton.IsHost ? 0 : 1].HideRack();
                    _playerControl.shootingCurrent.myCollider.enabled = true;
                    break;

                case BowState.Free:
                    _playerControl.shootingCurrent.ReleaseString();
                    interactor = null;
                    myRigid.isKinematic = false;
                    myRigid.velocity = CONST_ThrowVelocity * _diffPos;
                    break;
            }
        }
    }
    /*[ShowInInspector]*//*[ReadOnly]*/ BowState _bowState;
    public PlayerInteractor interactor;



    public void InitializeMe(PlayerControl playerControl)
    {
        _playerControl = playerControl;
        gm = GameManager.Instance;
        _playerControl.shootingCurrent.power = bowData.power;
        _rackParent = gm.bowRacks[NetworkManager.Singleton.IsHost ? 0 : 1].spawnPoint;
        Utils.GameStarted += ReturnBowToRack_Initial;
        if (Utils.GameType == MainGameType.Singleplayer && Utils.SinglePlayerType == SpType.Endless) ReturnBowToRack_Initial();
        IsInitialized = true;
    }


    private void OnDisable()
    {
        Utils.GameStarted -= ReturnBowToRack_Initial;
    }

    private void Update()
    {
        if (!IsInitialized) return;
        switch (Bstate)
        {
            case BowState.RackMoving:
                myTransform.SetPositionAndRotation(_rackParent.position, _rackParent.rotation);
                break;
            case BowState.InHand:
                if (interactor == null) return;
                myTransform.SetPositionAndRotation(interactor.myTransform.position - attachPoint.localPosition, interactor.myTransform.rotation * Quaternion.Inverse(attachPoint.localRotation));
                _diffPos = myTransform.position - _lastPosition;
                _lastPosition = myTransform.position;
                break;
            case BowState.Free:
                if (myTransform.position.y < CONST_MinY) ReturnBowToRack();
                break;
        }

    }
    void ReturnBowToRack()
    {
        myRigid.velocity = Vector3.zero;
        Bstate = BowState.RackMoving;
        gm.bowRacks[NetworkManager.Singleton.IsHost ? 0 : 1].EnterRack(() =>
        {
            Bstate = BowState.RackDone;
        });
    }

    void ReturnBowToRack_Initial()
    {
        if(_oneHitStartRackMoving) return;
        _oneHitStartRackMoving = true;
        ReturnBowToRack();
    }
}
