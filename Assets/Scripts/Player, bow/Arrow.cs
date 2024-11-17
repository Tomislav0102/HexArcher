using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    GameManager gm;
    [SerializeField] Transform tip;
    public TrailRenderer trail;
    public Transform myTransform;
    [SerializeField] Rigidbody myRigidbody;
    float _arrowLength;
    const int CONST_EdgeSideways = 15;
    const int CONST_EdgeForward = 30;
    const int CONST_EdgeBack = -10;
    const int CONST_EdgeVertical = -2;
    Ray _ray;
    RaycastHit _hit;
    RaycastHit2D _hit2D;
    Collider2D _hitCollider;

    bool _flying;
    bool _oneHitNextPlayer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gm = GameManager.Instance;
        _arrowLength = Vector3.Distance(myTransform.position, tip.position);
        myRigidbody.useGravity = false;
        gm.arrowReleased.OnValueChanged += NetVarEv_Released;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gm.arrowReleased.OnValueChanged -= NetVarEv_Released;
    }

    void NetVarEv_Released(PlayerColor previousvalue, PlayerColor newvalue)
    {
        if (previousvalue == newvalue) return;
        if (!IsOwner) return;
        _flying = true;
        myRigidbody.useGravity = true;
        myRigidbody.isKinematic = false;

        float force = gm.forceNet.Value;
        if (Mathf.Approximately(force, 0f))
        {
            force = gm.playerDatas[gm.indexInSo].playerControl.shooting.power;
        }
        print($"force is {force} and power is {gm.playerDatas[gm.indexInSo].playerControl.shooting.power}");
        myRigidbody.velocity = force * myTransform.forward;
        StartCoroutine(ArrowDirectionWhileFlying());

    }

    public void Release()
    {
        if (!IsOwner) return;
        _flying = true;
        myRigidbody.useGravity = true;
        myRigidbody.isKinematic = false;

        float force = gm.forceNet.Value;
        if (Mathf.Approximately(force, 0f))
        {
            force = gm.playerDatas[gm.indexInSo].playerControl.shooting.power;
        }
        print($"force is {force} and power is {gm.playerDatas[gm.indexInSo].playerControl.shooting.power}");
        myRigidbody.velocity = force * myTransform.forward;
        StartCoroutine(ArrowDirectionWhileFlying());
    }
    public void ReleaseByBot(Vector3 vel)
    {
        _flying = true;
        trail.colorGradient = gm.playerDatas[1].colGradientTrail;
        trail.enabled = true;
        myRigidbody.useGravity = true;
        myRigidbody.isKinematic = false;
        myRigidbody.velocity = vel;
        StartCoroutine(ArrowDirectionWhileFlying());
    }

    IEnumerator ArrowDirectionWhileFlying()
    {
        while (_flying)
        {
            Quaternion velRot = Quaternion.LookRotation(myRigidbody.velocity, Vector3.up);
            myTransform.rotation = velRot;
            yield return null;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!_flying)
        {
            bool draw = !Mathf.Approximately(0f, gm.forceNet.Value);
            gm.drawTrajectory.Trajectory(myTransform, gm.playerTurnNet.Value, myRigidbody.mass, gm.forceNet.Value, draw);
            return;
        }
        gm.drawTrajectory.Trajectory(false);
        
        if (!_oneHitNextPlayer && IsTooFarAway())
        {
            gm.NextPlayer_ServerRpc(true, $"arrow update from pos {myTransform.position}");
            _oneHitNextPlayer = true;
        }
        if (myTransform.position.y < CONST_EdgeVertical)
        {
            EndMe($"arrow {NetworkManager.Singleton.LocalClientId} fell too low");
        }
        
        bool IsTooFarAway()
        {
            return myTransform.position.z > CONST_EdgeForward ||
                   myTransform.position.z < CONST_EdgeBack ||
                   myTransform.position.x > CONST_EdgeSideways ||
                   myTransform.position.x < -CONST_EdgeSideways;
        }

    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
       // if (!_flying) return;
        CheckCollisions();
    }

    void CheckCollisions()
    {
        float extraLength = 4f; //need to compensate for fast moving arrows. Physic isn't accurate if speeds are too high
        _ray.origin = myTransform.position - extraLength * myTransform.forward;
        _ray.direction = myTransform.forward;

        _hit2D = Physics2D.GetRayIntersection(_ray, _arrowLength + extraLength, gm.layTargets);
        _hitCollider = _hit2D.collider;
        if (_hitCollider != null && _hitCollider.TryGetComponent(out ITargetForArrow tar))
        {
            _oneHitNextPlayer = true;
            tar.HitMe(gm.playerTurnNet.Value);
            print($"arrow owned by {NetworkManager.Singleton.LocalClientId} hit {_hitCollider.name}");
            gm.Destroy_ServerRpc(NetworkObject);
        }
    }

    void EndMe(string message = null)
    {
        if(!_oneHitNextPlayer)
        {
            gm.NextPlayer_ServerRpc(true, $"arrow EndMe {message}");
            _oneHitNextPlayer = true;
        }
        gm.Destroy_ServerRpc(NetworkObject);
    }

    void OnTriggerEnter(Collider other)
    {
        print($"collider arrow triggered {other.name}");
    }
}
