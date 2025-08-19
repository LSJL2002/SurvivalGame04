using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private UIInventory uiInventory;
    public LayerMask placementLayer; // e.g. Ground layer
    public Material validMaterial;
    public Material invalidMaterial;

    private GameObject previewObject;
    private MeshRenderer[] previewRenderers;
    private ItemData currentBuildItem;

    private bool canPlace;

    void Update()
    {
        if (previewObject == null) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, placementLayer))
        {
            previewObject.transform.position = hit.point;
            previewObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // simple check: if not colliding with anything → valid placement
            canPlace = !Physics.CheckBox(previewObject.transform.position, 
                                         previewObject.GetComponent<Collider>().bounds.extents * 0.9f);

            SetPreviewMaterial(canPlace ? validMaterial : invalidMaterial);

            // Left click to place
            if (Input.GetMouseButtonDown(0) && canPlace)
            {
                PlaceObject();
            }
        }
    }

    public void StartPlacing(ItemData item)
    {
        if (item.type != ItemType.Build || item.buildPrefab == null) return;

        if (previewObject != null) Destroy(previewObject);

        currentBuildItem = item;
        previewObject = Instantiate(item.buildPrefab);
        previewRenderers = previewObject.GetComponentsInChildren<MeshRenderer>();

        // disable colliders so it doesn’t block raycasts
        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void PlaceObject()
    {
        if (currentBuildItem == null) return;

        // Place actual object
        GameObject realObject = Instantiate(currentBuildItem.buildPrefab, 
                                            previewObject.transform.position, 
                                            previewObject.transform.rotation);

        // Re-enable colliders on the real one
        foreach (var col in realObject.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // Remove 1 item from inventory
        uiInventory.RemoveItem(currentBuildItem, 1);

        // If no more of that item → cancel build mode
        if (!uiInventory.HasItem(currentBuildItem, 1))
        {
            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        if (previewObject != null) Destroy(previewObject);
        previewObject = null;
        currentBuildItem = null;
    }

    private void SetPreviewMaterial(Material mat)
    {
        foreach (var r in previewRenderers)
        {
            r.material = mat;
        }
    }
}
