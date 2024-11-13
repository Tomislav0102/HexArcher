//using Sirenix.OdinInspector

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuElementMain : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    protected MainMenuManager mm;
    protected AudioManager audioManager;
    GameObject _emissionGameObject;

    void Awake()
    {
        _emissionGameObject = transform.GetChild(1).gameObject;
        mm = MainMenuManager.Instance;
        audioManager = mm.audioManager;
    }


    void OnEnable()
    {
        Utils.MainUiUnselect += CallEv_Unselect;
    }
    void OnDisable()
    {
        Utils.MainUiUnselect -= CallEv_Unselect;
    }
    void CallEv_Unselect()
    {
        _emissionGameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Utils.MainUiUnselect.Invoke();
        _emissionGameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CallEv_Unselect();
    }

}
