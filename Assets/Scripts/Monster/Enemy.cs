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
    public float attackCooldown = 1.2f;
    public Transform attackPoint;       // 없으면 정면 1m
    public float attackRadius = 0.8f;   // OverlapSphere 판정
    public LayerMask playerMask;        // Player 레이어

    [Header("Animator (optional)")]
    public string movingBool = "IsMoving";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";

    [Header("Knockback (physics)")]
    public float knockbackForce = 6f;       // 수평 힘
    public float knockbackUpForce = 1.5f;   // 위로 살짝
    public float knockbackTime = 0.12f;     // 이 시간 동안 이동 입력 무시

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

        // 에이전트는 "경로 계산만" 담당하게 만든다.
        agent.updateRotation = false;
        agent.updatePosition = false;          // ★ 물리 이동을 직접 할 것이므로 false
        agent.autoBraking = false;          // 추격 중 감속 OFF
        agent.stoppingDistance = Mathf.Max(attackRange * 0.9f, 0.2f);
        agent.speed = runSpeed;

        // 키네마틱 끈 상태를 전제로 한다.
        if (rb)
        {
            rb.isKinematic = false;            // ★ 요청사항: 키네마틱 OFF
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 쓰러짐 방지
        }
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        // 시작 위치 NavMesh 스냅
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        // 에이전트-리짓바디 위치 초기 동기화
        agent.Warp(transform.position);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        destTimer += Time.deltaTime;

        // 애니메이션 파라미터 갱신
        if (anim)
        {
            // 이동 판단: 경로가 있고 아직 멈출 거리보다 멀거나, 현재 속도가 조금이라도 있으면 이동
            bool moving =
                (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f)
                || rb.velocity.sqrMagnitude > 0.05f;

            if (!string.IsNullOrEmpty(movingBool)) anim.SetBool(movingBool, moving);
            anim.SetFloat("Speed", rb.velocity.magnitude, 0.1f, Time.deltaTime);
        }

        // 이동/공격 의사결정
        if (dist <= attackRange && IsPlayerInFOVAndVisible())
        {
            // 공격: 에이전트 정지(경로 계산 중지 X), 이동 입력 무시
            if (canAttack) StartCoroutine(Co_Attack());
            // 공격 중에도 agent.nextPosition은 계속 동기화
        }
        else
        {
            // 추격 (넉백 중이 아닐 때만)
            if (!isKnockback)
            {
                TrySetDestination(player.position);

                // agent.desiredVelocity를 이용해 물리 이동(Rigidbody) 수행
                Vector3 desired = agent.desiredVelocity; desired.y = 0f;

                // 정지 거리 안이라면 속도 0
                if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                    desired = Vector3.zero;

                // Move (가속/감속을 rb.drag로 조정해도 되고, 바로 MovePosition 해도 됨)
                Vector3 step = desired.normalized * agent.speed * Time.deltaTime;
                rb.MovePosition(rb.position + step);
            }
        }

        // 수평 회전
        FaceTowards();

        // 매 프레임 에이전트 위치를 리짓바디 위치로 동기화
        agent.nextPosition = rb.position;
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
        Vector3 dir = rb.velocity.sqrMagnitude > 0.01f
            ? rb.velocity
            : (player.position - transform.position);
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
        canAttack = false;
        if (anim && !string.IsNullOrEmpty(attackTrigger)) anim.SetTrigger(attackTrigger);

        // 타격 타이밍
        yield return new WaitForSeconds(0.35f);

        if (!isDead && player != null)
        {
            bool inRange = Vector3.Distance(transform.position, player.position) <= attackRange + 0.05f;

            bool overlapHit = false;
            Vector3 center = attackPoint ? attackPoint.position
                                         : transform.position + transform.forward * 1.0f;

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

    // ── Knockback (물리) ─────────────────────────────────
    public void ApplyKnockback(Vector3 dir)
    {
        if (isDead) return;
        StopCoroutine(nameof(Co_Knockback));
        StartCoroutine(Co_Knockback(dir));
    }

    IEnumerator Co_Knockback(Vector3 dir)
    {
        isKnockback = true;

        // 추격 입력 잠시 중단
        // (agent는 경로만 유지하고, 위치는 우리가 계속 nextPosition으로 동기화)
        rb.velocity = Vector3.zero;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir = dir.normalized;

        // 물리 힘으로 밀기
        Vector3 force = dir * knockbackForce + Vector3.up * knockbackUpForce;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackTime);

        isKnockback = false;

        // 넉백 중 NavMesh에서 조금 벗어났을 수 있으므로 에이전트 위치 재동기화
        agent.Warp(rb.position);
    }

    // EnemyHealth가 호출하는 메서드(죽음 상태 전환)
    public void SetDeadState()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        if (anim && !string.IsNullOrEmpty(deadBool)) anim.SetBool(deadBool, true);

        // 물리/충돌 정지
        if (rb) { rb.velocity = Vector3.zero; }
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    public void Die()
    {
        SetDeadState();
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRadius);
    }
}
