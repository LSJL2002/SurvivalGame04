using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NightOnlyEnemyToggle : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle dayNight; // Scene’s DayNightCycle
    public GameObject monsterPrefab;

    [Header("Spawn Settings")]
    public int spawnCount = 5;            // How many monsters to spawn
    public float spawnRadius = 50f;       // Radius around spawner to place monsters
    public LayerMask groundLayer;         // Where monsters can spawn
    public float minSpawnHeight = 1f;     // Height offset above ground

    private List<GameObject> activeMonsters = new List<GameObject>();
    private bool hasSpawned = false;

    void Update()
    {
        if (dayNight == null || monsterPrefab == null) return;

        // Night started → spawn if not already
        if (dayNight.isNight && !hasSpawned)
        {
            SpawnMonsters();
            hasSpawned = true;
        }
        // Day started → despawn all
        else if (!dayNight.isNight && hasSpawned)
        {
            DespawnMonsters();
            hasSpawned = false;
        }
    }

    void SpawnMonsters()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
            activeMonsters.Add(monster);
        }
        Debug.Log($"[Spawner] Spawned {spawnCount} monsters.");
    }

    void DespawnMonsters()
    {
        foreach (GameObject m in activeMonsters)
        {
            if (m != null) Destroy(m);
        }
        activeMonsters.Clear();
        Debug.Log("[Spawner] All monsters despawned.");
    }

    Vector3 GetRandomSpawnPoint()
    {
        for (int attempts = 0; attempts < 10; attempts++) // try multiple times
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPos.y += 20f; // cast from above

            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 50f, groundLayer))
            {
                return hit.point + Vector3.up * minSpawnHeight;
            }
        }
        // fallback: just return spawner position
        return transform.position + Vector3.up * minSpawnHeight;
    }
}