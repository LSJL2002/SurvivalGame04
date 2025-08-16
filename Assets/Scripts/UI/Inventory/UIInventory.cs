using TMPro;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
    [Header("Panels")]
    public Transform inventorySlotPanel;
    public Transform hotbarSlotPanel;

    [Header("UI References")]
    public GameObject inventoryWindow;
    public Transform dropPosition;
    public TextMeshProUGUI selectedItemName;
    public TextMeshProUGUI selectedItemDescription;
    public TextMeshProUGUI selectedStatName;
    public TextMeshProUGUI selectedStatValue;

    [Header("Hotbar Settings")]
    public int hotbarSize = 8;

    private ItemSlot[] inventorySlots;
    private ItemSlot[] hotbarSlots;
    private ItemSlot selectedItem;
    public HotbarData hotbarData = new HotbarData();
    private int selectedItemIndex;
    [SerializeField] private UIHotbar hotbarUI;

    private PlayerController controller;
    private PlayerCondition condition;

    void Start()
    {
        controller = CharacterManager.Instance.Player.controller;
        condition = CharacterManager.Instance.Player.condition;
        dropPosition = CharacterManager.Instance.Player.dropPosition;

        controller.inventory += Toggle;
        CharacterManager.Instance.Player.addItem += AddItem;

        inventoryWindow.SetActive(false);

        // Initialize inventory slots
        inventorySlots = new ItemSlot[inventorySlotPanel.childCount];
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i] = inventorySlotPanel.GetChild(i).GetComponent<ItemSlot>();
            inventorySlots[i].index = i;
            inventorySlots[i].inventory = this;
            inventorySlots[i].Clear();
        }

        // Initialize hotbar slots
        hotbarSlots = new ItemSlot[hotbarSlotPanel.childCount];
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i] = hotbarSlotPanel.GetChild(i).GetComponent<ItemSlot>();
            hotbarSlots[i].index = i;
            hotbarSlots[i].inventory = this;
            hotbarSlots[i].Clear();
        }

        ClearSelectedItemWindow();
    }


    void UpdateHotbarDisplay()
    {
        hotbarUI.UpdateHotbarUI(hotbarSlots);
    }
    private void ClearSelectedItemWindow()
    {
        selectedItem = null;
        selectedItemName.text = "";
        selectedItemDescription.text = "";
        selectedStatName.text = "";
        selectedStatValue.text = "";
    }

    public void Toggle()
    {
        inventoryWindow.SetActive(!inventoryWindow.activeSelf);
    }

    public bool IsOpen() => inventoryWindow.activeInHierarchy;

    public void AddItem()
    {
        ItemData data = CharacterManager.Instance.Player.itemData;
        if (data == null) return;

        // Try stack first in inventory
        if (data.canStack)
        {
            ItemSlot stackSlot = GetStackSlot(data);
            if (stackSlot != null)
            {
                stackSlot.quantity++;
                UpdateUI();
                CharacterManager.Instance.Player.itemData = null;
                return;
            }
        }

        // Try empty slot
        ItemSlot emptySlot = GetEmptySlot();
        if (emptySlot != null)
        {
            emptySlot.item = data;
            emptySlot.quantity = 1;
            UpdateUI();
            CharacterManager.Instance.Player.itemData = null;
            return;
        }

        // Inventory full, drop item
        ThrowItem(data);
        CharacterManager.Instance.Player.itemData = null;
    }

    public void UpdateUI()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.item != null) slot.Set();
            else slot.Clear();
        }

        foreach (var slot in hotbarSlots)
        {
            if (slot.item != null) slot.Set();
            else slot.Clear();
        }
    }

    private ItemSlot GetStackSlot(ItemData data)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.item == data && slot.quantity < data.maxStackAmount)
                return slot;
        }
        return null;
    }

    private ItemSlot GetEmptySlot()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.item == null) return slot;
        }
        return null;
    }

    public void ThrowItem(ItemData data)
    {
        Instantiate(data.dropPrefab, dropPosition.position, Quaternion.Euler(Vector3.one * Random.value * 360));
    }

    public void SelectItem(int index, bool isHotbar = false)
    {
        ItemSlot[] targetArray = isHotbar ? hotbarSlots : inventorySlots;
        if (targetArray[index].item == null) return;

        selectedItem = targetArray[index];
        selectedItemIndex = index;

        selectedItemName.text = selectedItem.item.displayName;
        selectedItemDescription.text = selectedItem.item.description;

        selectedStatName.text = "";
        selectedStatValue.text = "";

        foreach (var c in selectedItem.item.consumables)
        {
            selectedStatName.text += c.type + "\n";
            selectedStatValue.text += c.value + "\n";
        }
    }

    public void UseItem(int index, bool isHotbar = false)
    {
        ItemSlot[] targetArray = isHotbar ? hotbarSlots : inventorySlots;
        ItemSlot slot = targetArray[index];
        if (slot.item == null) return;

        Debug.Log($"Using {slot.item.displayName}");

            if (slot.item.type == ItemType.Consumable)
            {
                // Apply effects here
                foreach (var c in slot.item.consumables)
                {
                    // Example: CharacterManager.Instance.Player.condition.Apply(c);
                }

                slot.quantity--;
                if (slot.quantity <= 0)
                {
                    // Remove the item completely
                    targetArray[index].Clear();
                }

                UpdateUI();      // Refresh visuals
                UpdateHotbarDisplay(); // Refresh hotbar UI
        }
    }


    public void HighlightHotbarSlot(int index)
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].outline.enabled = (i == index && hotbarSlots[i].item != null);
    }

    // Helpers for drag-and-drop
    public void SwapSlots(ItemSlot fromSlot, ItemSlot toSlot)
    {
        if (fromSlot == null || toSlot == null) return;

        if (fromSlot.item == toSlot.item && fromSlot.item.canStack)
        {
            int total = fromSlot.quantity + toSlot.quantity;
            toSlot.quantity = Mathf.Min(total, toSlot.item.maxStackAmount);
            fromSlot.quantity = total - toSlot.quantity;
            if (fromSlot.quantity <= 0) fromSlot.Clear();
        }
        else
        {
            // Swap items
            (fromSlot.item, toSlot.item) = (toSlot.item, fromSlot.item);
            (fromSlot.quantity, toSlot.quantity) = (toSlot.quantity, fromSlot.quantity);
            (fromSlot.equipped, toSlot.equipped) = (toSlot.equipped, fromSlot.equipped);
        }
        UpdateHotbarDisplay();
        UpdateUI();
        UpdateHotbarData(toSlot.index);
        UpdateHotbarData(fromSlot.index);
    }
    public void ClearSelectedItem()
    {
        // Clear any UI that shows selected item info
        // For example:
        selectedItemName.text = "";
        selectedItemDescription.text = "";
    }
    public void UpdateHotbarData(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;

        ItemSlot slot = hotbarSlots[index];

        // Ensure the list is big enough
        while (hotbarData.slots.Count <= index)
            hotbarData.slots.Add(new ItemSlotData());

        hotbarData.slots[index] = slot.item != null ? new ItemSlotData
        {
            item = slot.item,
            quantity = slot.quantity,
            equipped = slot.equipped
        } : new ItemSlotData(); // clear if empty
    }


    // Updates the entire hotbarData
    public void RefreshHotbarData()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
            UpdateHotbarData(i);
    }

    // Public access to both slot arrays
    public ItemSlot[] GetInventorySlots() => inventorySlots;
    public ItemSlot[] GetHotbarSlots() => hotbarSlots;
}
