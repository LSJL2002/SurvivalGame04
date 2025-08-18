using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public ItemData item;
    public UIInventory inventory;
    public Button button;
    public Image icon;
    public TextMeshProUGUI quantityText;
    public Outline outline;

    public int index;
    public bool equipped;
    public int quantity;

    private Image draggedIcon;
    private Canvas parentCanvas;
    private bool droppedOutside;

    void Awake()
    {
        outline = GetComponent<Outline>();
    }

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void Set()
    {
        if (item == null)
        {
            Clear();
            return;
        }

        icon.gameObject.SetActive(true);
        icon.sprite = item.icon;
        quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }

    public void Clear()
    {
        item = null;
        icon.gameObject.SetActive(false);
        quantityText.text = "";
        equipped = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null)
        {
            // Clear selection in inventory UI
            inventory.ClearSelectedItem();
            return;
        }
        inventory.SelectItem(index, IsHotbarSlot());

    }

    private bool IsHotbarSlot()
    {
        foreach (var s in inventory.GetHotbarSlots())
            if (s == this) return true;
        return false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        draggedIcon = new GameObject("DraggedIcon", typeof(Image)).GetComponent<Image>();
        draggedIcon.transform.SetParent(parentCanvas.transform, false);
        draggedIcon.sprite = icon.sprite;
        draggedIcon.rectTransform.sizeDelta = icon.rectTransform.sizeDelta;
        draggedIcon.raycastTarget = false;

        icon.enabled = false;
        droppedOutside = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        draggedIcon.rectTransform.localPosition = localPoint;

        RectTransform invRect = inventory.inventoryWindow.transform as RectTransform;
        //Check to see if the dragged location is outside of the inventory.
        droppedOutside = !RectTransformUtility.RectangleContainsScreenPoint(invRect, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null) Destroy(draggedIcon.gameObject);
        icon.enabled = true;

        if (droppedOutside && item != null)
        {
            inventory.ThrowItem(item, quantity); // pass quantity
            Clear();
            inventory.UpdateUI();
            inventory.UpdateHotbarDisplay();
            inventory.UpdateHotbarData(index);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var fromSlot = eventData.pointerDrag?.GetComponent<ItemSlot>();
        if (fromSlot != null && fromSlot != this)
        {
            inventory.SwapSlots(fromSlot, this);
        }
    }
}
