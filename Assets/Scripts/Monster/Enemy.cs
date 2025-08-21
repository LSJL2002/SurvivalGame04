using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Refs")]
    public DayNightCycle dayNight;     // 비우면 자동 탐색
    public Transform target;           // 비우면 Player 태그 자동 탐색

    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;

    [Header("Combat/AI")]
    public int damage = 10;
    public float sightRange = 12f;
    public float attackRange = 1.8f;
    public float repathInterval = 0.2f;
    public float attackCooldown = 1.0f;

    [Header("Animator Triggers")]
    [SerializeField] string attackTrigger = "Attack"; // Animator와 동일한 트리거명

    [Header("Attack Movement Lock")]
    [Tooltip("공격 모션 동안 이동을 잠그는 시간(초). 애니 길이에 맞춰 조절")]
    public float attackLockDuration = 0.45f;

    [Header("NavMesh")]
    public float navSampleRadius = 3f;
    public bool autoWarpToNavMesh = true;

    [Header("Knockback")]
    public float defaultKnockbackTime = 0.2f;
    public float knockbackDrag = 8f;

    [Header("Death")]
    public float despawnAfter = 5f;

    float _nextPathTime;
    float _nextAttackTime;
    bool _isNightCached;
    bool _inKnockback;
    float _stunUntil;
    bool _isDead;

    // 공격 중 이동잠금 타이머
    float _unlockMoveAt = -1f;

    Collider[] _colliders;

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
        SafeStop(); // 시작은 멈춰두기(밤에만 활성)
        if (agent) agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);

        // 루트모션은 NavMeshAgent와 충돌하니 기본 비활성 권장
        if (anim) anim.applyRootMotion = false;
    }

    void OnEnable() => EnsureOnNavMesh();

    void Update()
    {
        if (_isDead) return;

        // 밤/낮 전환
        bool isNight = dayNight == null || dayNight.isNight;
        if (_isNightCached != isNight)
        {
            _isNightCached = isNight;
            if (isNight) { EnsureOnNavMesh(); SafeResume(); }
            else { SafeStop(); SetMoveSpeed(0f); return; }
        }
        if (!isNight) return;

        // 넉백/스턴
        if (_inKnockback || Time.time < _stunUntil)
        {
            SetMoveSpeed(0f);
            return;
        }

        // 공격 중 이동잠금 적용/해제
        ApplyAttackMoveLock();

        // 타깃 확보
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
            if (target == null) { SafeStop(); SetMoveSpeed(0f); return; }
        }

        EnsureOnNavMesh();

        float dist = Vector3.Distance(transform.position, target.position);

        // 시야 밖이면 정지
        if (dist > sightRange)
        {
            SafeStop();
            SetMoveSpeed(0f);
            return;
        }

        bool inAttack = dist <= attackRange;

        // 잠금 중이면 추격/경로 갱신 금지
        if (IsMoveLocked()) { SetMoveSpeed(0f); return; }

        if (!inAttack)
        {
            // 추격
            if (agent.isStopped) SafeResume();
            if (Time.time >= _nextPathTime)
            {
                _nextPathTime = Time.time + repathInterval;
                SafeSetDestination(target.position);
            }
            SetMoveSpeed(AgentReady() ? agent.velocity.magnitude : 0f);
        }
        else
        {
            // 공격
            SafeStop();
            SetMoveSpeed(0f);

            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + attackCooldown;

                // 타깃을 향해 회전(간단 보정)
                FaceTarget(target.position);

                // ★ 공격 트리거
                if (anim) anim.SetTrigger(attackTrigger);

                // ★ 이동 잠금 시작(모션 길이에 맞춰 튜닝)
                StartMoveLock(attackLockDuration);

                // 실제 데미지는 애니메이션 이벤트(타격 프레임)에서 AnimEvent_AttackHit() 호출
            }
        }
    }

    // === 이동 잠금 로직 ===
    void StartMoveLock(float duration)
    {
        _unlockMoveAt = Time.time + Mathf.Max(0.05f, duration);
        // 즉시 잠금 반영
        if (agent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    bool IsMoveLocked() => Time.time < _unlockMoveAt;

    void ApplyAttackMoveLock()
    {
        if (IsMoveLocked())
        {
            // 잠금 유지
            if (agent)
            {
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.velocity = Vector3.zero;
            }
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // 잠금 해제
            if (agent)
            {
                if (agent.updatePosition == false) agent.updatePosition = true;
                if (agent.updateRotation == false) agent.updateRotation = true;
                // 추격 분기에서 SafeSetDestination가 다시 경로를 깔아줄 것
            }
        }
    }

    // ===== 애니메이션 이벤트: 타격 타이밍 =====
    public void AnimEvent_AttackHit()
    {
        if (_isDead || target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange + 0.2f) return;

        var dmg = target.GetComponentInChildren<IDamageable>();
        if (dmg != null)
        {
            Vector3 hitDir = (target.position - transform.position).normalized;
            dmg.TakeDamage(damage, hitDir);
        }
    }

    // ===== 넉백(외부 호출용) =====
    public void ApplyKnockback(Vector3 direction, float power) =>
        ApplyKnockback(direction, power, defaultKnockbackTime);

    public void ApplyKnockback(Vector3 direction, float power, float duration)
    {
        if (_isDead) return;
        direction.y = 0f;
        StartCoroutine(KnockbackRoutine(direction.normalized, power, duration));
    }

    // ===== 사망 전환 =====
    public void SetDeadState()
    {
        if (_isDead) return;
        _isDead = true;

        SafeStop();
        if (agent) agent.enabled = false;

        if (_colliders != null)
            foreach (var c in _colliders) if (c) c.enabled = false;

        if (anim)
        {
            anim.SetBool("Dead", true);
            SetMoveSpeed(0f);
        }

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (despawnAfter > 0f) Destroy(gameObject, despawnAfter);
    }

    // ===== 코루틴: 넉백 =====
    IEnumerator KnockbackRoutine(Vector3 dir, float power, float time)
    {
        _inKnockback = true;
        _stunUntil = Time.time + time;

        bool agentWasEnabled = AgentReady();
        if (agentWasEnabled) agent.enabled = false;

        if (rb)
        {
            bool prevKinematic = rb.isKinematic;
            rb.isKinematic = false;
            rb.drag = knockbackDrag;

            rb.velocity = Vector3.zero;
            rb.AddForce(dir * power, ForceMode.VelocityChange);

            float t = 0f;
            while (t < time) { t += Time.deltaTime; yield return null; }

            rb.velocity = Vector3.zero;
            rb.isKinematic = prevKinematic;
        }
        else
        {
            float t = 0f;
            Vector3 start = transform.position;
            Vector3 end = start + dir * power * 0.1f;
            while (t < time)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(0f, 1f, t / time);
                transform.position = Vector3.Lerp(start, end, a);
                yield return null;
            }
        }

        if (agent)
        {
            agent.enabled = true;
            EnsureOnNavMesh();
            agent.isStopped = false;
        }

        _inKnockback = false;
    }

    // ===== NavMesh/이동 유틸 =====
    bool AgentReady() => agent != null && agent.enabled && agent.isOnNavMesh;

    void SafeStop()
    {
        if (!AgentReady()) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    void SafeResume()
    {
        if (AgentReady()) agent.isStopped = false;
    }

    bool SafeSetDestination(Vector3 pos)
    {
        if (!AgentReady()) return false;
        if (agent.isStopped) agent.isStopped = false; // 공격 후 자동 해제
        if (agent.hasPath) agent.ResetPath();
        return agent.SetDestination(pos);
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

    void SetMoveSpeed(float v)
    {
        if (anim) anim.SetFloat("Speed", v);
    }

    void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
