using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    public ItemData itemToGive;
    public int quantityPerHit = 1;
    public int capacity;

    public List<ToolType> allowedTools;

    public void Gather(Vector3 hitPoint, Vector3 hitNormal, EquipTool usingTool)
    {
        if (!allowedTools.Contains(usingTool.ToolType))
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
            Destroy(gameObject);
        }
    }
}

