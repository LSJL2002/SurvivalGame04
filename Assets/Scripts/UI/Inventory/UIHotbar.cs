using UnityEngine;

public class UIHotbar : MonoBehaviour
{
    public Transform hotbarSlotPanel;
    [SerializeField] private UIInventory inventoryUI;
    private ItemSlot[] hotbarSlots;

    private int selectedIndex = 0;

    void Start()
    {
        hotbarSlots = new ItemSlot[hotbarSlotPanel.childCount];
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i] = hotbarSlotPanel.GetChild(i).GetComponent<ItemSlot>();
            hotbarSlots[i].Clear();

            // Turn off outline initially
            if (hotbarSlots[i].outline != null)
                hotbarSlots[i].outline.enabled = false;
        }
    }

    void Update()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            UseSelectedItem();
        }
    }

    public void UpdateHotbarUI(ItemSlot[] inventoryHotbar)
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (i < inventoryHotbar.Length && inventoryHotbar[i].item != null)
            {
                hotbarSlots[i].item = inventoryHotbar[i].item;
                hotbarSlots[i].quantity = inventoryHotbar[i].quantity;
                hotbarSlots[i].Set();
            }
            else
            {
                hotbarSlots[i].Clear();
            }
        }
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        selectedIndex = index;

        // Highlight only the selected slot, regardless of whether it has an item
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].outline.enabled = (i == selectedIndex);
    }

    private void UseSelectedItem()
    {
        if (hotbarSlots[selectedIndex].item == null) return;

        // Let UIInventory handle usage and data sync
        inventoryUI.UseItem(selectedIndex, true); // true = hotbar slot
    }
}
