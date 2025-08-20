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
    public float destRefreshTime = 0.15f; // 목적지 갱신 최소 주기
    public float destRefreshDist = 0.3f;  // 목적지 변화 최소 거리

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1.2f;
    public Transform attackPoint;       // 없으면 정면 1m
    public float attackRadius = 0.8f;   // OverlapSphere 판정
    public LayerMask playerMask;        // Player 레이어

    [Header("Animator (optional)")]
    public string movingBool = "IsMoving"; // 있으면 쓰고, 없어도 무방
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";       // bool
    // Animator에는 Speed(float) 파라미터가 있어야 함 (Idle↔Run 전이용)

    [Header("Knockback (physics)")]
    public float knockbackForce = 6f;       // 수평 힘
    public float knockbackUpForce = 1.5f;   // 위로 약간
    public float knockbackTime = 0.12f;     // 넉백 동안 추격 입력 잠시 중지

    // ── internals ────────────────────────────────────────
    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;
    bool isDead;
    bool canAttack = true;
    bool isKnockback;
    float destTimer;
    Vector3 lastDest;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        if (!agent) agent = gameObject.AddComponent<NavMeshAgent>();

        // Agent는 경로만 계산. 이동은 우리가 Rigidbody로 한다.
        agent.updateRotation = false;
        agent.updatePosition = false;        // ★ 중요
        agent.autoBraking = false;        // 추격 중 감속 OFF
        agent.stoppingDistance = Mathf.Max(attackRange * 0.9f, 0.2f);
        agent.speed = runSpeed;

        // 물리 이동 사용(요청대로 키네마틱 OFF)
        if (rb)
        {
            rb.isKinematic = false;          // ★ 중요
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 넘어지는 것 방지
        }
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        // 시작 위치 NavMesh로 스냅
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        agent.Warp(transform.position); // 초기 동기화
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        destTimer += Time.deltaTime;

        // ── 애니메이션 파라미터 갱신
        if (anim)
        {
            // 이동 중 판단(경로 진행 중이거나 실제 속도가 조금이라도 있으면 true)
            bool moving =
                (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f)
                || rb.velocity.sqrMagnitude > 0.05f;

            if (!string.IsNullOrEmpty(movingBool)) anim.SetBool(movingBool, moving);

            // ★ Speed(float) 갱신 → Animator 전이 조건: Idle→Run(>0.05), Run→Idle(<0.01) 추천
            anim.SetFloat("Speed", rb.velocity.magnitude, 0.1f, Time.deltaTime);
        }

        // ── 공격/추격 의사결정
        if (dist <= attackRange && IsPlayerInFOVAndVisible())
        {
            if (canAttack) StartCoroutine(Co_Attack());
        }
        else
        {
            if (!isKnockback)
            {
                TrySetDestination(player.position);

                // NavMesh 경로 기반 이동(물리)
                Vector3 desired = agent.desiredVelocity; desired.y = 0f;

                if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                    desired = Vector3.zero;

                Vector3 step = desired.normalized * agent.speed * Time.deltaTime;
                rb.MovePosition(rb.position + step);
            }
        }

        FaceTowards();                 // 수평 회전
        agent.nextPosition = rb.position; // 매 프레임 Agent와 동기화
    }

    // 목적지 과도 갱신 방지 + NavMesh 위로 스냅
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
        Vector3 dir = rb.velocity.sqrMagnitude > 0.01f ? rb.velocity : (player.position - transform.position);
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

        // 벽/지형에 막히면 false
        if (Physics.Raycast(eye, dir.normalized, out var hit, dir.magnitude + 0.05f, visionBlockMask))
            return hit.transform == player;

        return true;
    }

    IEnumerator Co_Attack()
    {
        canAttack = false;
        if (anim && !string.IsNullOrEmpty(attackTrigger)) anim.SetTrigger(attackTrigger);

        // 타격 타이밍 맞춰 대기(필요 시 조정)
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
        canAttack = true;
    }

    // ── 넉백 (모든 무기 공통) ─────────────────────────────
    public void ApplyKnockback(Vector3 dir)
    {
        if (isDead || rb == null) return;
        StopCoroutine(nameof(Co_Knockback));
        StartCoroutine(Co_Knockback(dir));
    }

    IEnumerator Co_Knockback(Vector3 dir)
    {
        isKnockback = true;
        rb.velocity = Vector3.zero;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir = dir.normalized;

        Vector3 force = dir * knockbackForce + Vector3.up * knockbackUpForce;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackTime);

        isKnockback = false;
        agent.Warp(rb.position); // NavMesh와 재동기화
    }

    // EnemyHealth에서 호출
    public void SetDeadState()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        if (anim && !string.IsNullOrEmpty(deadBool)) anim.SetBool(deadBool, true);

        if (rb) rb.velocity = Vector3.zero;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
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
