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

        // Unequip current equip if any
        if (CharacterManager.Instance.Player.equip.curEquip != null)
        {
            CharacterManager.Instance.Player.equip.UnEquip();
            Debug.Log("Unequipped current item");
        }

        selectedIndex = index;

        // Highlight only the selected slot
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].outline.enabled = (i == selectedIndex);

        ItemSlot newSlot = hotbarSlots[selectedIndex];

        // Equip if equipable
        if (newSlot.item != null && newSlot.item.type == ItemType.Equipable)
        {
            CharacterManager.Instance.Player.equip.EquipNew(newSlot.item);
            Debug.Log("Equipped: " + newSlot.item.displayName);
        }

        // Show build preview if it's a build item
        if (newSlot.item != null && newSlot.item.type == ItemType.Build)
        {
            inventoryUI.buildingSystem.StartPlacing(newSlot.item);
        }
        else
        {
            // Turn off preview if not a build item
            inventoryUI.buildingSystem.CancelPlacement();
        }
    }

    private void UseSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= hotbarSlots.Length) return;

        if (hotbarSlots[selectedIndex].item == null) return;

        inventoryUI.UseItem(selectedIndex, true);
    }
}
