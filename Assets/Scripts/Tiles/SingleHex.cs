using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleHex : MonoBehaviour, ITargetForArrow
{
    public Transform MyMainTransform { get; set; }
    ParentHex _mParentHex; 
    Transform _myTransform;

    void Awake()
    {
        _myTransform = transform;
        MyMainTransform = _myTransform.parent.parent;
        _mParentHex = MyMainTransform.GetComponent<ParentHex>();
    }

    public void HitMe(PlayerColor arrowColor)
    {
        if (_mParentHex != null) _mParentHex.HexHit((sbyte)(_myTransform.GetSiblingIndex() + 1), arrowColor);
    }
}
