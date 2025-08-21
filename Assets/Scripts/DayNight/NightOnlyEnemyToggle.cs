using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NightOnlyEnemyToggle : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle dayNight;        // 씬의 DayNightCycle
    public GameObject monsterPrefab;      // NavMeshAgent 포함 프리팹

    [Header("Spawn Settings")]
    public int spawnCount = 5;            // 스폰 수
    public float spawnRadius = 50f;       // 스폰 반경
    public LayerMask groundLayer = ~0;    // 바닥 레이어(기본: 전체)
    public float minSpawnHeight = 1f;     // 바닥에서 띄우기

    private readonly List<GameObject> activeMonsters = new();
    private bool hasSpawned = false;

    void Update()
    {
        if (dayNight == null || monsterPrefab == null) return;

        if (dayNight.isNight && !hasSpawned)
        {
            SpawnMonsters();
            hasSpawned = true;
        }
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
            Vector3 pos = GetRandomSpawnPointOnNavMesh();
            GameObject m = Instantiate(monsterPrefab, pos, Quaternion.identity);
            activeMonsters.Add(m);

            // 생성 직후 NavMesh에 확실히 올려두기
            var ag = m.GetComponent<NavMeshAgent>();
            if (ag != null)
            {
                if (!ag.enabled) ag.enabled = true;
                if (!ag.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(pos, out NavMeshHit nh, 2f, NavMesh.AllAreas))
                        ag.Warp(nh.position);
                }
            }
        }
        Debug.Log($"[Spawner] Spawned {spawnCount} monsters.");
    }

    void DespawnMonsters()
    {
        foreach (var m in activeMonsters)
            if (m) Destroy(m);
        activeMonsters.Clear();
        Debug.Log("[Spawner] All monsters despawned.");
    }

    Vector3 GetRandomSpawnPointOnNavMesh()
    {
        for (int attempts = 0; attempts < 20; attempts++)
        {
            Vector3 randomTop = transform.position + Random.insideUnitSphere * spawnRadius;
            randomTop.y += 20f; // 위에서 아래로 캐스팅

            if (Physics.Raycast(randomTop, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                // 바닥 근처에서 NavMesh 샘플
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit nHit, 5f, NavMesh.AllAreas))
                    return nHit.position + Vector3.up * minSpawnHeight;
            }
        }

        // 실패 시: 스포너 위치에서 샘플
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit n, 10f, NavMesh.AllAreas))
            return n.position + Vector3.up * minSpawnHeight;

        // 정말 없으면 원점 반환(Enemy가 자체 EnsureOnNavMesh로 복구 시도)
        return transform.position + Vector3.up * minSpawnHeight;
    }
}
