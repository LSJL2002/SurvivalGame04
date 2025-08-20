using UnityEngine;
using UnityEngine.AI;

public class NightOnlyEnemyToggle : MonoBehaviour
{
    [Tooltip("씬의 DayNightCycle. 비워두면 자동으로 찾음")]
    public DayNightCycle dayNight;

    [Header("낮에는 끌(숨길) 비주얼 루트(자식 오브젝트). 비워두면 렌더러만 토글")]
    public Transform visualRoot;

    [Header("토글 대상(비워두면 자동 수집)")]
    public Renderer[] renderers;
    public Collider[] colliders;
    public Behaviour[] componentsToToggle; // ⚠️ NavMeshAgent는 넣지 말 것!

    public bool autoCollectIfEmpty = true;

    private bool lastNight = false;
    private NavMeshAgent agent;

    void Awake()
    {
        if (dayNight == null) dayNight = FindObjectOfType<DayNightCycle>();
        agent = GetComponent<NavMeshAgent>();

        if (autoCollectIfEmpty)
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);

            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(true);
        }
    }

    void Start()
    {
        Apply(dayNight != null && dayNight.isNight);
    }

    void Update()
    {
        if (dayNight == null) return;
        if (lastNight != dayNight.isNight)
            Apply(dayNight.isNight);
    }

    void Apply(bool night)
    {
        lastNight = night;

        // 비주얼 토글
        if (visualRoot != null)
        {
            if (visualRoot.gameObject.activeSelf != night)
                visualRoot.gameObject.SetActive(night);
        }
        else
        {
            if (renderers != null)
                foreach (var r in renderers) if (r) r.enabled = night;
        }

        // 콜라이더/기타 스크립트 토글 (Agent 제외!)
        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = night;

        if (componentsToToggle != null)
            foreach (var b in componentsToToggle) if (b) b.enabled = night;

        // NavMeshAgent는 컴포넌트 비활성화 X, isStopped로만 제어
        if (agent)
        {
            if (!night)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            }
            else
            {
                EnsureOnNavMesh(agent);    // 밤에 켤 때 NavMesh에 얹어놓기
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            }
        }
    }

    void EnsureOnNavMesh(NavMeshAgent a)
    {
        if (!a.enabled) a.enabled = true;

        // NavMesh에 없으면 근처 위치 샘플 후 워프
        if (!a.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
            {
                a.Warp(hit.position);  // Warp는 NavMesh 밖에서도 호출 가능
            }
            // 근처에 NavMesh가 전혀 없으면(샘플 실패) -> 에이전트는 일단 멈춰있게 둠
        }
    }
}
