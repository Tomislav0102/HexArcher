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
        Utils.ActivateOneArrayElement(mm.mainUIelements);
        
        if (requirePlayerLevel)
        {
            PlayerLeveling.CalculateLevelFromXp(out int lv, out int xp);
            if (lv < 5)
            {
                Utils.ActivateGo(mm.requiredLevelWindow);
                return;
            }
        }
        
        if (requireInternet && !mm.hasInternet)
        {
            Utils.ActivateGo(mm.noInternetWindow);
            return;
        }
        Utils.ActivateGo(windowToOpen);
    }

}
