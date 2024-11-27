using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;


public class BowShooting : MonoBehaviour
{
    GameManager gm;
    [SerializeField] BowControl bow;
    [SerializeField] PlayerControl playerControl;
    [SerializeField] AudioSource audioSource;
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
    
    void Awake()
    {
        gm = GameManager.Instance;
        _myTransform = transform;
    }

    private void Start()
    {
        _offsetStart = Mathf.Abs(_myTransform.localPosition.z);
        _maxLength = (end.localPosition - notch.localPosition).magnitude;
        Utils.DeActivateGo(end.gameObject);
    }

    void OnEnable()
    {
        gm.playerTurnNet.OnValueChanged += NetVarEv_PlayerTurn;
    }

    void OnDisable()
    {
        gm.playerTurnNet.OnValueChanged -= NetVarEv_PlayerTurn;
    }
    private void NetVarEv_PlayerTurn(PlayerColor previousValue, PlayerColor newValue)
    {
        _oneHitArrowNotched = false;
    }

    private void Update()
    {
        if(!gm.MyTurn()) return;

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
                gm.audioManager.PlayOnMyAudioSource(audioSource, gm.audioManager.bowDraw);
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
        if (!controllerPullingString || !gm.MyTurn() || gm.arrowReal == null) return;
        ProcessNotchedArrow();
    }

    void ProcessNotchedArrow()
    {
        int handThaPullsString = ((int)playerControl.SideThatHoldsBow() + 1) % 2;
        Vector3 handPos = playerControl.handInteractors[handThaPullsString].myTransform.position;
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
        _pullAmount = (_myTransform.position - endPoint).magnitude / _maxLength;
        gm.forceArrow = _pullAmount * power; 

        playerControl.LineRendererLengthRpc(_myTransform.InverseTransformPoint(endPoint));
    }

    public void ReleaseString()
    {
        if (gm.arrowReal == null) return;
        gm.arrowReal.Release(Vector3.zero);
        _pullAmount = 0.0f;
        controllerPullingString = false;
        _oneHitArrowNotched = false;
        gm.arrowReal = null;
        playerControl.LineRendererLengthRpc(Vector3.zero);

        gm.audioManager.PlayOnMyAudioSource(audioSource, gm.audioManager.bowRelease);
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

