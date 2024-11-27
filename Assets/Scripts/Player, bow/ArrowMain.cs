using Unity.Netcode;
using UnityEngine;

public class ArrowMain : MonoBehaviour
{
    protected GameManager gm;
    
    [SerializeField] TrailRenderer trail;
    public Transform myTransform;
    public ArrowState myArrowState;

    protected void Awake()
    {
        gm = GameManager.Instance;
    }

    
    public virtual void Release(Vector3 vel)
    {
        myArrowState = ArrowState.Flying;
    //    print("arrowMain released");
        SetTrail();
    }

    public void SetTrail()
    {
        if(trail.enabled) return;
        trail.enabled = true;
        trail.colorGradient = gm.playerDatas[(int)gm.playerTurnNet.Value].colGradientTrail;

    }


}
