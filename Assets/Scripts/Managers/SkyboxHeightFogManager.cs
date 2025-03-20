using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;


public class SkyboxHeightFogManager : MonoBehaviour
{
    enum SkyBoxMode {NoFog, Fog}

    [SerializeField] SkyBoxMode mode = SkyBoxMode.NoFog;
    Material _fogMat;
    [SerializeField] GameObject parFog;

    public Material[] Mats()
    {
        mode = (SkyBoxMode)PlayerPrefs.GetInt(Utils.HeightFog_Int);
        switch (mode)
        {
            case SkyBoxMode.NoFog:
                if (_materialsSky == null || _materialsSky.Length == 0) _materialsSky = Resources.LoadAll<Material>("Skybox materials");
                break;
            case SkyBoxMode.Fog:
                if (_materialsSky == null || _materialsSky.Length == 0) _materialsSky = Resources.LoadAll<Material>("SkyboxFog materials");
                break;
        }
        
        
        return _materialsSky;
    }
    Material[] _materialsSky;
    Vector2 _moveDir = Vector2.zero;

    public void InitSkybox(int index)
    {
        Utils.Activation(parFog, false);
        float offset = 0.02f;
        _moveDir = new Vector2(Random.Range(-offset, offset), Random.Range(-offset, offset));
        Material chosenSkybox = null;
        chosenSkybox = Mats()[index];
        RenderSettings.skybox = chosenSkybox;
        switch (mode)
        {
            case SkyBoxMode.NoFog:
                RenderSettings.customReflectionTexture = chosenSkybox.GetTexture("_Tex");
                break;
            case SkyBoxMode.Fog:
                _fogMat = parFog.transform.GetChild(0).GetComponent<Renderer>().material;
                _fogMat.color = chosenSkybox.GetColor("_HorizonColor");
                Utils.Activation(parFog, true);
                break;
        }
    }

    void Update()
    {
        if (mode == SkyBoxMode.NoFog) return;
        _fogMat.mainTextureOffset = Time.time * _moveDir;
    }
}

