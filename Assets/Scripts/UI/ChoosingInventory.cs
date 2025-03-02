using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;


public class ChoosingInventory : MonoBehaviour
{
    enum InventoryState { Bows, Heads, Hands };
    InventoryState CurrentSate
    {
        get => _currentState;
        set
        {
            _currentState = value;
            Utils.Activation(parBows.gameObject, false);
            Utils.Activation(parHeads.gameObject, false);
            Utils.Activation(parHands.gameObject, false);
            switch (value)
            {
                case InventoryState.Bows:
                    Utils.Activation(parBows.gameObject, true);
                    break;
                case InventoryState.Heads:
                    Utils.Activation(parHeads.gameObject, true);
                    break;
                case InventoryState.Hands:
                    Utils.Activation(parHands.gameObject, true);
                    break;
            }
            Utils.ActivateOneArrayElement(_itemGroups[(int)value].gos, PlayerPrefs.GetInt(_itemGroups[(int)value].savedData));
            for (int i = 0; i < _outlines.Length; i++)
            {
                _outlines[i].enabled = false;
            }
            _outlines[(int)value].enabled = true;
            displayItemName.text = _itemGroups[(int)value].gos[_itemGroups[(int)value].counter].GetComponent<ItemSoCarrierUi>().myItem.itemName;
        }
    }
    InventoryState _currentState;
    ItemGroup[] _itemGroups;
    
    [SerializeField] Button[] buttonsGroups;
    Outline[] _outlines;
    [SerializeField] Button buttonNextItem;
    [SerializeField] TextMeshProUGUI displayItemName;
    [SerializeField] Transform parBows, parHeads, parHands;


    class ItemGroup
    {
        public InventoryState state;
        public GameObject[] gos;
        public string savedData;
        public int counter;

        public ItemGroup(InventoryState state, GameObject[] gos, string savedData)
        {
            this.state = state;
            this.gos = gos;
            this.savedData = savedData;
        }
    }

    void Start()
    {
        _itemGroups = new ItemGroup[3]
        {
            new ItemGroup(InventoryState.Bows, Utils.AllChildrenGameObjects(parBows), Utils.Bow_Int),
            new ItemGroup(InventoryState.Heads, Utils.AllChildrenGameObjects(parHeads), Utils.Head_Int),
            new ItemGroup(InventoryState.Hands, Utils.AllChildrenGameObjects(parHands), Utils.Hand_Int),
        };
        _outlines = new Outline[buttonsGroups.Length];
        for (int i = 0; i < _outlines.Length; i++)
        {
            _outlines[i] = buttonsGroups[i].GetComponent<Outline>();
            _outlines[i].enabled = false;
        }
        CurrentSate = InventoryState.Bows;
    }

    void Update()
    {
        parBows.Rotate(Time.deltaTime * 10f * Vector3.up);
        parHeads.Rotate(Time.deltaTime * 10f * Vector3.up);
        parHands.Rotate(Time.deltaTime * 10f * Vector3.up);
    }

    private void OnEnable()
    {
        for (int i = 0; i < buttonsGroups.Length; i++)
        {
            int index = i;
            buttonsGroups[i].onClick.AddListener(() => { CurrentSate = (InventoryState)index; });
        }
        buttonNextItem.onClick.AddListener(ButtonMethodNext);
    }
    private void OnDisable()
    {
        for (int i = 0; i < buttonsGroups.Length; i++)
        {
            buttonsGroups[i].onClick.RemoveAllListeners();
        }
        buttonNextItem.onClick.RemoveAllListeners();
    }
    
    
    void ButtonMethodNext()
    {
        ItemGroup itemGroup = _itemGroups[(int)CurrentSate];
        itemGroup.counter = (1 + itemGroup.counter) % itemGroup.gos.Length;
        Utils.ActivateOneArrayElement(itemGroup.gos, itemGroup.counter);
        displayItemName.text = itemGroup.gos[itemGroup.counter].GetComponent<ItemSoCarrierUi>().myItem.itemName;
        PlayerPrefs.SetInt(itemGroup.savedData, itemGroup.counter);
    }
}
