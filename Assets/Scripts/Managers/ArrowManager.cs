using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ArrowManager : NetworkBehaviour
{
    public GameObject prefabArrowReal;
    public GameObject prefabArrowShadow;
    [Sirenix.OdinInspector.ReadOnly] public ArrowMain arrowReal;
    [Sirenix.OdinInspector.ReadOnly] public ArrowMain arrowShadow;
    [Sirenix.OdinInspector.ReadOnly] public float forceArrow;

    [Rpc(SendTo.Everyone)]
    public void ClearAllArrows_EveryoneRpc()
    {
        if (arrowReal != null)  Utils.DestroyGo(arrowReal.gameObject);
        if (arrowShadow != null)  Utils.DestroyGo(arrowShadow.gameObject);
    }

    public void SpawnRealArrow(Vector3 pos, Quaternion rot)
    {
        arrowReal = Instantiate(prefabArrowReal, pos, rot).GetComponent<ArrowMain>();
        SpawnShadowArrow_NotMeRpc(pos, rot);
    }

    [Rpc(SendTo.NotMe)]
    void SpawnShadowArrow_NotMeRpc(Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(prefabArrowShadow, pos, rot);
        arrowShadow = go.GetComponent<ArrowMain>();
    }

    [Rpc(SendTo.NotMe)]
    public void ShadowArrow_NotMeRpc(Vector3 pos, Quaternion rot)
    {
        if (arrowShadow == null) return;
        arrowShadow.myTransform.SetPositionAndRotation(pos, rot);
        arrowShadow.SetTrail();
    }

}
