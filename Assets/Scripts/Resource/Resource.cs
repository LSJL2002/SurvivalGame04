using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Resource : MonoBehaviour
{
    public ItemData itemToGive;
    public int quantityPerHit = 1;
    public int capacity;                    // max capacity. can change in Inspector
    public float respawnTime;

    public List<ToolType> allowedTools;

    private int originalCapacity;
    private int curCapacity;                // current capacity. use only in script
    private bool isDepleted = false;        // check is it depleted

    private Renderer childRenderer;
    private Collider resourceCollider;

    public void Start()
    {
        originalCapacity = capacity;
        curCapacity = capacity;             // Initialize capacity value

        childRenderer = GetComponentInChildren<Renderer>();
        resourceCollider = GetComponent<Collider>();
    }

    public void Gather(Vector3 hitPoint, Vector3 hitNormal, EquipTool usingTool)
    {
        if (isDepleted || !allowedTools.Contains(usingTool.ToolType))
        {
            return;
        }
        
        for (int i = 0; i < quantityPerHit; i++)
        {
            if (capacity <= 0) break;

            capacity -= 1;
            Instantiate(itemToGive.dropPrefab, hitPoint + Vector3.up, Quaternion.LookRotation(hitNormal, Vector3.up));
            Debug.Log("Instantiate");
        }

        if (capacity <= 0)
        {
            Startrespawn();
        }
    }

    private void Startrespawn()
    {
        isDepleted = true;

        childRenderer.enabled = false;
        resourceCollider.enabled = false;

        StartCoroutine(RespawnAfterTime());
    }

    private IEnumerator RespawnAfterTime()
    {
        yield return new WaitForSeconds(respawnTime);

        capacity = originalCapacity;
        curCapacity = capacity;
        isDepleted = false;

        childRenderer.enabled = true;
        resourceCollider.enabled = true;
    }
}

