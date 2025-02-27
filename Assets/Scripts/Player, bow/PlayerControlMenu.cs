using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlMenu : MonoBehaviour
{
    [SerializeField] Material matLineRenderer, matInvisible;
    [SerializeField] LineRenderer leftHandRayLineRend, rightHandRayLineRend;
    [SerializeField] AudioManager audioManager;
    [SerializeField] SpriteRenderer fadeSprite;

    private void OnEnable()
    {
        Utils.FadeOut += CallEv_FadeMethod;
    }
    private void OnDisable()
    {
        Utils.FadeOut -= CallEv_FadeMethod;
    }
    void CallEv_FadeMethod(bool fadeout)
    {
        Utils.Activation(fadeSprite.gameObject, true);
        Color from = fadeout ? Color.black : Color.clear;
        Color to = fadeout ? Color.clear : Color.black;
        fadeSprite.DOColor(to, 2f)
            .From(from)
            .OnComplete(() =>
            {
                if (fadeout)
                {
                    Utils.Activation(fadeSprite.gameObject, false);
                }
            });
    }

    #region UNITY EVENT SYSTEM IN INSPECTOR
    public void UIhoverLeft(bool hover)
    {
        leftHandRayLineRend.material = hover ? matLineRenderer : matInvisible;
        if (audioManager == null || !hover) return;
        audioManager.PlaySFX(audioManager.hoverEnter);
    }
    public void UIhoverRight(bool hover)
    {
        rightHandRayLineRend.material = hover ? matLineRenderer : matInvisible;
        if (audioManager == null || !hover) return;
        audioManager.PlaySFX(audioManager.hoverEnter);

    }
    #endregion
}
