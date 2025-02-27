using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuElementOpenWindows : MenuElementMain, IPointerClickHandler
{
    [SerializeField] GameObject windowToOpen;
    [SerializeField] bool requireInternet, requirePlayerLevel;

    public void OnPointerClick(PointerEventData eventData)
    {
        audioManager.PlaySFX(audioManager.uiButton);
        Utils.ActivateOneArrayElement(mm.mainUiElements);
        
        if (requirePlayerLevel)
        {
            PlayerLeveling.CalculateLevelFromXp(out int lv, out int xp);
            if (lv < 5)
            {
                Utils.Activation(mm.requiredLevelWindow, true);
                return;
            }
        }
        
        if (requireInternet && !mm.hasInternet)
        {
            Utils.Activation(mm.noInternetWindow, true);
            return;
        }
        Utils.Activation(windowToOpen, true);
    }

}
