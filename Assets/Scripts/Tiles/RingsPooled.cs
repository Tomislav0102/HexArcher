using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RingsPooled : MonoBehaviour
{
    GameManager gm;
    Transform _myTransform;
    Transform[] _transforms;
    MeshRenderer[] _meshes;
    const float CONST_MOVEHEX = 1f;
    const float CONST_TIME = 0.2f;

    private void Awake()
    {
        gm= GameManager.Instance;
        _myTransform = transform;
        _transforms = Utils.AllChildren<Transform>(_myTransform);
        _meshes = Utils.AllChildren<MeshRenderer>(_myTransform);
    }

    public void SpawnMe(Vector3 spawPos, sbyte val, System.Action callbackAtFinish = null)
    {
        _myTransform.position = spawPos;
        Material mat = null;
        if (val > 0) mat = gm.playerDatas[0].matMain;
        else if (val < 0) mat = gm.playerDatas[1].matMain;
        else mat = gm.playerDatas[2].matMain;
        byte ordinal = (byte)Mathf.Abs(val);
        _meshes[ordinal].material = mat;
        _meshes[ordinal].enabled = true;
        _transforms[ordinal].DOLocalMoveY(CONST_MOVEHEX, CONST_TIME)
                                    .SetLoops(2, LoopType.Yoyo)
                                    .OnComplete(() =>
                                    {
                                        _meshes[ordinal].enabled = false;
                                        callbackAtFinish?.Invoke();
                                    });
    }
    public void SpawnMeOnClient(Vector3 spawPos, sbyte val)
    {
        _myTransform.position = spawPos;
        Material mat = null;
        if (val > 0) mat = gm.playerDatas[0].matMain;
        else if (val < 0) mat = gm.playerDatas[1].matMain;
        else mat = gm.playerDatas[2].matMain;
        byte ordinal = (byte)Mathf.Abs(val);
        _meshes[ordinal].material = mat;
        _meshes[ordinal].enabled = true;
        _transforms[ordinal].DOLocalMoveY(CONST_MOVEHEX, CONST_TIME)
                                    .SetLoops(2, LoopType.Yoyo)
                                    .OnComplete(() =>
                                    {
                                        _meshes[ordinal].enabled = false;
                                    });
    }
}
