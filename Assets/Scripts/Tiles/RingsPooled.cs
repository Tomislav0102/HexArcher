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
    float _moveHex = 1f;
    float _time = 0.2f;

    private void Awake()
    {
        gm= GameManager.Instance;
        _myTransform = transform;
        _transforms = Utils.AllChildren<Transform>(_myTransform);
        _meshes = Utils.AllChildren<MeshRenderer>(_myTransform);
    }

    public void SpawnMe(Vector3 spawnPos, sbyte val, System.Action callbackAtFinish = null)
    {
        _myTransform.position = spawnPos;
        Material mat = null;
        if (val > 0) mat = gm.playerDatas[0].matMain;
        else if (val < 0) mat = gm.playerDatas[1].matMain;
        else mat = gm.playerDatas[2].matMain;
        byte ordinal = (byte)Mathf.Abs(val);
        _meshes[ordinal].material = mat;
        _meshes[ordinal].enabled = true;
        _transforms[ordinal].DOLocalMoveY(_moveHex, _time)
                                    .SetLoops(2, LoopType.Yoyo)
                                    .OnComplete(() =>
                                    {
                                        _meshes[ordinal].enabled = false;
                                        callbackAtFinish?.Invoke();
                                    });
    }
    public void SpawnMeOnClient(Vector3 spawnPos, sbyte val)
    {
        _myTransform.position = spawnPos;
        Material mat = val switch
        {
            > 0 => gm.playerDatas[0].matMain, //if
            < 0 => gm.playerDatas[1].matMain, //else if
            _ => gm.playerDatas[2].matMain //else
        };
        byte ordinal = (byte)Mathf.Abs(val);
        _meshes[ordinal].material = mat;
        _meshes[ordinal].enabled = true;
        _transforms[ordinal].DOLocalMoveY(_moveHex, _time)
                                    .SetLoops(2, LoopType.Yoyo)
                                    .OnComplete(() =>
                                    {
                                        _meshes[ordinal].enabled = false;
                                    });
    }
}
