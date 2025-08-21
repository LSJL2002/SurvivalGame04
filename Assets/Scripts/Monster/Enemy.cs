using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Refs")]
    public DayNightCycle dayNight;        // 비우면 자동 탐색
    public Transform target;              // 비우면 Player 태그 자동 탐색

    private NavMeshAgent agent;
    private Animator anim;
    private Rigidbody rb;

    [Header("AI Settings")]
    public float sightRange = 12f;
    public float attackRange = 1.8f;
    public float repathInterval = 0.2f;
    public float attackCooldown = 1.0f;

    [Header("NavMesh")]
    public float navSampleRadius = 3f;    // NavMesh.SamplePosition 반경
    public bool autoWarpToNavMesh = true; // NavMesh 밖이면 근처로 워프

    [Header("Knockback")]
    public float defaultKnockbackTime = 0.2f; // ApplyKnockback(dir, power) 사용 시 지속시간
    public float knockbackDrag = 8f;          // 넉백 중 감속(리짓바디 있을 때)

    [Header("Death")]
    public float despawnAfter = 5f;      // 죽고 나서 파괴까지 시간(<=0이면 유지)

    // 내부 상태
    private float _nextPathTime;
    private float _nextAttackTime;
    private bool _isNightCached;
    private bool _inKnockback;
    private float _stunUntil;
    private bool _isDead;

    // 캐시
    private Collider[] _colliders;

    void Awake()
    {
        if (dayNight == null) dayNight = FindObjectOfType<DayNightCycle>();
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);

        EnsureOnNavMesh();
        SafeStop(); // 시작은 멈춤(밤에만 움직이게)
    }

    void OnEnable()
    {
        EnsureOnNavMesh();
    }

    void Update()
    {
        if (_isDead) return;

        // 밤/낮 전환 한번만 반영
        bool isNight = dayNight == null || dayNight.isNight;
        if (_isNightCached != isNight)
        {
            _isNightCached = isNight;
            if (isNight)
            {
                EnsureOnNavMesh();
                SafeResume();
            }
            else
            {
                SafeStop();
                if (anim) anim.SetFloat("Speed", 0f);
                return;
            }
        }
        if (!isNight) return;

        // 넉백/스턴 중이면 AI 중지
        if (_inKnockback || Time.time < _stunUntil)
        {
            if (anim) anim.SetFloat("Speed", 0f);
            return;
        }

        // 타겟 확보
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
            if (target == null)
            {
                SafeStop();
                if (anim) anim.SetFloat("Speed", 0f);
                return;
            }
        }

        EnsureOnNavMesh();

        float dist = Vector3.Distance(transform.position, target.position);

        // 시야 밖 -> 정지
        if (dist > sightRange)
        {
            SafeStop();
            if (anim) anim.SetFloat("Speed", 0f);
            return;
        }

        bool inAttack = dist <= attackRange;

        if (!inAttack)
        {
            if (Time.time >= _nextPathTime)
            {
                _nextPathTime = Time.time + repathInterval;
                SafeSetDestination(target.position);
            }

            if (anim)
            {
                float speed = AgentReady() ? agent.velocity.magnitude : 0f;
                anim.SetFloat("Speed", speed);
            }
        }
        else
        {
            SafeStop();
            if (anim) anim.SetFloat("Speed", 0f);

            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + attackCooldown;
                if (anim) anim.SetTrigger("Attack");
                // 실제 데미지는 애니메이션 이벤트에서 호출하는 걸 권장.
                // DealDamage();
            }
        }
    }

    // ===================== 외부에서 호출할 API =====================

    /// <summary>
    /// 넉백(지속시간은 defaultKnockbackTime 사용)
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float power)
    {
        ApplyKnockback(direction, power, defaultKnockbackTime);
    }

    /// <summary>
    /// 넉백(방향/힘/지속시간 지정)
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float power, float duration)
    {
        if (_isDead) return;
        direction.y = 0f;
        StartCoroutine(KnockbackRoutine(direction.normalized, power, duration));
    }

    /// <summary>
    /// 사망 상태로 전환(외부에서 HP 0 될 때 호출)
    /// </summary>
    public void SetDeadState()
    {
        if (_isDead) return;
        _isDead = true;

        // 에이전트/이동 정지
        SafeStop();
        if (agent) agent.enabled = false;

        // 콜라이더 끄기(원하면 루트만 남기고 끄기)
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i]) _colliders[i].enabled = false;
        }

        // 애니메이션
        if (anim)
        {
            // 프로젝트에 맞춰 둘 중 하나/둘 다 사용
            anim.SetBool("Dead", true);
            anim.SetTrigger("Die");
            anim.SetFloat("Speed", 0f);
        }

        // 리짓바디가 있다면 넘어지지 않게 고정
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 일정 시간 후 삭제(옵션)
        if (despawnAfter > 0f) Destroy(gameObject, despawnAfter);
        // 계속 남길 거면 여기서 return; 만 해도 OK
    }

    // ===================== Knockback 구현 =====================

    IEnumerator KnockbackRoutine(Vector3 dir, float power, float time)
    {
        _inKnockback = true;
        _stunUntil = Time.time + time;

        bool agentWasEnabled = agent != null && agent.enabled;

        // 에이전트 잠시 끄고 물리 이동(있는 경우)
        if (agentWasEnabled) agent.enabled = false;

        if (rb)
        {
            bool prevKinematic = rb.isKinematic;
            rb.isKinematic = false;
            rb.drag = knockbackDrag;

            rb.velocity = Vector3.zero;
            rb.AddForce(dir * power, ForceMode.VelocityChange);

            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                yield return null;
            }

            rb.velocity = Vector3.zero;
            rb.isKinematic = prevKinematic;
        }
        else
        {
            // 리짓바디가 없으면 간단 이동
            float t = 0f;
            Vector3 start = transform.position;
            Vector3 end = start + dir * power * 0.1f; // 파워 스케일 약간 줄임
            while (t < time)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(0f, 1f, t / time);
                transform.position = Vector3.Lerp(start, end, a);
                yield return null;
            }
        }

        // NavMesh 복귀
        if (agent)
        {
            agent.enabled = true;
            EnsureOnNavMesh();
            agent.isStopped = false;
        }

        _inKnockback = false;
    }

    // ===================== NavMesh 안전 유틸 =====================

    bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    void SafeStop()
    {
        if (AgentReady()) agent.isStopped = true;
        if (agent != null) agent.velocity = Vector3.zero;
    }

    void SafeResume()
    {
        if (AgentReady()) agent.isStopped = false;
    }

    bool SafeSetDestination(Vector3 pos)
    {
        return AgentReady() && agent.SetDestination(pos);
    }

    float SafeRemainingDistance(float fallback = Mathf.Infinity)
    {
        return AgentReady() ? agent.remainingDistance : fallback;
    }

    void EnsureOnNavMesh()
    {
        if (agent == null) return;

        if (!agent.enabled) agent.enabled = true;
        if (agent.isOnNavMesh) return;

        if (autoWarpToNavMesh &&
            NavMesh.SamplePosition(transform.position, out var hit, navSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
