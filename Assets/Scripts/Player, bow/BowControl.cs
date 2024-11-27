//using Sirenix.OdinInspector
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BowControl : MonoBehaviour
{
    GameManager gm;
    public SoBowData bowData;
    [SerializeField] PlayerControl playerControl;
    [SerializeField] GameObject bowBody;
    [SerializeField] Transform myTransform;
    [SerializeField] Rigidbody myRigid;
    [SerializeField] Transform attachPoint;
    [SerializeField] BoxCollider myCollider;
    Transform _rackParent;

    Vector3 _diffPos, _lastPosition;
    const float CONST_MINY = -2f;
    const int CONST_THROWVELOCITY = 80;

    public BowState Bstate
    {
        get => _bowState;
        set
        {
            myRigid.isKinematic = true;
            playerControl.shooting.myCollider.enabled = false;
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
                    gm.bowRacks[gm.indexInSo].HideRack();
                    playerControl.shooting.myCollider.enabled = true;
                    break;

                case BowState.Free:
                    playerControl.shooting.ReleaseString();
                    interactor = null;
                    myRigid.isKinematic = false;
                    myRigid.velocity = CONST_THROWVELOCITY * _diffPos;
                    break;
            }
        }
    }
    /*[ShowInInspector]*//*[ReadOnly]*/ BowState _bowState;
    public PlayerInteractor interactor;


    private void Awake()
    {
        gm = GameManager.Instance;
        playerControl.shooting.power = bowData.power;

        _rackParent = gm.bowRacks[gm.indexInSo].spawnPoint;
    }
    private void OnEnable()
    {
        Utils.GameStarted += ReturnBowToRack;
    }
    private void OnDisable()
    {
        Utils.GameStarted -= ReturnBowToRack;
    }

    private void Update()
    {
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
                if (myTransform.position.y < CONST_MINY) ReturnBowToRack();
                break;
        }

    }
    void ReturnBowToRack()
    {
        myRigid.velocity = Vector3.zero;
        Bstate = BowState.RackMoving;
        gm.bowRacks[gm.indexInSo].EnterRack(() =>
        {
            Bstate = BowState.RackDone;
        });

    }
}
