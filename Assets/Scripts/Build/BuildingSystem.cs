using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private UIInventory uiInventory;
    public LayerMask placementLayer;

    private ItemData currentBuildItem;

    public void StartPlacing(ItemData item)
    {
        if (item == null || item.type != ItemType.Build || item.buildPrefab == null) return;

        currentBuildItem = item;
    }

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
            // Instantiate the object
            GameObject newObj = Instantiate(currentBuildItem.buildPrefab);

            // Align rotation to surface normal
            newObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // Adjust position so bottom of the object sits on the hit point
            Collider objCollider = newObj.GetComponent<Collider>();
            if (objCollider != null)
            {
                newObj.transform.position = hit.point + Vector3.up * objCollider.bounds.extents.y;
            }
            else
            {
                newObj.transform.position = hit.point;
            }

            // Remove item from inventory
            uiInventory.RemoveItem(currentBuildItem, 1);

            // Stop placing if no more items
            if (!uiInventory.HasItem(currentBuildItem, 1))
                CancelPlacement();
        }
    }
}
