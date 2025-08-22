using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [SerializeField] private UIInventory uiInventory;
    public LayerMask placementLayer;
    public Material validMaterial;
    public Material invalidMaterial;

    private ItemData currentBuildItem;
    private GameObject previewObject;
    private MeshRenderer[] previewRenderers;
    private float rotationY;
    private bool canPlace;


    public void StartPlacing(ItemData item)
    {
        if (item == null || item.type != ItemType.Build || item.buildPrefab == null) return;

        currentBuildItem = item;

        // Create preview object
        if (previewObject != null) Destroy(previewObject);
        previewObject = Instantiate(item.buildPrefab);
        previewRenderers = previewObject.GetComponentsInChildren<MeshRenderer>();

        // Disable colliders for preview
        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    public void CancelPlacement()
    {
        if (previewObject != null) Destroy(previewObject);
        previewObject = null;
        currentBuildItem = null;
    }

    void Update()
    {
        if (currentBuildItem == null) return;
        //If statment()
        UpdatePreview();

        // Rotate with mouse scroll
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            rotationY += scroll * 15f; // rotate 15 degrees per scroll
        }

        // Place object on right click
        if (Input.GetMouseButtonDown(1) && canPlace)
        {
            PlaceObject();
        }
    }

    private void UpdatePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, placementLayer))
        {
            Collider objCollider = previewObject.GetComponent<Collider>();
            float heightOffset = (objCollider != null) ? objCollider.bounds.extents.y : 0f;

            // Position preview above ground
            previewObject.transform.position = hit.point + Vector3.up * heightOffset;

            // Keep current rotationY while aligning to ground
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            previewObject.transform.rotation = baseRotation * Quaternion.Euler(0f, rotationY, 0f);

            // Check for placement validity
            canPlace = true;
            if (objCollider != null)
            {
                Vector3 size = objCollider.bounds.extents;
                canPlace = !Physics.CheckBox(previewObject.transform.position, size, previewObject.transform.rotation, placementLayer);
            }

            SetPreviewMaterial(canPlace ? validMaterial : invalidMaterial);
        }
    }


    private void PlaceObject()
    {
        if (currentBuildItem == null || previewObject == null) return;

        // Capture preview rotation before instantiating
        Quaternion finalRot = previewObject.transform.rotation;
        Vector3 finalPos = previewObject.transform.position;

        GameObject newObj = Instantiate(currentBuildItem.buildPrefab, finalPos, finalRot);

        foreach (var col in newObj.GetComponentsInChildren<Collider>())
            col.enabled = true;

        Debug.Log("Placing object with rotation: " + newObj.transform.rotation.eulerAngles);

        uiInventory.RemoveItem(currentBuildItem, 1);
        if (!uiInventory.HasItem(currentBuildItem, 1))
        {
            CancelPlacement();
        }

    }



    private void SetPreviewMaterial(Material mat)
    {
        if (previewRenderers == null) return;

        foreach (var r in previewRenderers)
        {
            r.material = mat;
        }
    }
}
