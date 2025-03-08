using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    GameManager gm;
    public Transform myTransform;
    public PlayerControl playerControl;
    [SerializeField] GenSide side;
    bool _inBow, _inString;

    public bool Selected
    {
        set
        {
            if (gm == null ||
                gm.playerVictoriousNet.Value != PlayerColor.Undefined) return;

            if (value)
            {
                if (playerControl.handsAnim[(int)side].GetFloat("Fist") == 0) playerControl.handsAnim[(int)side].SetFloat("Fist", 1);
                else playerControl.handsAnim[(int)side].SetFloat("Fist", 0);

                if (playerControl.SideThatHoldsBow() == side)
                {
                    playerControl.bowCurrent.Bstate = BowState.Free;
                }
                else if (_inBow && playerControl.bowCurrent.Bstate != BowState.RackMoving)
                {
                    playerControl.bowCurrent.interactor = this;
                    playerControl.bowCurrent.Bstate = BowState.InHand;
                    playerControl.handsAnim[((int)side + 1) % 2].SetFloat("Fist", 0);
                }
                _inBow = false;
            }
            else
            {
                if (playerControl.SideThatHoldsBow() == side) return;
                playerControl.handsAnim[(int)side].SetFloat("Fist", 0);
            }
        }
    }
    public bool Activated
    {
        get => _activated;
        set
        {
            if (gm == null ||
                playerControl.SideThatHoldsBow() == side ||
                !gm.MyTurn() ||
                gm.playerVictoriousNet.Value != PlayerColor.Undefined) return;

            _activated = value;

            playerControl.handsAnim[(int)side].SetFloat("Fist", value ? 1 : 0);

            if (value)
            {
                if (!_inString) return;
                playerControl.shootingCurrent.controllerPullingString = true;
                VrRigRef.instance.RemoveLines();
            }
            else
            {
                playerControl.shootingCurrent.controllerPullingString = false;
            }
        }
    }
    bool _activated;

    private void Start()
    {
        gm = GameManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Activated) return;

        if (other.CompareTag("Bow"))
        {
            _inBow = true;
        }
        if (other.CompareTag("String"))
        {
            _inString = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Bow"))
        {
            _inBow = false;
        }
        if (other.CompareTag("String"))
        {
            _inString = false;
        }

    }
}
