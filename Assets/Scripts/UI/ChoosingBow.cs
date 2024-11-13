using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ChoosingBow : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI displayName;
    [SerializeField] Transform parBows;
    GameObject[] _bows;
    string[] _names;
    int _counter;

    private void Awake()
    {
        _bows = new GameObject[parBows.childCount];
        _names = new string[parBows.childCount];
        for (int i = 0; i < parBows.childCount; i++)
        {
            BowControl bc = parBows.GetChild(i).GetComponent<BowControl>();
            _bows[bc.bowData.ordinal] = bc.gameObject;
            _names[bc.bowData.ordinal] = bc.bowData.bowName;
        }
    }
    private void Start()
    {
        _counter = PlayerPrefs.GetInt(Utils.Bow_Int);
        Utils.ActivateOneArrayElement(_bows, _counter);
        displayName.text = _names[_counter];
    }

    private void OnEnable()
    {
        button.onClick.AddListener(BtnMain);
    }
    private void OnDisable()
    {
        button.onClick.RemoveAllListeners();
    }

    void BtnMain()
    {
        _counter = (1 + _counter) % _bows.Length;
        Utils.ActivateOneArrayElement(_bows, _counter);
        displayName.text = _names[_counter];
        PlayerPrefs.SetInt(Utils.Bow_Int, _counter);
    }
}
