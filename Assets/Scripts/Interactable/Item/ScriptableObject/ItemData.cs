using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ItemType
{
    Equipable,
    Consumable,
    Resource
}

public enum ConsumableType
{
    Health,
    Hunger,
    Stamina
}

[Serializable]
public class ItemDataConsumable
{
    public ConsumableType type;
    public float value;
}

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class ItemData : ScriptableObject
{
    [Header("info")]
    public string displayName;                  // displayName : 아이템 이름
    public string description;                  // description : 아이템 설명
    public ItemType type;                       // type : 아이템 타입
    public Sprite Icon;                         // Icon : 아이템 아이콘
    public GameObject dropPrefeb;               // dropPrefeb : 

    [Header("Stacking")]
    public bool canStack;                       // canStack : 여러 개 한 번에 가질 수 있는지
    public int maxStackAmount;                  // maxStackAmount : 한 번에 가질 수 있는 최대 양

    [Header("Consumable")]
    public ItemDataConsumable consumables;      // consumables : 어떤 속성을 얼마나 회복하는지
}
