using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;


public class BowShooting : MonoBehaviour, ILateInitialization<PlayerControl>
{
    GameManager gm;
    BowControl _bow;
    PlayerControl _playerControl;
    public bool IsInitialized { get; set; }
    AudioSource _audioSource;
    bool _oneHitAudioDraw;
    public BoxCollider myCollider;
    [SerializeField] Transform end;
    float _maxLength, _offsetStart;
    [SerializeField] Transform notch;
    float _pullAmount;
    Transform _myTransform;

    [ReadOnly] public float power;
    bool _oneHitArrowNotched;
    public bool controllerPullingString;

    [SerializeField] bool vRSimulation = true; //only for keyboard control
    
    
    public void InitializeMe(PlayerControl playerControl)
    {
        _playerControl = playerControl;
        _bow = playerControl.bowCurrent;
        _audioSource = _bow.GetComponent<AudioSource>();
        gm = GameManager.Instance;
        _myTransform = transform;
        _offsetStart = Mathf.Abs(_myTransform.localPosition.z);
        //_maxLength = (end.localPosition - notch.localPosition).magnitude;
        _maxLength =  Mathf.Abs(end.localPosition.z - _myTransform.localPosition.z);
        gm.playerTurnNet.OnValueChanged += NetVarEv_PlayerTurn;
        IsInitialized = true;
    }



    void OnDisable()
    {
        if (!IsInitialized) return;
        gm.playerTurnNet.OnValueChanged -= NetVarEv_PlayerTurn;
    }
    private void NetVarEv_PlayerTurn(PlayerColor previousValue, PlayerColor newValue)
    {
        _oneHitArrowNotched = false;
    }

    private void Update()
    {
        if (!IsInitialized || !gm.MyTurn()) return;

        if (controllerPullingString)
        {
            if (!_oneHitArrowNotched)
            {
                _oneHitArrowNotched = true;
               gm.SpawnRealArrow(_myTransform.position, _myTransform.rotation);
            }
            
            //audio
            if (_oneHitArrowNotched && _pullAmount > 0.3f && !_oneHitAudioDraw)
            {
                _oneHitAudioDraw = true;
                gm.audioManager.PlayOnMyAudioSource(_audioSource, gm.audioManager.bowDraw);
            }

        }
        else
        {
            if (!vRSimulation) ReleaseString();
        }
        
        if (Input.GetKeyDown(KeyCode.K) && vRSimulation) TestShooting();

    }
    private void LateUpdate()
    {
        if (!IsInitialized || !controllerPullingString || !gm.MyTurn() || gm.arrowReal == null) return;
        ProcessNotchedArrow();
    }

    void ProcessNotchedArrow()
    {
        int handThaPullsString = ((int)_playerControl.SideThatHoldsBow() + 1) % 2;
        Vector3 handPos = _playerControl.handsInteractorCurrent[handThaPullsString].myTransform.position;
        Vector3 pullDir = notch.position - handPos;

        Vector3 endPoint;
        Quaternion rot;
        if (_myTransform.InverseTransformPoint(handPos).z > 0f)
        {
            endPoint = _myTransform.position;
            rot = _myTransform.rotation;
        }
        else if (pullDir.magnitude < _maxLength + _offsetStart)
        {
            endPoint = handPos;
            rot = Quaternion.LookRotation(pullDir.normalized);
        }
        else
        {
            endPoint = notch.position - (_maxLength + _offsetStart) * pullDir.normalized;
            rot = Quaternion.LookRotation(pullDir.normalized);
        }

        gm.arrowReal.transform.SetPositionAndRotation(endPoint, rot);
       // _pullAmount = (_myTransform.position - endPoint).magnitude / _maxLength;
        _pullAmount = (Mathf.Abs(_myTransform.InverseTransformPoint(endPoint).z)) / _maxLength;
        gm.forceArrow = _pullAmount * power; 

        _playerControl.LineRendererLength_EveryoneRpc(_myTransform.InverseTransformPoint(endPoint));
    }

    public void ReleaseString()
    {
        if (gm.arrowReal == null) return;
        gm.arrowReal.Release(Vector3.zero);
        _pullAmount = 0.0f;
        controllerPullingString = false;
        _oneHitArrowNotched = false;
        gm.arrowReal = null;
        _playerControl.LineRendererLength_EveryoneRpc(Vector3.zero);

        gm.audioManager.PlayOnMyAudioSource(_audioSource, gm.audioManager.bowRelease);
        _oneHitAudioDraw = false;
    }

    void TestShooting()
    {
        _oneHitArrowNotched = true;
        gm.SpawnRealArrow(_myTransform.position, _myTransform.rotation);
        gm.forceArrow = 1 * power;
        gm.arrowReal.Release(Vector3.zero);
    }


}

// void ProcessNotchedArrow()
// {
//     int handThaPullsString = ((int)playerControl.SideThatHoldsBow() + 1) % 2;
//     Vector3 handPos = playerControl.handInteractors[handThaPullsString].myTransform.position;
//     Vector3 pullDir = notch.position - handPos;
//
//     Vector3 endPoint;
//     Quaternion rot;
//     if (_myTransform.InverseTransformPoint(handPos).z > 0f)
//     {
//         endPoint = _myTransform.position;
//         rot = _myTransform.rotation;
//     }
//     else if (pullDir.magnitude < _maxLength + _offsetStart)
//     {
//         endPoint = handPos;
//         rot = Quaternion.LookRotation(pullDir.normalized);
//     }
//     else
//     {
//         endPoint = notch.position - (_maxLength + _offsetStart) * pullDir.normalized;
//         rot = Quaternion.LookRotation(pullDir.normalized);
//     }
//
//     gm.arrowReal.transform.SetPositionAndRotation(endPoint, rot);
//     _pullAmount = (_myTransform.position - endPoint).magnitude / _maxLength;
//     gm.forceArrow = _pullAmount * power; 
//
//     playerControl.LineRendererLength_EveryoneRpc(_myTransform.InverseTransformPoint(endPoint));
// }

