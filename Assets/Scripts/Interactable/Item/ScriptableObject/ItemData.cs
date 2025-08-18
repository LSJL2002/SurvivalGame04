using UnityEngine;

public enum ItemType
{
    Resource,
    Equipable,
    Consumable
}

public enum ConsumableType
{
    Stamina,
    Health,
    Boost
}

public enum BoostType
{
    None,
    Speed,
    Jump,
    Stamina
}

[System.Serializable]
public class ItemDataConsumable
{
    public ConsumableType type;
    public BoostType boostType; 
    public float value;
}

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string displayName;
    public string description;
    public ItemType type;
    public Sprite icon;
    public GameObject dropPrefab;
    public int attackDamage;

    [Header("Stacking")]
    public bool canStack;
    public int maxStackAmount;

    [Header("Consumable")]
    public ItemDataConsumable[] consumables;

    [Header("Equip")]
    public GameObject equipPrefab;
    [Header("Boost Duration")]
    public int boostDuration;
}