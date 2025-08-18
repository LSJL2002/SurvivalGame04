using UnityEngine;

public class UIHotbar : MonoBehaviour
{
    public Transform hotbarSlotPanel;
    [SerializeField] private UIInventory inventoryUI;
    private ItemSlot[] hotbarSlots;

    private int selectedIndex = -1;

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
        Debug.Log("SelectSlot called: " + index);
        if (index < 0 || index >= hotbarSlots.Length) return;

        // Always unequip current equip
        if (CharacterManager.Instance.Player.equip.curEquip != null)
        {
            CharacterManager.Instance.Player.equip.UnEquip();
            Debug.Log("Unequipped current item");
        }

        // Update selection index
        selectedIndex = index;

        // Highlight only the selected slot
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].outline.enabled = (i == selectedIndex);

        // Equip the new slot's item if it's equipable
        ItemSlot newSlot = hotbarSlots[selectedIndex];
        if (newSlot.item != null && newSlot.item.type == ItemType.Equipable)
        {
            CharacterManager.Instance.Player.equip.EquipNew(newSlot.item);
            Debug.Log("Equipped: " + newSlot.item.displayName);
        }
    }


    private void UseSelectedItem()
    {
        if (hotbarSlots[selectedIndex].item == null) return;

        // Let UIInventory handle usage and data sync
        inventoryUI.UseItem(selectedIndex, true); // true = hotbar slot
    }
}
