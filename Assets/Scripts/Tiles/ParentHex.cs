using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
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
    const int CONST_HEXNUM = 10;

    [Title("Final Hex")]
    [SerializeField] Transform finalHexagonTransform;
    MeshRenderer _finalHexMesh;
    [SerializeField] TextMeshPro finalHexScore;
    [SerializeField] MeshRenderer finalHexFrameMesh;

    public TileState Tstate
    {
        get => _tState;
        set
        {
            _tState = value;
            gm.hexStateNet[transform.GetSiblingIndex()] = (byte)value;
            switch (value)
            {
                case TileState.Free: //can be hit
                    break;
                case TileState.InActive: //disabled, not part of game
                    Utils.DeActivateGo(gameObject);
                    break;
                case TileState.Taken: //can't be hit anymore
                    _allAinimationsDone?.Invoke();
                    for (int i = 0; i < CONST_HEXNUM; i++)
                    {
                        _colliders[i].enabled = false;
                    }
                    UpdateTakenHexRpc(CurrentValue);
                    gm.Scoring_ServerRpc();
                    gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
                    break;
            }

        }
    }
    public TileState startTs;
    
    [ShowInInspector] 
    [ReadOnly] 
    List<ParentHex> _myNeighbours;

    /// <summary>
    /// Most important variable for this monobehaviour.
    /// Positive value means points for blue, negative is points for red.
    /// </summary>
    public sbyte CurrentValue
    {
        get => _currentValueNet.Value;
        private set
        {
            _currentValueNet.Value = value;
            sbyte positiveValue = (sbyte)Mathf.Abs(value);
            for (int i = 0; i < CONST_HEXNUM; i++)
            {
                _colliders[i].enabled = i >= positiveValue;
            }

            UpdateColorsRpc(value);

            if (positiveValue > 9)
            {
                _currentValueNet.Value = _hitValue;
                Tstate = TileState.Taken;
            }
            else UpdateTextsRpc(positiveValue);

        }
    }
    NetworkVariable<sbyte> _currentValueNet = new NetworkVariable<sbyte>();


    sbyte PlayerModifier() => (sbyte)(gm.playerTurnNet.Value == PlayerColor.Blue ? 1 : -1);
    const float CONST_TIME = 0.2f;

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
    int _neighboursCheckIn; //not part of getter beacuse of server issues
    bool _oneShotNeighbourCheckIn; //safety bool, makes sure '_gm.NextPlayer()' is triggered only once, probablly redundant
    System.Action _allAinimationsDone; //event used to communicate between hex and its neighbours
    sbyte _hitValue;

    [HideInInspector] public int valueForBot; 
    [Title("Testing")]
    public sbyte hitValue = 5;
    [Button]
   [ContextMenu("Score hit")]
    void TestHit() => HexHit(hitValue);


    private void Awake()
    {
        gm = GameManager.Instance;
        _colliders = Utils.AllChildren<Collider2D>(parColliders);
        _displayValues = Utils.AllChildrenGameObjects(parDisplays);
        _spritesOrange = Utils.AllChildrenGameObjects(parSpritesOrange);
        _spritesTeal = Utils.AllChildrenGameObjects(parSpritesTeal);
        _finalHexMesh = finalHexagonTransform.GetComponent<MeshRenderer>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer)
        {
            Tstate = startTs;
            CurrentValue = 0;
        }
        else
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].gameObject.SetActive(false);
            }
        }
    }

    public void HexHit(sbyte ordinal)
    {
        if (Tstate == TileState.InActive || Tstate == TileState.Taken) return;

        gm.audioManager.PlayOnMyAudioSource(aSource, gm.audioManager.hexHit);
        CurrentValue = _hitValue = (sbyte)(ordinal * PlayerModifier());
        gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
        Tstate = TileState.Taken;
        Utils.ActivateOneArrayElement(_displayValues);

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

    public void ClientLateJoin(byte stateFromByte, sbyte val)
    {
        sbyte positiveValue = (sbyte)Mathf.Abs(val);
        switch ((TileState)stateFromByte)
        {
            case TileState.Free:
                if (val > 0)
                {
                    spriteRenderer.enabled = false;
                    Utils.ActivateOneArrayElement(_displayValues, positiveValue);
                    _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
                    Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
                }
                else if (val < 0)
                {
                    spriteRenderer.enabled = false;
                    Utils.ActivateOneArrayElement(_displayValues, positiveValue);
                    _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
                    Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
                }
                break;
            case TileState.InActive:
                Utils.DeActivateGo(gameObject);
                return;
            case TileState.Taken:
                if (positiveValue != 0)
                {
                    spriteRenderer.enabled = false;
                    Utils.ActivateOneArrayElement(_spritesTeal);
                    Utils.ActivateOneArrayElement(_spritesOrange);
                    Utils.ActivateOneArrayElement(_displayValues);
                    finalHexScore.text = positiveValue.ToString();
                    finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
                    finalHexScore.enabled = true;
                    finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
                    finalHexFrameMesh.enabled = true;
                    finalHexagonTransform.localEulerAngles = new Vector3(60f, 90f, -90f);
                }
                if (val > 0) _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
                else _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
                break;
        }

        enabled = false;
    }

    #region RPC CALLS
    [Rpc(SendTo.Everyone)]
    private void UpdateColorsRpc(sbyte value)
    {
        sbyte positiveValue = (sbyte)Mathf.Abs(value);
        spriteRenderer.enabled = false;
        Utils.ActivateOneArrayElement(_spritesTeal);
        Utils.ActivateOneArrayElement(_spritesOrange);
        if (value > 0)
        {
            _finalHexMesh.material = gm.playerDatas[0].matsHex[1];
            Utils.ActivateOneArrayElement(_spritesTeal, positiveValue);
        }
        else if (value < 0)
        {
            _finalHexMesh.material = gm.playerDatas[1].matsHex[1];
            Utils.ActivateOneArrayElement(_spritesOrange, positiveValue);
        }
        else
        {
            _finalHexMesh.material = gm.playerDatas[2].matsHex[1];
            spriteRenderer.enabled = true;
        }
    }
    [Rpc(SendTo.Everyone)]
    void UpdateTextsRpc(sbyte val, bool hideAll = false)
    {
        if(hideAll) Utils.ActivateOneArrayElement(_displayValues);
        else Utils.ActivateOneArrayElement(_displayValues, val);
    }
    [Rpc(SendTo.Everyone)]
    void UpdateFinalHexRpc()
    {
        finalHexScore.enabled = false;
        finalHexFrameMesh.material = gm.playerDatas[2].matMain;
        finalHexFrameMesh.enabled = false;
        finalHexagonTransform.localEulerAngles = new Vector3(60f, -90f, -90f);
    }

    [Rpc(SendTo.Everyone)]
    void HexAnimationFromPoolRpc(Vector3 spawnPosition, sbyte val)
    {
        RingsPooled hr = gm.poolManager.GetHexRing();
        hr.SpawnMeOnClient(spawnPosition, val);
    }
    [Rpc(SendTo.Everyone)]
    void UpdateTakenHexRpc(sbyte val) 
    {
        spriteRenderer.enabled = false;

        Utils.ActivateOneArrayElement(_spritesTeal);
        Utils.ActivateOneArrayElement(_spritesOrange);
        Utils.ActivateOneArrayElement(_displayValues);
        finalHexScore.text = Mathf.Abs(val).ToString();
        finalHexScore.color = gm.playerDatas[val > 0 ? 0 : 1].colMain;
        finalHexScore.enabled = true;
        finalHexFrameMesh.material = gm.playerDatas[val > 0 ? 0 : 1].matMain;
        finalHexFrameMesh.enabled = true;

        finalHexagonTransform.DOLocalRotate(new Vector3(60f, 90f, -90f), CONST_TIME);
    }
    #endregion


    #region NEIGHBOUR BEHAVIOUR 
    /// <summary>
    /// This method is called if one of its neighbour is hit directly.
    /// </summary>
    /// <param name="hitValue">Hit value of hit neioghbour.</param>
    /// <param name="callbackFinishedAnimation">Reports to 'ParentHex' that started it, that animation is done and 'CurrentValue' is updated.</param>
    void ActivateFromDirectHit(sbyte hitValue, System.Action callbackFinishedAnimation)
    {
        _hitValue = (sbyte)(hitValue + CurrentValue);
        _allAinimationsDone = callbackFinishedAnimation;
        CheckCurrentValue();
    }
    

    /// <summary>
    /// This method and 'AnimateHexagon' call each other until 'CurrentValue' is updated.
    /// This should be done in one frame (and one method) but is separated beacuse of animations
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
            _allAinimationsDone?.Invoke();
            _allAinimationsDone = null;
            gm.hexValNet[transform.GetSiblingIndex()] = CurrentValue;
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
        HexAnimationFromPoolRpc(center.position, CurrentValue);
        if (CurrentValue == 0) return;
        if (increase) UpdateColorsRpc((sbyte)(CurrentValue + currValueChange));
        else UpdateColorsRpc((sbyte)(CurrentValue - currValueChange));
    }

    #endregion
}
