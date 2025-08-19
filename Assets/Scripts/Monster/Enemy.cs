using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(-100)] // EnemyHealth.Start()보다 먼저 동작(HP 세팅 선행)
public class Enemy : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Dead }

    [Header("Target")]
    public string targetTag = "Player";
    public LayerMask targetMask;         // 공격 판정할 레이어(예: Player)
    public Transform target;             // 비워두면 태그로 자동탐색

    [Header("Enemy Type (1~5)")]
    [Range(1, 5)] public int enemyType = 1;
    [SerializeField] int[] hpByType = { 40, 60, 90, 120, 180 };

    [Header("Vision / Move")]
    [SerializeField] float visionRange = 10f;  // 시야 거리
    [SerializeField] float chaseSpeed = 3.0f;  // 타입별로 덮어씌움
    [SerializeField] float stopDistance = 0.2f;

    [Header("Attack")]
    [SerializeField] int attackDamage = 10;    // 타입별로 덮어씌움
    [SerializeField] float attackRange = 2.0f;
    [SerializeField] float attackCooldown = 1.2f;
    [SerializeField] float attackDelay = 0.3f;       // 애니 이벤트 안 쓸 때 타이밍
    [SerializeField] bool useAnimationEvent = true;  // 애니 이벤트로 타격할지
    [SerializeField] Transform attackPoint;          // 비워두면 전방 1m

    [Header("Animator Params")]
    public string speedFloat = "Speed";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";

    Rigidbody rb;
    Animator anim;
    EnemyHealth hp;
    State state = State.Idle;
    float atkTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        hp = GetComponent<EnemyHealth>();
        ApplyEnemyType(); // 속도/공격력/HP 세팅
    }

    void Start()
    {
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

        // 애니 속도 파라미터(댐핑)
        if (anim)
        {
            float planar = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
            anim.SetFloat(speedFloat, planar, 0.1f, Time.deltaTime);
        }

        switch (state)
        {
            case State.Idle:
                LookForTarget();
                // 제자리 대기
                rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                rb.velocity = Vector3.zero;
                break;
        }

        // 타겟 바라보기(수평 회전)
        if (target)
        {
            var look = target.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 10f);
        }
    }

    void LookForTarget()
    {
        if (!target || Vector3.Distance(transform.position, target.position) > visionRange)
            target = FindClosestByTag(targetTag);

        if (!target) { state = State.Idle; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        state = (dist <= visionRange) ? State.Chase : State.Idle;
    }

    void Chase()
    {
        if (!target) { state = State.Idle; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > visionRange * 1.25f) { state = State.Idle; return; }

        if (dist <= attackRange && atkTimer <= 0f)
        {
            StartAttack();
            return;
        }

        MoveTowards(target.position, chaseSpeed);
    }

    void MoveTowards(Vector3 goal, float speed)
    {
        Vector3 to = goal - transform.position; to.y = 0f;
        if (to.magnitude <= stopDistance)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        Vector3 dir = to.normalized;
        rb.velocity = dir * speed + Vector3.up * rb.velocity.y; // 중력 유지
    }

    // ───── Attack core ─────
    void StartAttack()
    {
        state = State.Attack;
        rb.velocity = Vector3.zero;
        atkTimer = attackCooldown;

        if (anim) anim.SetTrigger(attackTrigger);

        if (!useAnimationEvent)
            StartCoroutine(AttackHitAfter(attackDelay));
        // useAnimationEvent=true면 애니메이션 이벤트에서 AnimationAttackHit() 호출
    }

    IEnumerator AttackHitAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        DoMeleeHit();
        if (state != State.Dead) state = State.Chase;
    }

    // 애니메이션 이벤트용 (클립 타임라인에서 호출)
    public void AnimationAttackHit()
    {
        if (state == State.Dead) return;
        DoMeleeHit();
    }

    void DoMeleeHit()
    {
        Vector3 center = attackPoint
            ? attackPoint.position
            : (transform.position + transform.forward * 1.0f);

        Collider[] hits = Physics.OverlapSphere(center, attackRange, targetMask);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<IDamageable>(out var dmg))
            {
                Vector3 dirToTarget = (h.transform.position - transform.position).normalized;
                dmg.TakeDamage(attackDamage, dirToTarget);
            }
        }
    }
    // ───────────────────────

    // EnemyHealth에서 사망 시 호출
    public void SetDeadState()
    {
        state = State.Dead;
        rb.velocity = Vector3.zero;
        if (anim) anim.SetBool(deadBool, true);
    }

    void ApplyEnemyType()
    {
        // 타입별 스탯
        switch (enemyType)
        {
            case 1: chaseSpeed = 2.5f; attackDamage = 5; break;
            case 2: chaseSpeed = 2.7f; attackDamage = 8; break;
            case 3: chaseSpeed = 3.0f; attackDamage = 12; break;
            case 4: chaseSpeed = 3.3f; attackDamage = 16; break;
            case 5: chaseSpeed = 3.6f; attackDamage = 22; break;
        }

        if (hp)
        {
            int idx = Mathf.Clamp(enemyType - 1, 0, hpByType.Length - 1);
            hp.SetMaxHP(hpByType[idx], true); // HP도 타입에 맞춰 자동 세팅
        }
    }

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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Vector3 c = attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);
        Gizmos.DrawWireSphere(c, attackRange);
    }
}
