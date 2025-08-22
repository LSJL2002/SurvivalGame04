using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [HideInInspector] public int maxHP = 50;
    public bool destroyOnDeath = true;
    public float destroyDelay = 3f;

    [Header("Hit Reaction")]
    public float flashDuration = 0.12f;

    [Header("Animation Params (optional)")]
    public string hurtTrigger = "Hurt";
    public string deadBool = "Dead";

    [Header("Knockback")]
    public float knockbackPower = 6f;
    public float knockbackTime = 0.20f;

    int currentHP;
    bool isDead;

    Rigidbody rb;
    Animator anim;
    Renderer rend;
    Color originalColor;
    Enemy enemyAI;

    void Awake()
    {
        TryGetComponent(out rb);
        anim = GetComponentInChildren<Animator>();
        rend = GetComponentInChildren<Renderer>();
        enemyAI = GetComponent<Enemy>();

        if (rend != null)
        {
            // material 인스턴스 한번만 잡아둠
            originalColor = rend.material.color;
        }
    }

    void Start()
    {
        currentHP = Mathf.Max(1, maxHP);
        isDead = false;
    }

    public void SetMaxHP(int newMax, bool fill = true)
    {
        maxHP = Mathf.Max(1, newMax);
        currentHP = fill ? maxHP : Mathf.Clamp(currentHP, 0, maxHP);
    }

    // IDamageable 호환: 방향 없는 경우(필요 시)
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    // 무기에서 호출
    public void TakeDamage(int amount, Vector3 hitDir)
    {
        if (isDead) return;

        int dmg = Mathf.Max(0, amount);
        if (dmg <= 0) return;

        currentHP -= dmg;

        if (rend != null) StartCoroutine(FlashRed());

        // 넉백
        if (enemyAI != null)
        {
            if (hitDir.sqrMagnitude > 0.0001f)
            {
                hitDir.y = 0f; // 수평 유지
                enemyAI.ApplyKnockback(hitDir.normalized, knockbackPower, knockbackTime);
            }
        }

        if (anim != null && !string.IsNullOrEmpty(hurtTrigger))
            anim.SetTrigger(hurtTrigger);

        if (currentHP <= 0)
            Die();
    }

    IEnumerator FlashRed()
    {
        // 머티리얼 색상 Flash
        var mat = rend.material;
        mat.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        mat.color = originalColor;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // AI 죽음 상태
        if (enemyAI != null) enemyAI.SetDeadState();

        // 충돌 비활성
        var cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        // 애니메이션 Dead 세팅
        if (anim != null && !string.IsNullOrEmpty(deadBool))
            anim.SetBool(deadBool, true);

        // 물리 정지
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // 사망 후 밀리지 않게
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    // 유틸(선택): 현재 HP 조회
    public int GetCurrentHP() => currentHP;

    // 유틸(선택): 즉시 체력 회복
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Clamp(currentHP + Mathf.Max(0, amount), 0, maxHP);
    }
}
