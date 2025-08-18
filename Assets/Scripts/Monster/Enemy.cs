using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Dead }

    [Header("Target")]
    public string targetTag = "Player";     // ���� �±�
    public LayerMask targetMask;            // ���� ���� ��� ���̾�
    public Transform target;                // ����θ� �±׷� �ڵ�Ž��

    [Header("Enemy Type (1~5)")]
    [Range(1, 5)] public int enemyType = 1;

    [Header("Vision / Move")]
    [SerializeField] float visionRange = 10f;
    [SerializeField] float patrolDistance = 6f;
    float patrolSpeed, chaseSpeed;

    [Header("Attack")]
    [SerializeField] int attackDamage = 10;
    [SerializeField] float attackRange = 2.0f;       // ���� �ݰ�
    [SerializeField] float attackCooldown = 1.2f;    // ���� ��Ÿ��
    [SerializeField] float attackDelay = 0.3f;       // �ִϸ��̼� Ÿ�̹�(�̺�Ʈ ���� �� ���)
    [SerializeField] bool useAnimationEvent = true;  // �ִϸ��̼� �̺�Ʈ�� Ÿ������
    [SerializeField] Transform attackPoint;          // ����θ� ���� 1m

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
                rb.velocity = Vector3.zero; // ���� �� �̵� ����
                break;
        }

        // Ÿ�� �ٶ󺸱�(����)
        if (target)
        {
            var look = target.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 10f);
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
        if (Vector3.Distance(transform.position, goal) < 0.3f) patrolDir *= -1;
    }

    void Chase()
    {
        if (!target) { state = State.Patrol; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > visionRange * 1.2f) { state = State.Patrol; return; }

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
        rb.velocity = dir * speed + Vector3.up * rb.velocity.y; // �߷� ����
    }

    // ������������������ Attack core ������������������
    void StartAttack()
    {
        state = State.Attack;
        rb.velocity = Vector3.zero;
        atkTimer = attackCooldown;

        if (anim) anim.SetTrigger(attackTrigger);

        if (!useAnimationEvent)
            StartCoroutine(AttackHitAfter(attackDelay));
        // useAnimationEvent=true��, �ִϸ��̼� �̺�Ʈ���� AnimationAttackHit() ȣ��
    }

    IEnumerator AttackHitAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        DoMeleeHit();
        if (state != State.Dead) state = State.Chase;
    }

    // �ִϸ��̼� �̺�Ʈ�� (Clip Ÿ�Ӷ��ο��� ȣ��)
    // Animation Event �̸�: AnimationAttackHit
    public void AnimationAttackHit()
    {
        if (state == State.Dead) return;
        DoMeleeHit();
    }

    void DoMeleeHit()
    {
        Vector3 center =
            attackPoint ? attackPoint.position : (transform.position + transform.forward * 1.0f);

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
    // ��������������������������������������������������������������

    // EnemyHealth���� ��� �� ȣ��
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

    int GetAttackDamageByType()
    {
        return attackDamage; // �� ApplyEnemyType���� ���õ�
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
