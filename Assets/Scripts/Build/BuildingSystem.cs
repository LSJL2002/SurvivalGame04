using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private UIInventory uiInventory;
    public LayerMask placementLayer;

    private ItemData currentBuildItem;

    /// <summary>
    /// Call this when you select a build item in the hotbar.
    /// </summary>
    public void StartPlacing(ItemData item)
    {
        if (item == null || item.type != ItemType.Build || item.buildPrefab == null) return;

        currentBuildItem = item;
    }

    /// <summary>
    /// Call this when you deselect a build item.
    /// </summary>
    public void CancelPlacement()
    {
        currentBuildItem = null;
    }

    void Update()
    {
        if (currentBuildItem == null) return;

        // Right click to place
        if (Input.GetMouseButtonDown(1))
        {
            PlaceObject();
        }
    }

    private void PlaceObject()
    {
        if (currentBuildItem == null || currentBuildItem.buildPrefab == null) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, placementLayer))
        {
            Instantiate(currentBuildItem.buildPrefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            uiInventory.RemoveItem(currentBuildItem, 1);

            // If no more of this item, stop placing
            if (!uiInventory.HasItem(currentBuildItem, 1))
                CancelPlacement();
        }
    }
}
