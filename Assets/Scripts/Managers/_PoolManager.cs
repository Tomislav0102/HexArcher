using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class PoolManager 
{
    public Transform parHexRings;
    RingsPooled[] _hexRings;
    int _counterHexRings;

    public void Init()
    {
        _hexRings = Utils.AllChildren<RingsPooled>(parHexRings);
    }

    public RingsPooled GetHexRing() => GetGenericObject<RingsPooled>(_hexRings, ref _counterHexRings);

    T GetGenericObject<T>(T[] arr, ref int count, int miliSecondsDelayEnd = 0)
    {
        T obj = arr[count];
        count = (1 + count) % arr.Length;

        if (miliSecondsDelayEnd > 0) End(obj, miliSecondsDelayEnd);
        return obj;
    }
    async void End<T>(T tip, int miliSecondsDelay)
    {
        await Task.Delay(miliSecondsDelay);
        if (tip.GetType() == typeof(GameObject))
        {
            GameObject go = tip as GameObject;
            go.SetActive(false);
        }
        else if (tip.GetType() == typeof(Transform))
        {
            Transform tr = tip as Transform;
            tr.gameObject.SetActive(false);
        }
        else if (tip.GetType() == typeof(ParticleSystem))
        {
            ParticleSystem ps = tip as ParticleSystem;
            ps.Stop();
        }
        else if (tip.GetType() == typeof(LineRenderer))
        {
            LineRenderer lr = tip as LineRenderer;
            lr.enabled = false;
        }
    }
}