using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowMain : MonoBehaviour
{
    protected GameManager gm;
    protected bool oneHitNextPlayer;
    
    public TrailRenderer trail;
    public Transform myTransform;
    [SerializeField] protected MeshRenderer myMesh;


    public ArrowState MyArrowState;

    protected void Awake()
    {
        gm = GameManager.Instance;
    }

    protected virtual void DestroyMe(string message = null)
    {
        print(string.IsNullOrEmpty(message) ? "" : message);
        Destroy(gameObject);
    }
}
