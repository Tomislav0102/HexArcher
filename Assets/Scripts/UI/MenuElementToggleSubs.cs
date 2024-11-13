using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuElementToggleSubs : MenuElementMain, IPointerClickHandler
{
    [SerializeField] GameObject[] subIcons;

    void Start()
    {
        for (int i = 0; i < subIcons.Length; i++)
        {
            subIcons[i].SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        audioManager.PlaySFX(audioManager.uiButton);
        for (int i = 0; i < subIcons.Length; i++)
        {
            subIcons[i].SetActive(!subIcons[i].activeInHierarchy);
        }
    }
}
