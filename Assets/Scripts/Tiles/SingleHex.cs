using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleHex : MonoBehaviour, ITargetForArrow
{
    public Transform MyMainTransform { get; set; }
    ParentHex _mParentHex; 
    Transform _myTransform;
    sbyte _ordinal;

    void Awake()
    {
        _myTransform = transform;
        MyMainTransform = _myTransform.parent.parent;
        _mParentHex = MyMainTransform.GetComponent<ParentHex>();
    }
    void Start()
    {
        _ordinal = (sbyte)(_myTransform.GetSiblingIndex() + 1);
    }

    public void HitMe()
    {
        if (_mParentHex != null) _mParentHex.HexHit(_ordinal);
    }
}
