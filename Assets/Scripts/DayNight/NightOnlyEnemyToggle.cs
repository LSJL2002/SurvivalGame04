using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NightOnlyEnemyToggle : MonoBehaviour
{
    public DayNightCycle dayNight;          // 씬의 DayNightCycle (인스펙터에 드래그 권장)

    [Header("테스트용")]
    public bool debugOverride = false;      // 체크하면 강제
    public bool debugNight = false;         // true=밤, false=낮

    // 한 번만 모아두고, 매 프레임 강제로 on/off
    List<Renderer> renderers = new List<Renderer>();
    List<Collider> colliders = new List<Collider>();
    List<Animator> animators = new List<Animator>();
    NavMeshAgent agent;
    bool lastNight;

    void Awake()
    {
        if (dayNight == null) dayNight = FindObjectOfType<DayNightCycle>();
        agent = GetComponent<NavMeshAgent>();

        // Enemy 루트 전체에서 시각/충돌/애니메이터를 한 번만 수집
        renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        colliders.AddRange(GetComponentsInChildren<Collider>(true));
        animators.AddRange(GetComponentsInChildren<Animator>(true));
    }

    void Update()
    {
        if (dayNight == null && !debugOverride) return;

        bool night = dayNight ? dayNight.isNight : false;
        if (debugOverride) night = debugNight;

        // 밤/낮 상태가 바뀔 때만 로그
        if (lastNight != night)
        {
            lastNight = night;
            Debug.Log($"[NightToggle] {name} night={night}");
        }

        // ✅ 매 프레임 강제 동기화: 밤이면 전부 켜기, 낮이면 전부 끄기
        foreach (var r in renderers) if (r) r.enabled = night;
        foreach (var c in colliders) if (c) c.enabled = night;
        foreach (var a in animators) if (a) a.enabled = night;

        // NavMeshAgent는 enable/disable 하지 않고 isStopped만 제어
        if (agent)
        {
            if (!agent.enabled) agent.enabled = true;

            if (night)
            {
                // 밤: NavMesh 위로 보정 + 이동 허용
                if (!agent.isOnNavMesh &&
                    NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                    agent.Warp(hit.position);

                agent.isStopped = false;
            }
            else
            {
                // 낮: 정지
                agent.isStopped = true;
            }
        }
    }
}
