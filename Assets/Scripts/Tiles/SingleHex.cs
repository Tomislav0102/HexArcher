using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleHex : MonoBehaviour, ITargetForArrow
{
    ParentHex _mParentHex; 

    void Awake()
    {
        _mParentHex = transform.parent.parent.GetComponent<ParentHex>();
    }

    public void HitMe()
    {
        if (_mParentHex != null) _mParentHex.HexHit((sbyte)(transform.GetSiblingIndex() + 1));
    }
}
