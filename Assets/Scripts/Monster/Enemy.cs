using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Senses / Ranges")]
    public float detectionRange = 12f;
    public float fieldOfView = 120f;
    public LayerMask visionBlockMask = ~0;

    [Header("Move / Agent")]
    public float runSpeed = 3.6f;
    public float attackRange = 1.8f;
    public float destRefreshTime = 0.15f;
    public float destRefreshDist = 0.3f;

    [Header("Rotation")]
    public float turnSpeed = 12f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1.2f;
    public Transform attackPoint;
    public float attackRadius = 0.8f;
    public LayerMask playerMask;

    [Header("Animator params")]
    public string speedFloat = "Speed";   // float
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";      // bool

    [Header("Knockback (physics)")]
    public float knockbackForce = 6f;
    public float knockbackUpForce = 1.5f;
    public float knockbackTime = 0.12f;
    public bool ignoreKnockbackWhileAttacking = true;
    public float postAttackKnockbackGrace = 0.12f;
    public float knockbackCooldown = 0.08f;

    [Header("NavMesh Safety")]
    public float navSampleRadius = 2.0f;      // NavMesh.SamplePosition 반경
    public float navReacquireInterval = 0.2f; // NavMesh 재탐색 주기

    // internals
    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;
    bool isDead;
    bool canAttack = true;
    bool isAttacking;
    bool isKnockback;
    float destTimer;
    Vector3 lastDest;

    float lastAttackEndTime = -999f;
    float lastKnockbackTime = -999f;
    float lastNavCheck;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        if (!agent) agent = gameObject.AddComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updatePosition = false;
        agent.autoBraking = false;
        agent.stoppingDistance = Mathf.Max(attackRange * 0.9f, 0.2f);
        agent.speed = runSpeed;

        if (rb)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        // 최초 NavMesh 정착
        EnsureAgentOnNavMesh(forceSnap: true);
        SafeWarp(rb.position);
    }

    void Update()
    {
        if (isDead || player == null) return;

        // 매 프레임 NavMesh 상태 확인 & 필요시 복구
        if (!EnsureAgentOnNavMesh())
        {
            // NavMesh를 못 찾는 동안은 agent API를 쓰지 않는다
            if (anim) anim.SetFloat(speedFloat, 0f, 0.1f, Time.deltaTime);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        destTimer += Time.deltaTime;

        bool actionLocked = isAttacking || isKnockback;
        float speedForAnim = 0f;

        if (actionLocked)
        {
            if (isAttacking) rb.velocity = Vector3.zero;

            SafeStopAgent(true);            // agent.isStopped = true (안전)
            agent.ResetPath();
            agent.nextPosition = rb.position;

            FaceTowards();
            speedForAnim = 0f;
        }
        else
        {
            // 시야 밖이거나 사거리 밖이면 추격
            if (dist > attackRange || !IsPlayerInFOVAndVisible())
            {
                TrySetDestination(player.position);

                // 의도 속도
                Vector3 desired = agent.desiredVelocity; desired.y = 0f;

                // 거의 도달했다면 정지
                if (SafeRemainingDistance() <= agent.stoppingDistance + 0.05f)
                    desired = Vector3.zero;

                // 이동
                Vector3 step = desired.sqrMagnitude > 0.0001f
                    ? desired.normalized * agent.speed * Time.deltaTime
                    : Vector3.zero;

                rb.MovePosition(rb.position + step);
                SafeStopAgent(false);        // agent.isStopped = false (안전)

                // 회전
                if (desired.sqrMagnitude > 0.0001f) RotateTowards(desired);

                speedForAnim = desired.magnitude;
            }
            else
            {
                FaceTowards();
                if (IsPlayerInFOVAndVisible() && canAttack)
                    StartCoroutine(Co_Attack());
            }
        }

        // Animator
        if (anim)
        {
            if (speedForAnim < 0.03f) speedForAnim = 0f;
            anim.SetFloat(speedFloat, speedForAnim, 0.1f, Time.deltaTime);
        }

        // 동기화
        agent.nextPosition = rb.position;
    }

    // ---------- NavMesh 안전 유틸 ----------

    // true를 반환해야 agent API 호출해도 안전
    bool EnsureAgentOnNavMesh(bool forceSnap = false)
    {
        if (agent == null) return false;

        // 비활성이라면 활성화
        if (!agent.enabled) agent.enabled = true;

        // 이미 OnNavMesh면 OK
        if (agent.isOnNavMesh) return true;

        // 너무 자주 시도하지 않기
        if (!forceSnap && Time.time - lastNavCheck < navReacquireInterval) return false;
        lastNavCheck = Time.time;

        // 현재 위치 근방에서 NavMesh 스냅 시도
        if (NavMesh.SamplePosition(rb ? rb.position : transform.position, out var hit, navSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);           // 안전: 샘플 좌표로만 Warp
            return true;
        }

        return false; // 아직 못 올림
    }

    // Warp 하기 전 반드시 NavMesh.SamplePosition으로 검사
    void SafeWarp(Vector3 worldPos)
    {
        if (agent == null) return;
        if (NavMesh.SamplePosition(worldPos, out var hit, navSampleRadius, NavMesh.AllAreas))
            agent.Warp(hit.position);
        // 못 찾으면 다음 프레임 EnsureAgentOnNavMesh가 다시 시도
    }

    // agent.isStopped 세터 안전 래퍼 (NavMesh 위일 때만)
    void SafeStopAgent(bool stop)
    {
        if (agent && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = stop;
    }

    // remainingDistance 읽기 안전 래퍼
    float SafeRemainingDistance()
    {
        if (agent && agent.enabled && agent.isOnNavMesh) return agent.remainingDistance;
        return Mathf.Infinity;
    }

    // ---------- 나머지 로직 ----------

    void TrySetDestination(Vector3 target)
    {
        if (destTimer < destRefreshTime) return;
        if (!agent || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 want = target;
        if (NavMesh.SamplePosition(target, out var hit, 1.0f, NavMesh.AllAreas))
            want = hit.position;

        if ((lastDest - want).sqrMagnitude < destRefreshDist * destRefreshDist) return;

        agent.speed = runSpeed;
        agent.SetDestination(want);
        lastDest = want;
        destTimer = 0f;
    }

    void RotateTowards(Vector3 dir)
    {
        dir.y = 0f; if (dir.sqrMagnitude < 0.0001f) return;
        var want = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed);
    }

    void FaceTowards()
    {
        if (!player) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f; if (dir.sqrMagnitude < 0.0001f) return;
        var want = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed);
    }

    bool IsPlayerInFOVAndVisible()
    {
        Vector3 to = player.position - transform.position; to.y = 0f;
        if (Vector3.Angle(transform.forward, to) > fieldOfView * 0.5f) return false;

        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 tgt = player.position + Vector3.up * 1.2f;
        Vector3 dir = tgt - eye;
        if (Physics.Raycast(eye, dir.normalized, out var hit, dir.magnitude + 0.05f, visionBlockMask))
            return hit.transform == player;

        return true;
    }

    IEnumerator Co_Attack()
    {
        isAttacking = true;
        canAttack = false;

        rb.velocity = Vector3.zero;
        SafeStopAgent(true);
        agent.ResetPath();
        FaceTowards();

        if (anim && !string.IsNullOrEmpty(attackTrigger)) anim.SetTrigger(attackTrigger);

        yield return new WaitForSeconds(0.35f);

        if (!isDead && player != null)
        {
            bool inRange = Vector3.Distance(transform.position, player.position) <= attackRange + 0.05f;

            bool overlapHit = false;
            Vector3 center = attackPoint ? attackPoint.position
                                         : (transform.position + transform.forward * 1.0f);

            if (playerMask.value != 0)
            {
                var cols = Physics.OverlapSphere(center, attackRadius, playerMask);
                foreach (var c in cols) { if (c.transform == player) { overlapHit = true; break; } }
            }

            if (inRange || overlapHit)
            {
                var dmg = player.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Vector3 dir = (player.position - transform.position).normalized;
                    dmg.TakeDamage(attackDamage, dir);
                }
            }
        }

        yield return new WaitForSeconds(attackCooldown);

        lastAttackEndTime = Time.time;
        isAttacking = false;
        canAttack = true;

        // 넉백 등으로 NavMesh에서 벗어났을 수 있으니 복귀 시도
        EnsureAgentOnNavMesh(forceSnap: true);
        SafeStopAgent(false);
    }

    public void ApplyKnockback(Vector3 dir, float force, float duration)
    {
        if (isDead || rb == null) return;
        if (ignoreKnockbackWhileAttacking && isAttacking) return;
        if (Time.time - lastAttackEndTime < postAttackKnockbackGrace) return;
        if (isKnockback) return;
        if (Time.time - lastKnockbackTime < knockbackCooldown) return;

        StartCoroutine(Co_KnockbackCustom(dir, force, duration));
    }

    public void ApplyKnockback(Vector3 dir)
    {
        if (isDead || rb == null) return;
        if (ignoreKnockbackWhileAttacking && isAttacking) return;
        if (Time.time - lastAttackEndTime < postAttackKnockbackGrace) return;
        if (isKnockback) return;
        if (Time.time - lastKnockbackTime < knockbackCooldown) return;

        StartCoroutine(Co_Knockback(dir));
    }

    IEnumerator Co_KnockbackCustom(Vector3 dir, float force, float duration)
    {
        float pf = knockbackForce;
        float pt = knockbackTime;
        knockbackForce = force;
        knockbackTime = duration;
        yield return StartCoroutine(Co_Knockback(dir));
        knockbackForce = pf;
        knockbackTime = pt;
    }

    IEnumerator Co_Knockback(Vector3 dir)
    {
        isKnockback = true;
        lastKnockbackTime = Time.time;

        SafeStopAgent(true);
        agent.ResetPath();

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir = dir.normalized;

        Vector3 vel = dir * knockbackForce + Vector3.up * knockbackUpForce;
        rb.AddForce(vel, ForceMode.VelocityChange);

        yield return new WaitForSeconds(knockbackTime);

        isKnockback = false;

        // 넉백으로 NavMesh에서 벗어났을 수 있으므로 스냅 후 재시작
        EnsureAgentOnNavMesh(forceSnap: true);
        SafeWarp(rb.position);
        SafeStopAgent(false);
    }

    public void SetDeadState()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        isAttacking = false;
        isKnockback = false;

        if (anim && !string.IsNullOrEmpty(deadBool)) anim.SetBool(deadBool, true);

        rb.velocity = Vector3.zero;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        if (agent)
        {
            agent.ResetPath();
            SafeStopAgent(true);
        }
    }

    public void Die()
    {
        SetDeadState();
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRadius);
    }
}
