using System.Collections;
using System.Data.Common;
using TMPro;
using UnityEngine;



public class UIInventory : MonoBehaviour
{
    [SerializeField] public BuildingSystem buildingSystem;
    private enum ActiveMenu
    {
        None,
        Inventory,
        Crafting
    }
    private ActiveMenu currentMenu = ActiveMenu.None;
    [Header("Panels")]
    public Transform inventorySlotPanel;
    public Transform hotbarSlotPanel;
    public GameObject craftingWindow;

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

        controller.inventory += ToggleUI;
        CharacterManager.Instance.Player.addItem += AddItem;

        inventoryWindow.SetActive(false);
        craftingWindow.SetActive(false);

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


    public void UpdateHotbarDisplay()
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

    public void ToggleUI()
    {
        // If either menu is open, close both
        if (currentMenu != ActiveMenu.None)
        {
            inventoryWindow.SetActive(false);
            craftingWindow.SetActive(false);
            currentMenu = ActiveMenu.None;
        }
        else
        {
            // If nothing is open, open inventory by default
            inventoryWindow.SetActive(true);
            craftingWindow.SetActive(false);
            currentMenu = ActiveMenu.Inventory;
        }
    }

    public bool IsOpen() => inventoryWindow.activeInHierarchy;

    public void AddItem()
    {
        ItemData data = CharacterManager.Instance.Player.itemData;
        if (data == null) return;

        if (data.canStack)
        {
            foreach (var slot in hotbarSlots)
            {
                if (slot.item == data && slot.quantity < data.maxStackAmount)
                {
                    slot.quantity++;
                    UpdateUI();
                    UpdateHotbarDisplay();
                    CharacterManager.Instance.Player.itemData = null;
                    return;
                }
            }
        }

        foreach (var slot in hotbarSlots)
        {
            if (slot.item == null)
            {
                slot.item = data;
                slot.quantity = 1;
                UpdateUI();
                UpdateHotbarDisplay();
                CharacterManager.Instance.Player.itemData = null;
                return;
            }
        }

        if (data.canStack)
        {
            ItemSlot stackSlot = GetStackSlot(data);
            if (stackSlot != null)
            {
                stackSlot.quantity++;
                UpdateUI();
                UpdateHotbarDisplay();
                CharacterManager.Instance.Player.itemData = null;
                return;
            }
        }

        ItemSlot emptySlot = GetEmptySlot();
        if (emptySlot != null)
        {
            emptySlot.item = data;
            emptySlot.quantity = 1;
            UpdateUI();
            UpdateHotbarDisplay();
            CharacterManager.Instance.Player.itemData = null;
            return;
        }

        ThrowItem(data, 1);
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

    public void ThrowItem(ItemData data, int amount)
    {
        var player = CharacterManager.Instance.Player;
        Transform playerTransform = CharacterManager.Instance.Player.transform;

        if (player.equip.curEquip != null && player.equip.curEquip.itemData == data)
        {
            player.equip.UnEquip();
        }

        for (int i = 0; i < amount; i++)
        {
            // Forward direction + slight random spread
            Vector3 forwardOffset = playerTransform.forward * 1.5f; // throw distance
            Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            Vector3 spawnPosition = dropPosition.position + forwardOffset + randomOffset;

            Instantiate(data.dropPrefab, spawnPosition, Quaternion.identity);
        }

        UpdateUI();           // Refresh visuals
        UpdateHotbarDisplay(); // Refresh hotbar UI
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

        ItemData data = slot.item;

        Debug.Log($"Using {data.displayName}");

        if (data.type == ItemType.Consumable)
        {
            foreach (var c in data.consumables)
            {
                Debug.Log("Testing");
                ApplyConsumableEffect(c, data.boostDuration);
            }

            // Reduce stack
            slot.quantity--;
            if (slot.quantity <= 0) slot.Clear();

            UpdateUI();
            UpdateHotbarDisplay();
        }
        else if (data.type == ItemType.Build)
        {
            buildingSystem.StartPlacing(data);
        }
    }

    private void ApplyConsumableEffect(ItemDataConsumable consumable, float duration)
    {
        var PlayerCondition = CharacterManager.Instance.Player.condition;

        switch (consumable.type)
        {
            case ConsumableType.Health:
                PlayerCondition.Heal(consumable.value);
                break;
            case ConsumableType.Stamina:
                PlayerCondition.Recover(consumable.value);
                break;
            case ConsumableType.Thirst:
                PlayerCondition.Drink(consumable.value);
                break;
            case ConsumableType.Hunger:
                PlayerCondition.Eat(consumable.value);
                break;
            case ConsumableType.Boost:
                ApplyBoost(consumable, duration);
                break;
        }
    }

    private IEnumerator ApplyBoost(ItemDataConsumable consumable, float duration)
    {
        var player = CharacterManager.Instance.Player;
        var controller = player.controller;
        var condition = player.condition;

        float baseMoveSpeed = controller.moveSpeed;
        float baseJumpForce = controller.jumpPower;

        switch (consumable.boostType)
        {
            case BoostType.Speed:
                controller.moveSpeed = baseMoveSpeed * consumable.value;
                yield return new WaitForSeconds(duration);
                controller.moveSpeed = baseMoveSpeed;
                break;

            case BoostType.Jump:
                controller.jumpPower = baseJumpForce * consumable.value;
                yield return new WaitForSeconds(duration);
                controller.jumpPower = baseJumpForce;
                break;

            case BoostType.Stamina:
                condition.SendMessage("EnableInfiniteStamina", true);
                yield return new WaitForSeconds(duration);
                condition.SendMessage("EnableInfiniteStamina", false);
                break;
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

    public bool HasItem(ItemData item, int amount)
    {
        int total = 0;

        // Count in hotbar
        foreach (var slot in hotbarSlots)
        {
            if (slot.item == item)
                total += slot.quantity;
        }

        // Count in inventory
        foreach (var slot in inventorySlots)
        {
            if (slot.item == item)
                total += slot.quantity;
        }

        return total >= amount;
    }
    public void RemoveItem(ItemData item, int amount)
    {
        int remaining = amount;

        // Remove from hotbar first
        foreach (var slot in hotbarSlots)
        {
            if (slot.item == item)
            {
                if (slot.quantity >= remaining)
                {
                    slot.quantity -= remaining;
                    if (slot.quantity <= 0) slot.Clear();
                    UpdateUI();
                    UpdateHotbarDisplay();
                    return;
                }
                else
                {
                    remaining -= slot.quantity;
                    slot.Clear();
                }
            }
        }

        // Then remove from inventory
        foreach (var slot in inventorySlots)
        {
            if (slot.item == item)
            {
                if (slot.quantity >= remaining)
                {
                    slot.quantity -= remaining;
                    if (slot.quantity <= 0) slot.Clear();
                    UpdateUI();
                    UpdateHotbarDisplay();
                    return;
                }
                else
                {
                    remaining -= slot.quantity;
                    slot.Clear();
                }
            }
        }

        UpdateUI();
        UpdateHotbarDisplay();
    }


    // Public access to both slot arrays
    public ItemSlot[] GetInventorySlots() => inventorySlots;
    public ItemSlot[] GetHotbarSlots() => hotbarSlots;
}
