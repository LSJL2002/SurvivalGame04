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
    public string displayName;                  // displayName : ������ �̸�
    public string description;                  // description : ������ ����
    public ItemType type;                       // type : ������ Ÿ��
    public Sprite Icon;                         // Icon : ������ ������
    public GameObject dropPrefeb;               // dropPrefeb : 

    [Header("Stacking")]
    public bool canStack;                       // canStack : ���� �� �� ���� ���� �� �ִ���
    public int maxStackAmount;                  // maxStackAmount : �� ���� ���� �� �ִ� �ִ� ��

    [Header("Consumable")]
    public ItemDataConsumable consumables;      // consumables : � �Ӽ��� �󸶳� ȸ���ϴ���
}
