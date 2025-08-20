using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    Transform player;

    [Header("Senses / Ranges")]
    public float detectionRange = 12f;     // 감지 거리
    public float fieldOfView = 120f;       // 시야각
    public LayerMask visionBlockMask = ~0; // 시야 가림(벽/지형)

    [Header("Move / Agent")]
    public float walkSpeed = 2.0f;         // 배회 속도(선택)
    public float runSpeed = 3.6f;         // 추격 속도
    public float attackRange = 1.8f;       // 공격 사거리(Stopping Distance와 맞추기)
    public float destRefreshTime = 0.15f;  // 목적지 갱신 최소 주기
    public float destRefreshDist = 0.3f;   // 목적지 변화 최소 거리

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1.2f;    // 공격 쿨타임
    public Transform attackPoint;          // 없으면 정면 1m
    public float attackRadius = 0.8f;      // 코드 판정용(OverlapSphere)
    public LayerMask playerMask;           // Player 레이어

    [Header("Animator (optional)")]
    public string movingBool = "IsMoving";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";

    // ── internals ────────────────────────────────────────
    NavMeshAgent agent;
    Animator anim;
    Rigidbody rb;             // 물리 충돌만(이동은 Agent)
    bool isDead;
    bool canAttack = true;
    float destTimer;
    Vector3 lastDest;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        // Agent 기본 세팅
        if (!agent) agent = gameObject.AddComponent<NavMeshAgent>();
        agent.updateRotation = false;                               // 회전은 코드에서(수평만)
        agent.autoBraking = true;
        agent.stoppingDistance = Mathf.Max(attackRange * 0.9f, 0.2f);
        agent.speed = runSpeed;

        // 물리 이동은 끄고 충돌만
        if (rb) { rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate; }
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        // 시작 위치가 NavMesh 밖이면 근처로 스냅
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        destTimer += Time.deltaTime;

        // 애니메이션 상태
        if (anim)
        {
            bool moving = agent.velocity.sqrMagnitude > 0.05f;
            if (!string.IsNullOrEmpty(movingBool)) anim.SetBool(movingBool, moving);
            // 속도 기반 보정(선택)
            // anim.speed = Mathf.Lerp(anim.speed, Mathf.Clamp(agent.velocity.magnitude / walkSpeed, 0.8f, 1.2f), 0.2f);
        }

        // 공격 사거리 내: 정지 후 공격
        if (dist <= attackRange && IsPlayerInFOVAndVisible())
        {
            agent.isStopped = true;
            if (canAttack) StartCoroutine(Co_Attack());
        }
        else
        {
            // 추격
            agent.isStopped = false;
            TrySetDestination(player.position);
        }

        FaceTowards(); // 수평 회전만
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
        Vector3 dir = agent.velocity.sqrMagnitude > 0.01f
            ? agent.velocity
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

        // 모션 타이밍(필요시 조정)
        yield return new WaitForSeconds(0.35f);

        if (!isDead && player != null)
        {
            // 거리/원형 판정 중 하나라도 맞으면 타격
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

    // EnemyHealth가 호출하는 메서드(죽음 상태 전환)
    public void SetDeadState()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        if (anim && !string.IsNullOrEmpty(deadBool)) anim.SetBool(deadBool, true);
        if (agent) { agent.isStopped = true; agent.velocity = Vector3.zero; }

        // 충돌 차단(선택)
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    // 필요 시 외부에서 직접 사망 처리
    public void Die()
    {
        SetDeadState();
        Destroy(gameObject, 2f);
    }

    // 디버그
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRadius);
    }
}
