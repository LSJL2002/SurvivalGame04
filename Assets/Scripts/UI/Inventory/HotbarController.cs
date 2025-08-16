using UnityEngine;

public class HotbarController : MonoBehaviour
{
    public UIInventory uiInventory;
    public HotbarData hotbarData;

    void Update()
    {
        // Example: Number keys 1–7 to select slots
        for (int i = 0; i < 7; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                hotbarData.activeIndex = i;
                uiInventory.HighlightHotbarSlot(i);
            }
        }

        // Example: Left click to use selected item
        if (Input.GetMouseButtonDown(0))
        {
            UseHotbarItem(hotbarData.activeIndex);
        }
    }

    void UseHotbarItem(int index)
    {
        if (index < 0 || index >= hotbarData.slots.Count) return;

        ItemSlotData slot = hotbarData.slots[index];
        if (slot.IsEmpty) return;

        Debug.Log($"Using {slot.item.name}");

        // Here you'd trigger your actual item logic (equip, consume, etc.)
    }
}
