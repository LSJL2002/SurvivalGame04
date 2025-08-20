using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private UIInventory uiInventory;
    public LayerMask placementLayer;
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

            // Check placement using Renderer bounds instead of disabled collider
            Vector3 size = previewObject.GetComponent<Renderer>().bounds.extents * 0.9f;
            canPlace = !Physics.CheckBox(previewObject.transform.position, size);

            SetPreviewMaterial(canPlace ? validMaterial : invalidMaterial);

            // Right click to place
            if (Input.GetMouseButtonDown(1) && canPlace)
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

        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void PlaceObject()
    {
        if (currentBuildItem == null || previewObject == null) return;

        GameObject realObject = Instantiate(currentBuildItem.buildPrefab, 
                                            previewObject.transform.position, 
                                            previewObject.transform.rotation);

        foreach (var col in realObject.GetComponentsInChildren<Collider>())
            col.enabled = true;

        uiInventory.RemoveItem(currentBuildItem, 1);

        if (!uiInventory.HasItem(currentBuildItem, 1))
        {
            CancelPlacement();
        }
    }

    public void CancelPlacement()
    {
        if (previewObject != null) Destroy(previewObject);
        previewObject = null;
        currentBuildItem = null;
    }

    private void SetPreviewMaterial(Material mat)
    {
        foreach (var r in previewRenderers)
            r.material = mat;
    }
}
