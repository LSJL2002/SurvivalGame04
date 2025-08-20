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
    public float walkSpeed = 2.0f;
    public float runSpeed = 3.6f;
    public float attackRange = 1.8f;
    public float destRefreshTime = 0.15f;
    public float destRefreshDist = 0.3f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1.2f;   // 다음 공격 가능까지 대기
    public Transform attackPoint;
    public float attackRadius = 0.8f;
    public LayerMask playerMask;

    [Header("Animator (optional)")]
    public string movingBool = "IsMoving";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";      // bool, Speed(float)는 파라미터에 존재해야 함

    [Header("Knockback (physics)")]
    public float knockbackForce = 6f;
    public float knockbackUpForce = 1.5f;
    public float knockbackTime = 0.12f;              // 넉백 유지 시간
    public bool ignoreKnockbackWhileAttacking = true;
    public float postAttackKnockbackGrace = 0.12f;   // 공격 종료 직후 넉백 무시 시간
    public float knockbackCooldown = 0.08f;          // 넉백 종료 직후 또 넉백 방지 쿨다운

    // ── internals ────────────────────────────────────────
    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;
    bool isDead;
    bool canAttack = true;
    bool isAttacking;          // 공격 잠금
    bool isKnockback;          // 넉백 잠금
    float destTimer;
    Vector3 lastDest;

    // 타임스탬프
    float lastAttackEndTime = -999f;
    float lastKnockbackTime = -999f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        if (!agent) agent = gameObject.AddComponent<NavMeshAgent>();

        // Agent는 경로만, 이동은 Rigidbody
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

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }
        agent.Warp(transform.position);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        destTimer += Time.deltaTime;

        bool actionLocked = isAttacking || isKnockback;
        float speedForAnim = 0f; // 애니메이터에 넣을 속도(의도 속도 기반)

        if (actionLocked)
        {
            // 공격 중에는 정지, 넉백 중엔 물리에 맡김
            if (isAttacking) rb.velocity = Vector3.zero;

            agent.isStopped = true;
            agent.ResetPath();
            agent.nextPosition = rb.position;
            FaceTowards();

            if (!isKnockback) speedForAnim = 0f; // 넉백 중엔 Speed를 강제로 0으로 만들지 않음
        }
        else
        {
            // 추격/이동
            if (dist > attackRange || !IsPlayerInFOVAndVisible())
            {
                TrySetDestination(player.position);

                // 의도 속도 사용
                Vector3 desired = agent.desiredVelocity;
                desired.y = 0f;

                // stoppingDistance 안이면 정지
                if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                    desired = Vector3.zero;

                // 실제 이동은 Rigidbody로
                Vector3 step = desired.normalized * agent.speed * Time.deltaTime;
                rb.MovePosition(rb.position + step);

                agent.isStopped = false;

                // 애니메이터용 속도(데드존 포함)
                speedForAnim = desired.magnitude;
            }

            // 공격 시도
            if (dist <= attackRange && IsPlayerInFOVAndVisible() && canAttack)
                StartCoroutine(Co_Attack());
        }

        // 애니 파라미터 갱신
        if (anim)
        {
            if (speedForAnim < 0.03f) speedForAnim = 0f; // 미세 떨림 제거
            anim.SetFloat("Speed", speedForAnim, 0.1f, Time.deltaTime);
            if (!string.IsNullOrEmpty(movingBool))
                anim.SetBool(movingBool, speedForAnim > 0f || isKnockback);
        }

        // 공통 동기화
        agent.nextPosition = rb.position;
    }

    void TrySetDestination(Vector3 target)
    {
        if (destTimer < destRefreshTime) return;

        Vector3 want = target;
        if (NavMesh.SamplePosition(target, out var hit, 1.0f, NavMesh.AllAreas))
            want = hit.position;

        if ((lastDest - want).sqrMagnitude < destRefreshDist * destRefreshDist) return;

        agent.speed = runSpeed;
        agent.SetDestination(want);
        lastDest = want;
        destTimer = 0f;
    }

    void FaceTowards()
    {
        Vector3 dir = (player ? (player.position - transform.position) : transform.forward);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        var want = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * 10f);
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
        // 공격 잠금 시작
        isAttacking = true;
        canAttack = false;

        rb.velocity = Vector3.zero; // 공격 모션 안정화
        agent.isStopped = true;
        agent.ResetPath();

        if (anim && !string.IsNullOrEmpty(attackTrigger)) anim.SetTrigger(attackTrigger);

        // 타격 타이밍까지 대기 (클립 타이밍에 맞춰 조절)
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

        // 공격 후딜
        yield return new WaitForSeconds(attackCooldown);

        // 공격 종료 시각 기록 + 잠금 해제
        lastAttackEndTime = Time.time;
        isAttacking = false;
        canAttack = true;
        agent.isStopped = false;
        agent.Warp(rb.position); // 재동기화
    }

    // ── 넉백(잠금 포함) ────────────────────────────────
    public void ApplyKnockback(Vector3 dir)
    {
        if (isDead || rb == null) return;

        // 1) 공격 중이면 무시
        if (ignoreKnockbackWhileAttacking && isAttacking) return;

        // 2) 공격 직후 그레이스 타임 동안 무시
        if (Time.time - lastAttackEndTime < postAttackKnockbackGrace) return;

        // 3) 이미 넉백 중이면 체인 차단(무시)
        if (isKnockback) return;

        // 4) 넉백 쿨다운(끝난 직후 연속 방지)
        if (Time.time - lastKnockbackTime < knockbackCooldown) return;

        // 기존 넉백을 덮어쓰지 않음(StopCoroutine X) → 체인 방지
        StartCoroutine(Co_Knockback(dir));
    }

    IEnumerator Co_Knockback(Vector3 dir)
    {
        isKnockback = true;
        lastKnockbackTime = Time.time;

        agent.isStopped = true;
        agent.ResetPath();

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir = dir.normalized;

        // 즉시 반응형 넉백
        Vector3 vel = dir * knockbackForce + Vector3.up * knockbackUpForce;
        rb.AddForce(vel, ForceMode.VelocityChange);

        yield return new WaitForSeconds(knockbackTime);

        isKnockback = false;
        agent.isStopped = false;
        agent.Warp(rb.position);
    }

    // EnemyHealth에서 호출
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
        agent.ResetPath();
        agent.isStopped = true;
    }

    public void Die()
    {
        SetDeadState();
        Destroy(gameObject, 2f);
    }

    // 디버그
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRadius);
    }
}
