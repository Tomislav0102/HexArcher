using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowLocal : ArrowMain
{

    [SerializeField] Transform tip;
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

    void Start()
    {
        _arrowLength = Vector3.Distance(myTransform.position, tip.position);
        myRigidbody.useGravity = false;
    }


    public void Release()
    {
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
        gm.DataPass_NotMeRpc(myTransform.position, myTransform.rotation);
        
        if (!_flying)
        {
            bool draw = !Mathf.Approximately(0f, gm.forceNet.Value);
            gm.drawTrajectory.Trajectory(myTransform, gm.playerTurnNet.Value, myRigidbody.mass, gm.forceNet.Value, draw);
            return;
        }
        gm.drawTrajectory.Trajectory(false);
        
        if (!oneHitNextPlayer && IsTooFarAway())
        {
            bool b = myTransform.position.z > CONST_EdgeForward ||
                     myTransform.position.z < CONST_EdgeBack ||
                     myTransform.position.x > CONST_EdgeSideways ||
                     myTransform.position.x < -CONST_EdgeSideways;
            gm.NextPlayer_ServerRpc(true, $"arrow update from pos {myTransform.position}");
        }
        if (myTransform.position.y < CONST_EdgeVertical)
        {
            DestroyMe($"arrow fell too low");
        }
        
        bool IsTooFarAway()
        {
            oneHitNextPlayer = true;
            return myTransform.position.z > CONST_EdgeForward ||
                   myTransform.position.z < CONST_EdgeBack ||
                   myTransform.position.x > CONST_EdgeSideways ||
                   myTransform.position.x < -CONST_EdgeSideways;
        }

    }

    private void FixedUpdate()
    {
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
            oneHitNextPlayer = true;
            tar.HitMe(gm.playerTurnNet.Value);
            print($"arrow hit {_hitCollider.name}");
        }
    }

}
