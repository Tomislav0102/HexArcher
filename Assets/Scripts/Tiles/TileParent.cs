using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Sirenix.OdinInspector;

public class TileParent : NetworkBehaviour //because of RPC calls (only works on NetworkBehaviour)
{
    public Vector2Int pos;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [ReadOnly] public TileState _tState;
    protected Color colorActive = Color.white;
    protected Color colorInactive = new Color(0.45f, 0.45f, 0.45f, 1f);

}
