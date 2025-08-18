using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Dead }

    [Header("Target")]
    public string targetTag = "Player";     // 쫓을 태그
    public LayerMask targetMask;            // 공격 판정 대상 레이어
    public Transform target;                // 비워두면 태그로 자동탐색

    [Header("Enemy Type (1~5)")]
    [Range(1, 5)] public int enemyType = 1;

    [Header("Vision / Move")]
    [SerializeField] float visionRange = 10f;
    [SerializeField] float patrolDistance = 6f;
    float patrolSpeed, chaseSpeed;

    [Header("Attack")]
    [SerializeField] int attackDamage = 10;
    [SerializeField] float attackRange = 2.0f;       // 판정 반경
    [SerializeField] float attackCooldown = 1.2f;    // 공격 쿨타임
    [SerializeField] float attackDelay = 0.15f;      // 애니 없이 사용할 타격 딜레이
    [SerializeField] bool useAnimationEvent = false; // ✅ 애니메이션 이벤트 없이도 동작
    [SerializeField] Transform attackPoint;          // 비워두면 전방 1m

    [Header("Animator Params")]
    public string speedFloat = "Speed";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";

    Rigidbody rb;
    Animator anim;
    State state = State.Patrol;

    Vector3 spawnPos, patrolRight;
    int patrolDir = 1;
    float atkTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        ApplyEnemyType();

        spawnPos = transform.position;
        patrolRight = spawnPos + Vector3.right * patrolDistance;

        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }
    }

    void Update()
    {
        if (state == State.Dead) return;
        if (atkTimer > 0f) atkTimer -= Time.deltaTime;

        // 이동 속도를 애니메이터에 전달 (Idle/Run 전환용)
        if (anim)
        {
            float planar = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
            anim.SetFloat(speedFloat, planar);
        }

        switch (state)
        {
            case State.Idle:
            case State.Patrol:
                LookForTarget();
                Patrol();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                rb.velocity = Vector3.zero; // 공격 중 이동 정지
                break;
        }

        // 타겟 바라보기(수평)
        if (target)
        {
            var look = target.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(look),
                    Time.deltaTime * 10f
                );
        }
    }

    void LookForTarget()
    {
        if (!target || Vector3.Distance(transform.position, target.position) > visionRange)
            target = FindClosestByTag(targetTag);

        if (!target) { state = State.Patrol; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        state = (dist <= visionRange) ? State.Chase : State.Patrol;
    }

    void Patrol()
    {
        Vector3 goal = (patrolDir > 0) ? patrolRight : spawnPos;
        MoveTowards(goal, patrolSpeed);
        if (Vector3.Distance(transform.position, goal) < 0.3f)
            patrolDir *= -1;
    }

    void Chase()
    {
        if (!target) { state = State.Patrol; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > visionRange * 1.2f) { state = State.Patrol; return; }

        // 사거리 + 쿨타임 충족 시 공격 시작
        if (dist <= attackRange && atkTimer <= 0f)
        {
            StartAttack();
            return;
        }

        MoveTowards(target.position, chaseSpeed);
    }

    void MoveTowards(Vector3 goal, float speed)
    {
        Vector3 dir = (goal - transform.position).normalized;
        dir.y = 0;
        rb.velocity = dir * speed + Vector3.up * rb.velocity.y; // 중력 유지
    }

    // ───────── Attack core ─────────
    void StartAttack()
    {
        state = State.Attack;
        rb.velocity = Vector3.zero;
        atkTimer = attackCooldown;

        if (anim) anim.SetTrigger(attackTrigger);

        // 이벤트를 쓰지 않는 간단 모드: 딜레이 후 범위 판정
        if (!useAnimationEvent)
            StartCoroutine(AttackHitAfter(attackDelay));
        // useAnimationEvent == true면, 애니메이션 이벤트에서 AnimationAttackHit() 호출
    }

    IEnumerator AttackHitAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        DoMeleeHit();
        if (state != State.Dead) state = State.Chase;
    }

    // 애니메이션 이벤트용 (Clip 타임라인에서 호출)
    // Animation Event 이름: AnimationAttackHit
    public void AnimationAttackHit()
    {
        if (state == State.Dead) return;
        DoMeleeHit();
    }

    void DoMeleeHit()
    {
        // 판정 중심: attackPoint 지정 시 그 위치, 없으면 전방 1m
        Vector3 center = attackPoint
            ? attackPoint.position
            : (transform.position + transform.forward * 1.0f);

        // 반경 내 대상 레이어만 탐색
        Collider[] hits = Physics.OverlapSphere(center, attackRange, targetMask);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<IDamageable>(out var dmg))
            {
                Vector3 dirToTarget = (h.transform.position - transform.position).normalized;
                dmg.TakeDamage(GetAttackDamageByType(), dirToTarget);
            }
        }
    }
    // ───────────────────────────────

    // EnemyHealth에서 사망 시 호출
    public void SetDeadState()
    {
        state = State.Dead;
        rb.velocity = Vector3.zero;
        if (anim) anim.SetBool(deadBool, true);
    }

    void ApplyEnemyType()
    {
        switch (enemyType)
        {
            case 1: patrolSpeed = 1.5f; chaseSpeed = 2.5f; attackDamage = 5; break;
            case 2: patrolSpeed = 1.6f; chaseSpeed = 2.7f; attackDamage = 8; break;
            case 3: patrolSpeed = 1.7f; chaseSpeed = 3.0f; attackDamage = 12; break;
            case 4: patrolSpeed = 1.8f; chaseSpeed = 3.3f; attackDamage = 16; break;
            case 5: patrolSpeed = 2.0f; chaseSpeed = 3.6f; attackDamage = 22; break;
        }
    }

    int GetAttackDamageByType() => attackDamage;

    Transform FindClosestByTag(string tagName)
    {
        var objs = GameObject.FindGameObjectsWithTag(tagName);
        Transform best = null; float bestDist = Mathf.Infinity; Vector3 p = transform.position;
        foreach (var go in objs)
        {
            if (go == gameObject) continue;
            float d = Vector3.Distance(go.transform.position, p);
            if (d < bestDist) { bestDist = d; best = go.transform; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        // 시야
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // 공격 판정
        Gizmos.color = Color.red;
        Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRange);
    }
}
