using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryData
{
    public List<ItemSlotData> slots = new List<ItemSlotData>();
}

[Serializable]
public class HotbarData
{
    public List<ItemSlotData> slots = new List<ItemSlotData>();
    public int activeIndex = 0; // currently selected slot
}

[Serializable]
public class ItemSlotData
{
    public ItemData item;
    public int quantity;
    public bool equipped;

    public bool IsEmpty => item == null;
}
