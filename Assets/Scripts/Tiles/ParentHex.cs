using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ParentHex : TileParent
{
    GameManager gm;

    [Title("Class specific")]
    public Transform center;

    [SerializeField] AudioSource aSource;
    [SerializeField] Transform parColliders, parDisplays, parSpritesOrange, parSpritesTeal;
    [SerializeField] GameObject trajectoryCollider;
    Collider2D[] _colliders;
    GameObject[] _displayValues;
    GameObject[] _spritesOrange, _spritesTeal;
    const int CONST_HexNum = 10;
    byte _mySiblingsIndex;

    [Title("Final Hex")]
    [SerializeField] Transform finalHexagonTransform;

    MeshRenderer _finalHexMesh;
    [SerializeField] TextMeshPro finalHexScore;
    [SerializeField] MeshRenderer finalHexFrameMesh;

    
    public TileState Tstate
    {
        get => _tState;
        private set
        {
            _tState = value;
            gm.SetGridTileStateNet_ServerRpc(_mySiblingsIndex, (byte)_tState);
            HexDefinition_EveryoneRpc((byte)_tState, CurrentValue);
            gm.Scoring_ServerRpc();
            switch (_tState)
            {
                case TileState.Free:
                    break;
                case TileState.InActive:
                    InactiveHex();
                    return;
                case TileState.Taken:
                    _allAnimationsDone?.Invoke();
                    break;
            }
        }
    }

    public sbyte CurrentValue
    {
        get => _currentValue;
        private set
        {
            _currentValue = value;
            byte absoluteValue = (byte)Mathf.Abs(_currentValue);
            if (absoluteValue > 9 || Tstate == TileState.Taken)
            {
                _currentValue = _hitValue;
                Tstate = TileState.Taken;
            }
            else HexDefinition_EveryoneRpc((byte)Tstate, _currentValue);
            gm.SetGridValuesNet_ServerRpc(_mySiblingsIndex, _currentValue);

        }
    }
    [ShowInInspector]
    [ReadOnly]
    sbyte _currentValue;

    [ShowInInspector]
    [ReadOnly]
    List<ParentHex> _myNeighbours;


    /// <summary>
    /// Turn is over after all this hex and all its neighbours have updated their 'CurrentValue'.
    /// </summary>
    int NeighboursCheckIn
    {
        get => _neighboursCheckIn;
        set
        {
            _neighboursCheckIn = value;
            gm.audioManager.PlaySFX(gm.audioManager.hexCounter, finalHexagonTransform);
            if (value <= 0) //all neighbours have finished their animations
            {
                if (_oneShotNeighbourCheckIn) return;
                _oneShotNeighbourCheckIn = true;
                gm.NextPlayer_ServerRpc(false, "NeighboursCheckIn");
            }
        }
    }

    [ShowInInspector]
    [ReadOnly]
    int _neighboursCheckIn;

    bool _oneShotNeighbourCheckIn; //safety bool, makes sure '_gm.NextPlayer()' is triggered only once, probably redundant
    System.Action _allAnimationsDone; //event used to communicate between hex and its neighbours
    sbyte _hitValue;

    [HideInInspector] public int valueForBot;

    [Title("Testing")]
    public sbyte hitValueTest = 8;

    [Button]
    void TestHit() => HexHit(hitValueTest);


    private void Awake()
    {
        gm = GameManager.Instance;
        _colliders = Utils.AllChildren<Collider2D>(parColliders);
        _displayValues = Utils.AllChildrenGameObjects(parDisplays);
        _spritesOrange = Utils.AllChildrenGameObjects(parSpritesOrange);
        _spritesTeal = Utils.AllChildrenGameObjects(parSpritesTeal);
        _finalHexMesh = finalHexagonTransform.GetComponent<MeshRenderer>();
        _mySiblingsIndex = (byte)transform.GetSiblingIndex();
    }

    void InactiveHex()
    {
        spriteRenderer.enabled = false;
        Utils.Activation(parColliders.gameObject, false);
        Utils.Activation(parDisplays.gameObject, false);
        Utils.Activation(parSpritesOrange.gameObject, false);
        Utils.Activation(parSpritesTeal.gameObject, false);
        Utils.Activation(finalHexagonTransform.gameObject, false);
        Utils.Activation(trajectoryCollider, false);
    }
    public void HexHit(sbyte ordinal)
    {
        if (Tstate == TileState.InActive || Tstate == TileState.Taken) return;
        gm.audioManager.PlayOnMyAudioSource(aSource, gm.audioManager.hexHit);

        int mod = gm.playerTurnNet.Value == PlayerColor.Blue ? 1 : -1;
        CurrentValue = _hitValue = (sbyte)(ordinal * mod);
        Tstate = TileState.Taken;

        _myNeighbours = gm.gridManager.AllNeighbours(pos);
        if (_myNeighbours.Count == 0)
        {
            gm.NextPlayer_ServerRpc(false, "HexHit");
            return;
        }
        NeighboursCheckIn = _myNeighbours.Count;
        foreach (ParentHex item in _myNeighbours)
        {
            item.ActivateFromDirectHit(CurrentValue, () => NeighboursCheckIn--);
        }
    }

    public void HexDefinition(byte tileStateByte, sbyte valueByte)
    {
        _tState = (TileState)tileStateByte;
        _currentValue = valueByte;

        sbyte positiveValue = (sbyte)Mathf.Abs(valueByte);
        finalHexScore.enabled = false;
        spriteRenderer.enabled = false;
        for (int i = 0; i < CONST_HexNum; i++)
        {
            _colliders[i].enabled = false;
        }

        switch ((TileState)tileStateByte)
        {
            case TileState.Free:
                switch (valueByte)
                {
                    case > 0:
                        Utils.ActivateOneArrayElement(_displayValues, positiveValue);
                        _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
                        Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
                        break;
                    case < 0:
                        Utils.ActivateOneArrayElement(_displayValues, positiveValue);
                        _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
                        Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
                        break;
                    default:
                        spriteRenderer.enabled = true;
                        Utils.ActivateOneArrayElement(_spritesTeal);
                        Utils.ActivateOneArrayElement(_spritesOrange);
                        Utils.ActivateOneArrayElement(_displayValues);
                        break;
                }
                for (int i = positiveValue; i < CONST_HexNum; i++)
                {
                    _colliders[i].enabled = true;
                }
                break;

            case TileState.InActive:
                InactiveHex();
                return;

            case TileState.Taken:
                if (positiveValue != 0)
                {
                    Utils.ActivateOneArrayElement(_spritesTeal);
                    Utils.ActivateOneArrayElement(_spritesOrange);
                    Utils.ActivateOneArrayElement(_displayValues);
                    finalHexScore.text = positiveValue.ToString();
                    finalHexScore.color = gm.playerDatas[valueByte > 0 ? 0 : 1].colMain;
                    finalHexScore.enabled = true;
                    finalHexFrameMesh.material = gm.playerDatas[valueByte > 0 ? 0 : 1].matMain;
                    finalHexFrameMesh.enabled = true;
                    finalHexagonTransform.localEulerAngles = new Vector3(60f, 90f, -90f);
                }
                if (valueByte > 0) _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
                else _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
                break;
        }
        
    }

    [Rpc(SendTo.Everyone)]
    void HexDefinition_EveryoneRpc(byte tileStateByte, sbyte valueByte) => HexDefinition(tileStateByte, valueByte);


    [Rpc(SendTo.NotMe)]
    void HexAnimationFromPool_NotMeRpc(Vector3 spawnPosition, sbyte val)
    {
        RingsPooled hr = gm.poolManager.GetHexRing();
        hr.SpawnMe(spawnPosition, val);
    }


    #region NEIGHBOUR BEHAVIOUR
    /// <summary>
    /// This method is called if one of its neighbour is hit directly.
    /// </summary>
    /// <param name="hitValue">Hit value of hit neighbour.</param>
    /// <param name="callbackFinishedAnimation">Reports to 'ParentHex' that started it, that animation is done and 'CurrentValue' is updated.</param>
    void ActivateFromDirectHit(sbyte hitValue, System.Action callbackFinishedAnimation)
    {
        _hitValue = (sbyte)(hitValue + CurrentValue);
        _allAnimationsDone = callbackFinishedAnimation;
        CheckCurrentValue();
    }


    /// <summary>
    /// This method and 'AnimateHexagon' call each other until 'CurrentValue' is updated.
    /// This should be done in one frame (and one method) but is separated because of animations
    /// </summary>
    void CheckCurrentValue()
    {
        if (Tstate == TileState.Taken) return;

        if (_hitValue > CurrentValue) //blue
        {
            if (_hitValue != CurrentValue) AnimateHexagon(1);
        }
        else if (_hitValue < CurrentValue) //red
        {
            if (_hitValue != CurrentValue) AnimateHexagon(-1);
        }
        else
        {
            _allAnimationsDone?.Invoke();
            _allAnimationsDone = null;
            // gm.SetGridValuesNet_ServerRpc(_mySiblingsIndex, CurrentValue);
        }

    }

    void AnimateHexagon(sbyte currValueChange)
    {
        if (Tstate == TileState.Taken) return;

        RingsPooled hr = gm.poolManager.GetHexRing();
        hr.SpawnMe(center.position, CurrentValue, () =>
        {
            CurrentValue += currValueChange;
            CheckCurrentValue();
        });
        HexAnimationFromPool_NotMeRpc(center.position, CurrentValue);
    }

    #endregion
}


