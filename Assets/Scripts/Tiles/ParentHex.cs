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
    Collider2D[] _colliders;
    GameObject[] _displayValues;
    GameObject[] _spritesOrange, _spritesTeal;
    const int CONST_HexNum = 10;

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
            if (IsOwner)
            {
                gm.SetHexStateNet_ServerRpc(transform.GetSiblingIndex(), (byte)value);
                gm.SetHexValNet_ServerRpc(transform.GetSiblingIndex(), CurrentValue);
                SetStateAndValue_EveryoneRpc((byte)value, CurrentValue);
                gm.Scoring_ServerRpc();
            }
            switch (value)
            {
                case TileState.Free: 
                    break;
                case TileState.InActive: 
                    Utils.DeActivateGo(gameObject);
                    return;
                case TileState.Taken:
                    _allAnimationsDone?.Invoke();
                    break;
            }

        }
    }
    public TileState startTs;
    
    [ShowInInspector] 
    [ReadOnly] 
    List<ParentHex> _myNeighbours;

    public sbyte CurrentValue
    {
        get => _currentValue;
        private set
        {
           _currentValue = value;
            byte absoluteValue = (byte)Mathf.Abs(value);
            SetStateAndValue_EveryoneRpc((byte)Tstate, value);
            if (absoluteValue > 9)
            {
                _currentValue = _hitValue;
                Tstate = TileState.Taken;
            }

        }
    }
    sbyte _currentValue;


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
    void TestHit() => HexHit(hitValueTest, gm.playerTurnNet.Value);


    private void Awake()
    {
        gm = GameManager.Instance;
        _colliders = Utils.AllChildren<Collider2D>(parColliders);
        _displayValues = Utils.AllChildrenGameObjects(parDisplays);
        _spritesOrange = Utils.AllChildrenGameObjects(parSpritesOrange);
        _spritesTeal = Utils.AllChildrenGameObjects(parSpritesTeal);
        _finalHexMesh = finalHexagonTransform.GetComponent<MeshRenderer>();
    }

    void Start()
    {
        Tstate = startTs;
        CurrentValue = 0;
    }

    public void HexHit(sbyte ordinal, PlayerColor arrowColor)
    {
        if (Tstate == TileState.InActive || Tstate == TileState.Taken) return;

        gm.audioManager.PlayOnMyAudioSource(aSource, gm.audioManager.hexHit);
        int mod = arrowColor == PlayerColor.Blue ? 1 : -1;
        CurrentValue = _hitValue = (sbyte)(ordinal * mod);
        Tstate = TileState.Taken;

        _myNeighbours = gm.gridManager.AllNeighbours(pos);
        NeighboursCheckIn = _myNeighbours.Count;
        if (_myNeighbours.Count == 0)
        {
            gm.NextPlayer_ServerRpc(false, "HexHit");
            return;
        }
        foreach (ParentHex item in _myNeighbours)
        {
            item.ActivateFromDirectHit(CurrentValue, () => NeighboursCheckIn--);
        }
    }

    public void SetStateAndValue(byte tileStateByte, sbyte valueByte)
    {
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
                Utils.DeActivateGo(gameObject);
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
        _currentValue = valueByte;

    }

    [Rpc(SendTo.Everyone)]
    void SetStateAndValue_EveryoneRpc(byte tileStateByte, sbyte valueByte) => SetStateAndValue(tileStateByte, valueByte);


    [Rpc(SendTo.NotMe)]
    void HexAnimationFromPool_NotMeRpc(Vector3 spawnPosition, sbyte val)
    {
        RingsPooled hr = gm.poolManager.GetHexRing();
        hr.SpawnMeOnClient(spawnPosition, val);
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
            if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue >= 0, 1);
        }
        else if (_hitValue < CurrentValue) //red
        {
            if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue <= 0, -1);
        }
        else
        {
            _allAnimationsDone?.Invoke();
            _allAnimationsDone = null;
            gm.SetHexValNet_ServerRpc(transform.GetSiblingIndex(), CurrentValue);
        }

    }
    void AnimateHexagon(bool increase, sbyte currValueChange)
    {
        if (Tstate == TileState.Taken) return;

        RingsPooled hr = gm.poolManager.GetHexRing();
        hr.SpawnMe(center.position, CurrentValue, () =>
        {
            CurrentValue += currValueChange;
            CheckCurrentValue();
        });
        HexAnimationFromPool_NotMeRpc(center.position, CurrentValue);
        if (CurrentValue == 0) return;
        // if (increase) UpdateColors_EveryoneRpc((sbyte)(CurrentValue + currValueChange));
        // else UpdateColors_EveryoneRpc((sbyte)(CurrentValue - currValueChange));
        // if (increase) SetTargetValue_EveryoneRpc((byte)Tstate, (sbyte)(CurrentValue + currValueChange));
        // else SetTargetValue_EveryoneRpc((byte)Tstate, (sbyte)(CurrentValue - currValueChange));
    }

    #endregion
}
//
// public class ParentHex : TileParent
// {
//     GameManager gm;
//     [Title("Class specific")]
//     public Transform center;
//     [SerializeField] AudioSource aSource;
//     [SerializeField] Transform parColliders, parDisplays, parSpritesOrange, parSpritesTeal;
//     Collider2D[] _colliders;
//     GameObject[] _displayValues;
//     GameObject[] _spritesOrange, _spritesTeal;
//     const int CONST_HexNum = 10;
//
//     [Title("Final Hex")]
//     [SerializeField] Transform finalHexagonTransform;
//     MeshRenderer _finalHexMesh;
//     [SerializeField] TextMeshPro finalHexScore;
//     [SerializeField] MeshRenderer finalHexFrameMesh;
//
//     public TileState Tstate
//     {
//         get => _tState;
//         set
//         {
//             _tState = value;
//             gm.hexStateNet[transform.GetSiblingIndex()] = (byte)value;
//             switch (value)
//             {
//                 case TileState.Free: //can be hit
//                     break;
//                 case TileState.InActive: //disabled, not part of game
//                     Utils.DeActivateGo(gameObject);
//                     break;
//                 case TileState.Taken: //can't be hit anymore
//                     _allAnimationsDone?.Invoke();
//                     for (int i = 0; i < CONST_HexNum; i++)
//                     {
//                         _colliders[i].enabled = false;
//                     }
//                     UpdateTakenHex_EveryoneRpc(CurrentValue);
//                     gm.Scoring_ServerRpc();
//                     gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
//                     break;
//             }
//
//         }
//     }
//     public TileState startTs;
//     
//     [ShowInInspector] 
//     [ReadOnly] 
//     List<ParentHex> _myNeighbours;
//
//     /// <summary>
//     /// Most important variable for this monobehaviour.
//     /// Positive value means points for blue, negative is points for red.
//     /// </summary>
//     public sbyte CurrentValue
//     {
//         get => _currentValueNet.Value;
//         private set
//         {
//            _currentValueNet.Value = value;
//             sbyte positiveValue = (sbyte)Mathf.Abs(value);
//             for (int i = 0; i < CONST_HexNum; i++)
//             {
//                 _colliders[i].enabled = i >= positiveValue;
//             }
//
//             UpdateColors_EveryoneRpc(value);
//
//             if (positiveValue > 9)
//             {
//                 _currentValueNet.Value = _hitValue;
//                 Tstate = TileState.Taken;
//             }
//             else UpdateTexts_EveryoneRpc(positiveValue);
//
//         }
//     }
//     NetworkVariable<sbyte> _currentValueNet = new NetworkVariable<sbyte>();
//
//
//     sbyte PlayerModifier() => (sbyte)(gm.playerTurnNet.Value == PlayerColor.Blue ? 1 : -1);
//     const float CONST_Time = 0.2f;
//
//     /// <summary>
//     /// Turn is over after all this hex and all its neighbours have updated their 'CurrentValue'.
//     /// </summary>
//     int NeighboursCheckIn
//     {
//         get => _neighboursCheckIn;
//         set
//         {
//             _neighboursCheckIn = value;
//             gm.audioManager.PlaySFX(gm.audioManager.hexCounter, finalHexagonTransform);
//             if (value <= 0) //all neighbours have finished their animations
//             {
//                 if (_oneShotNeighbourCheckIn) return;
//                 _oneShotNeighbourCheckIn = true;
//                 gm.NextPlayer_ServerRpc(false, "NeighboursCheckIn");
//             }
//         }
//     }
//     [ShowInInspector]
//     [ReadOnly]
//     int _neighboursCheckIn; 
//     bool _oneShotNeighbourCheckIn; //safety bool, makes sure '_gm.NextPlayer()' is triggered only once, probably redundant
//     System.Action _allAnimationsDone; //event used to communicate between hex and its neighbours
//     sbyte _hitValue;
//
//     [HideInInspector] public int valueForBot; 
//     [Title("Testing")]
//     public sbyte hitValueTest = 8;
//     [Button]
//     void TestHit() => HexHit(hitValueTest, gm.playerTurnNet.Value);
//
//
//     private void Awake()
//     {
//         gm = GameManager.Instance;
//         _colliders = Utils.AllChildren<Collider2D>(parColliders);
//         _displayValues = Utils.AllChildrenGameObjects(parDisplays);
//         _spritesOrange = Utils.AllChildrenGameObjects(parSpritesOrange);
//         _spritesTeal = Utils.AllChildrenGameObjects(parSpritesTeal);
//         _finalHexMesh = finalHexagonTransform.GetComponent<MeshRenderer>();
//     }
//
//     void Start()
//     {
//         Tstate = startTs;
//         CurrentValue = 0;
//     }
//
//
//     public void HexHit(sbyte ordinal, PlayerColor arrowColor)
//     {
//         if (Tstate == TileState.InActive || Tstate == TileState.Taken) return;
//
//         gm.audioManager.PlayOnMyAudioSource(aSource, gm.audioManager.hexHit);
//         int mod = arrowColor == PlayerColor.Blue ? 1 : -1;
//         CurrentValue = _hitValue = (sbyte)(ordinal * mod);
//         Tstate = TileState.Taken;
//         Utils.ActivateOneArrayElement(_displayValues);
//
//         _myNeighbours = gm.gridManager.AllNeighbours(pos);
//         NeighboursCheckIn = _myNeighbours.Count;
//         if (_myNeighbours.Count == 0)
//         {
//             gm.NextPlayer_ServerRpc(false, "HexHit");
//             return;
//         }
//         foreach (ParentHex item in _myNeighbours)
//         {
//             item.ActivateFromDirectHit(CurrentValue, () => NeighboursCheckIn--);
//         }
//     }
//
//     public void ClientLateJoin(byte stateFromByte, sbyte val)
//     {
//         sbyte positiveValue = (sbyte)Mathf.Abs(val);
//         switch ((TileState)stateFromByte)
//         {
//             case TileState.Free:
//                 switch (val)
//                 {
//                     case > 0:
//                         spriteRenderer.enabled = false;
//                         Utils.ActivateOneArrayElement(_displayValues, positiveValue);
//                         _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                         Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
//                         break;
//                     case < 0:
//                         spriteRenderer.enabled = false;
//                         Utils.ActivateOneArrayElement(_displayValues, positiveValue);
//                         _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                         Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
//                         break;
//                 }
//                 break;
//             case TileState.InActive:
//                 Utils.DeActivateGo(gameObject);
//                 return;
//             case TileState.Taken:
//                 if (positiveValue != 0)
//                 {
//                     spriteRenderer.enabled = false;
//                     Utils.ActivateOneArrayElement(_spritesTeal);
//                     Utils.ActivateOneArrayElement(_spritesOrange);
//                     Utils.ActivateOneArrayElement(_displayValues);
//                     finalHexScore.text = positiveValue.ToString();
//                     finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
//                     finalHexScore.enabled = true;
//                     finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
//                     finalHexFrameMesh.enabled = true;
//                     finalHexagonTransform.localEulerAngles = new Vector3(60f, 90f, -90f);
//                 }
//                 if (val > 0) _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                 else _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                 break;
//         }
//
//         enabled = false;
//     }
//
//     #region RPC CALLS
//     [Rpc(SendTo.Server)]
//     void SetCurrentValue_ServerRpc(sbyte bat) =>  _currentValueNet.Value = bat;
//     [Rpc(SendTo.Everyone)]
//     void UpdateColors_EveryoneRpc(sbyte value)
//     {
//         sbyte positiveValue = (sbyte)Mathf.Abs(value);
//         spriteRenderer.enabled = false;
//         Utils.ActivateOneArrayElement(_spritesTeal);
//         Utils.ActivateOneArrayElement(_spritesOrange);
//         switch (value)
//         {
//             case > 0:
//                 _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
//                 Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
//                 break;
//             case < 0:
//                 _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
//                 Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
//                 break;
//             default:
//                 _finalHexMesh.material = gm.playerDatas[2].matsHex[1];
//                 spriteRenderer.enabled = true;
//                 break;
//         }
//     }
//     [Rpc(SendTo.Everyone)]
//     void UpdateTexts_EveryoneRpc(sbyte val, bool hideAll = false)
//     {
//         if(hideAll) Utils.ActivateOneArrayElement(_displayValues);
//         else Utils.ActivateOneArrayElement(_displayValues, val);
//     }
//
//     [Rpc(SendTo.Everyone)]
//     void HexAnimationFromPool_EveryoneRpc(Vector3 spawnPosition, sbyte val)
//     {
//         RingsPooled hr = gm.poolManager.GetHexRing();
//         hr.SpawnMeOnClient(spawnPosition, val);
//     }
//     [Rpc(SendTo.Everyone)]
//     void UpdateTakenHex_EveryoneRpc(sbyte val) 
//     {
//         spriteRenderer.enabled = false;
//
//         Utils.ActivateOneArrayElement(_spritesTeal);
//         Utils.ActivateOneArrayElement(_spritesOrange);
//         Utils.ActivateOneArrayElement(_displayValues);
//         finalHexScore.text = Mathf.Abs(val).ToString();
//         finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
//         finalHexScore.enabled = true;
//         finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
//         finalHexFrameMesh.enabled = true;
//
//         finalHexagonTransform.DOLocalRotate(new Vector3(60f, 90f, -90f), CONST_Time);
//     }
//     #endregion
//
//
//     #region NEIGHBOUR BEHAVIOUR 
//     /// <summary>
//     /// This method is called if one of its neighbour is hit directly.
//     /// </summary>
//     /// <param name="hitValue">Hit value of hit neighbour.</param>
//     /// <param name="callbackFinishedAnimation">Reports to 'ParentHex' that started it, that animation is done and 'CurrentValue' is updated.</param>
//     void ActivateFromDirectHit(sbyte hitValue, System.Action callbackFinishedAnimation)
//     {
//         _hitValue = (sbyte)(hitValue + CurrentValue);
//         _allAnimationsDone = callbackFinishedAnimation;
//         CheckCurrentValue();
//     }
//     
//
//     /// <summary>
//     /// This method and 'AnimateHexagon' call each other until 'CurrentValue' is updated.
//     /// This should be done in one frame (and one method) but is separated because of animations
//     /// </summary>
//     void CheckCurrentValue()
//     {
//         if (Tstate == TileState.Taken) return;
//
//         if (_hitValue > CurrentValue) //blue
//         {
//             if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue >= 0, 1);
//         }
//         else if (_hitValue < CurrentValue) //red
//         {
//             if (_hitValue != CurrentValue) AnimateHexagon(CurrentValue <= 0, -1);
//         }
//         else
//         {
//             _allAnimationsDone?.Invoke();
//             _allAnimationsDone = null;
//             gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
//         }
//
//     }
//     void AnimateHexagon(bool increase, sbyte currValueChange)
//     {
//         if (Tstate == TileState.Taken) return;
//
//         RingsPooled hr = gm.poolManager.GetHexRing();
//         hr.SpawnMe(center.position, CurrentValue, () =>
//         {
//             CurrentValue += currValueChange;
//             CheckCurrentValue();
//         });
//         HexAnimationFromPool_EveryoneRpc(center.position, CurrentValue);
//         if (CurrentValue == 0) return;
//         if (increase) UpdateColors_EveryoneRpc((sbyte)(CurrentValue + currValueChange));
//         else UpdateColors_EveryoneRpc((sbyte)(CurrentValue - currValueChange));
//     }
//
//     #endregion
// }
