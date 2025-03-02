using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VrRigRef : MonoBehaviour
{
    public static VrRigRef instance;

    public Transform root, head, leftHand, rightHand;
    public Camera mainCam;
    public ActionBasedController leftController;
    public ActionBasedController rightController;
    public SpriteRenderer fadeSprite;
    public LineRenderer leftHandRayLineRend, rightHandRayLineRend;
    [SerializeField] Material matLineRenderer, matInvisible;
    [SerializeField] AudioManager audioManager;
    public PlayerControl playerControl;

    private void Awake()
    {
        instance = this;
    }
    public void RemoveLines()
    {
        leftHandRayLineRend.material = rightHandRayLineRend.material = matInvisible;
    }
    public void UIhoverLeftCallFromVrRig(bool hover)
    {
        if (playerControl == null || playerControl.shootingCurrent.controllerPullingString) return;

        leftHandRayLineRend.material = hover ? matLineRenderer : matInvisible;
        if (audioManager == null || !hover) return;
        audioManager.PlaySFX(audioManager.hoverEnter);
    }
    public void UIhoverRightCallFromVrRig(bool hover)
    {
        if (playerControl == null || playerControl.shootingCurrent.controllerPullingString) return;

        rightHandRayLineRend.material = hover ? matLineRenderer : matInvisible;
        if (audioManager == null || !hover) return;
        audioManager.PlaySFX(audioManager.hoverEnter);

    }


}
