using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public class BowRack
{
    [SerializeField] Transform meshTr;
    public Transform spawnPoint;
    const float CONST_StartY = -1.173f;
    const int CONST_RiseSpeed = 2;
    const float CONST_FallSpeed = 0.5f;
    Tween _tRise, _tFall;

    void KillAllTweens()
    {
        if (_tRise != null && _tRise.IsActive()) _tRise.Kill();
        if (_tFall != null && _tFall.IsActive()) _tFall.Kill();
    }
    public void EnterRack(System.Action callBackAtEnd)
    {
        KillAllTweens();
        _tRise = meshTr.DOLocalMoveY(0f, CONST_RiseSpeed)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                callBackAtEnd?.Invoke();
            });
    }
    public void HideRack()
    {
        KillAllTweens();
        _tFall = meshTr.DOLocalMoveY(CONST_StartY, CONST_FallSpeed);
    }


}