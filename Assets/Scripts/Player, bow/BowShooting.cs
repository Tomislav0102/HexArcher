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
    bool _oneHitArrowNothced;
    public bool controllerPullingString;


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


    private void Update()
    {
        if(!gm.MyTurn()) return;

        if (controllerPullingString)
        {
            if (!_oneHitArrowNothced && gm.oneShotPerTurnNet.Value)
            {
                _oneHitArrowNothced = true;
                gm.SpawnArrow_ServerRpc(NetworkManager.Singleton.LocalClientId, _myTransform.position, _myTransform.rotation);
            }
            if (_oneHitArrowNothced && _pullAmount > 0.3f && !_oneHitAudioDraw)
            {
                _oneHitAudioDraw = true;
                gm.audioManager.PlayOnMyAudioSource(audioSource, gm.audioManager.bowDraw);
            }

        }
        else
        {
            ReleaseString();
        }

        if(Input.GetKeyDown(KeyCode.K)) TestShoot();
    }
    private void LateUpdate()
    {
        if (!controllerPullingString || !gm.MyTurn() || gm.spawnedArrow == null) return;
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

        gm.spawnedArrow.myTransform.SetPositionAndRotation(endPoint, rot);
        _pullAmount = (_myTransform.position - endPoint).magnitude / _maxLength;
      //  _pullAmount = Mathf.Clamp(_pullAmount, 0f, 1f);
        if (gm.drawTrajectory.showTrajectory) gm.SetForceNetRpc(_pullAmount * power); 

        playerControl.LineRendererLengthRpc(_myTransform.InverseTransformPoint(endPoint));
    }

    public void ReleaseString()
    {
        if (gm.spawnedArrow == null) return;

        gm.DisableShootingRpc();
        gm.SetForceNetRpc(_pullAmount * power);
        gm.ShowTrails_EveryoneRpc(gm.indexInSo);
        gm.spawnedArrow.Release();
        _pullAmount = 0.0f;

        controllerPullingString = false;
        _oneHitArrowNothced = false;
        gm.spawnedArrow = null;
        playerControl.LineRendererLengthRpc(Vector3.zero);

        gm.audioManager.PlayOnMyAudioSource(audioSource, gm.audioManager.bowRelease);
        _oneHitAudioDraw = false;
    }


    [ContextMenu("Shoot")]
    void TestShoot()
    {
        gm.DisableShootingRpc();
        gm.SetForceNetRpc(power);
        gm.SpawnArrow_ServerRpc(NetworkManager.Singleton.LocalClientId, _myTransform.position, _myTransform.rotation);
        gm.ShowTrails_EveryoneRpc(gm.indexInSo);
    }
}

