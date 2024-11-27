using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowReal : ArrowMain
{
    bool _oneHitNextPlayer;
    [SerializeField] Transform tip;
    [SerializeField] Rigidbody myRigidbody;
    float _arrowLength;
    const int CONST_EdgeSideways = 15;
    const int CONST_EdgeForward = 21;
    const int CONST_EdgeBack = -10;
    const int CONST_EdgeVertical = -2;
    Ray _ray;
    RaycastHit _hit;
    RaycastHit2D _hit2D;
    Collider2D _hitCollider;

    void Start()
    {
        _arrowLength = Vector3.Distance(myTransform.position, tip.position);
    }


    public override void Release(Vector3 vel)
    {
        base.Release(vel);
        myRigidbody.useGravity = true;
        myRigidbody.isKinematic = false;
     //   print($"force is {gm.forceArrow} and power is {gm.playerDatas[gm.indexInSo].playerControl.shooting.power}");
        if(vel == Vector3.zero) myRigidbody.velocity = gm.forceArrow * myTransform.forward;
        else   myRigidbody.velocity = vel;
        StartCoroutine(ArrowDirectionWhileFlying(myRigidbody.velocity.magnitude > 0f));
    }


    IEnumerator ArrowDirectionWhileFlying(bool rotateObject = true)
    {
        while (myArrowState == ArrowState.Flying && rotateObject)
        {
            Quaternion velRot = Quaternion.LookRotation(myRigidbody.velocity, Vector3.up);
            myTransform.rotation = velRot;
            yield return null;
        }
    }

    void Update()
    {
        gm.ShadowArrow_NotMeRpc(myTransform.position, myTransform.rotation);
        
        if (myArrowState == ArrowState.Notched)
        {
            bool draw = !Mathf.Approximately(0f, gm.forceArrow);
            gm.drawTrajectory.Trajectory(myTransform, gm.playerTurnNet.Value, myRigidbody.mass, gm.forceArrow, draw);
            return;
        }
        gm.drawTrajectory.Trajectory(false);
        
        if (!_oneHitNextPlayer && IsTooFarAway())
        {
            _oneHitNextPlayer = true;
            gm.NextPlayer_ServerRpc(true, $"arrow update from pos {myTransform.position}");
            EndMe();
        }
        if (myTransform.position.y < CONST_EdgeVertical)
        {
            EndMe(("arrow fell too low"));
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
        if (myArrowState == ArrowState.Flying) CheckCollisions();
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
          //  print(gm.playerDatas[gm.indexInSo].playerColor);
          //  tar.HitMe(gm.playerDatas[gm.indexInSo].playerColor);
          print(gm.playerTurnNet.Value);
            tar.HitMe(gm.playerTurnNet.Value);
            _oneHitNextPlayer = true;
            EndMe(($"arrow hit {_hitCollider.name}"));
        }
    }


    void EndMe(string message = null)
    {
       // if (!string.IsNullOrEmpty(message)) print(message);
        if (!_oneHitNextPlayer) gm.NextPlayer_ServerRpc();
        Destroy(gameObject);
    }

}
