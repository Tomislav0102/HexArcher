using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Used in editor only.
/// Holds data for generation of levels in game.
/// Notice '[ExecuteInEditMode]' attribute - you've been warned
/// </summary>
[ExecuteInEditMode]
[System.Serializable]
public class Marker : TileParent
{
    public TileState Tstate
    {
        get => _tState;
        set
        {
            _tState = value;
            numTS = (int)value;
            switch (value)
            {
                case TileState.Free:
                    spriteRenderer.color = colorActive;
                    break;
                case TileState.InActive:
                    spriteRenderer.color = colorInactive;
                    break;
                case TileState.Taken:
                    break;
            }
        }
    }
    [HideInInspector] public int numTS; //decribes 'TileState' enum beacuse of problems with serialization
    [HideInInspector] public bool done = false; //called after it becomes prefab (once created, levels can't be edited)

#if (UNITY_EDITOR)
    private void Update()
    {
        if(done) return;
        if (Selection.activeGameObject == gameObject)
        {
            if (Tstate == TileState.Free) Tstate = TileState.InActive;
            else Tstate = TileState.Free;
            Selection.activeGameObject = null;
        }
    }
#endif
}
