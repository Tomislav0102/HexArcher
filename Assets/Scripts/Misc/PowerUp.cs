using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class PowerUp : MonoBehaviour, ITargetForArrow
{
    [SerializeField] MeshRenderer mesh;
    [SerializeField] Collider coll;
    [SerializeField] TextMeshPro display;
    string[] _messages = new string[]
    {
        "I am a power up. Try to shoot me.",
        "Well done!"
    };
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            mesh.enabled = value;
            coll.enabled = value;
            display.enabled = value;
        }
    }

    [field:SerializeField] public Transform MyMainTransform { get; set; }

    bool _isActive;

    float _startScale;

    void Awake()
    {
        _startScale = MyMainTransform.localScale.x;
        IsActive = true;
    }

    public void HitMe()
    {
        display.text = _messages[1];
        coll.enabled = false;
        MyMainTransform.DOPunchScale(_startScale * 1.3f * Vector3.one, 0.5f, 5, 0.2f)
            .OnComplete(() =>
            {
                display.text = _messages[0];
                coll.enabled = IsActive;
            });
    }
}
