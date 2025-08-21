using System.Collections;
using UnityEngine;

public class InteractableResource : MonoBehaviour, IInteractable
{
    [Header("Resource Settings")]
    public ItemData item;                  
    public int quantityPerInteract = 1;    
    public int capacity = 5;               
    public float respawnTime = 5f;         

    private int originalCapacity;
    private int curCapacity;
    private bool isDepleted = false;

    private Collider resourceCollider;
    private float respawnTimer;

    void Start()
    {
        originalCapacity = capacity;
        curCapacity = capacity;

        resourceCollider = GetComponent<Collider>();
    }

    public string GetInteractPrompt()
    {
        if (isDepleted)
        {
            return $"{name} (Depleted - {Mathf.Ceil(respawnTimer)}s left)";
        }
        return $"{name} (Press E to gather)";
    }

    public void OnInteract()
    {
        if (isDepleted) return;

        Vector3 spawnOffset = transform.forward + Vector3.up;
        Vector3 spawnPosition = transform.position + spawnOffset;
        for (int i = 0; i < quantityPerInteract; i++)
        {
            if (capacity <= 0) break;
            
            capacity -= 1;
            Instantiate(item.dropPrefab, spawnPosition, Quaternion.identity);
        }

        if (capacity <= 0)
        {
            StartRespawn();
        }
    }

    private void StartRespawn()
    {
        isDepleted = true;
        respawnTimer = respawnTime;
        StartCoroutine(RespawnAfterTime());
    }

    private IEnumerator RespawnAfterTime()
    {
        while (respawnTimer > 0)
        {
            respawnTimer -= Time.deltaTime;
            yield return null;
        }

        capacity = originalCapacity;
        curCapacity = capacity;
        isDepleted = false;

        if (resourceCollider != null) resourceCollider.enabled = true;
    }
}
