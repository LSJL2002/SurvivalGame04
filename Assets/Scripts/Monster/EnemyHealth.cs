using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int maxHP = 50;
    public bool destroyOnDeath = true;
    public float destroyDelay = 3f;

    [Header("Hit Reaction")]
    public float knockbackForce = 3f;
    public float flashDuration = 0.12f;

    [Header("Animation Params (optional)")]
    public string hurtTrigger = "Hurt";
    public string deadBool = "Dead";

    private int currentHP;
    private Rigidbody rb;
    private Animator anim;
    private Renderer rend;              // 자식까지 포함
    private Color originalColor;
    private Enemy enemyAI;              // 상태 제어용(선택)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        rend = GetComponentInChildren<Renderer>();
        enemyAI = GetComponent<Enemy>();

        if (rend) originalColor = rend.material.color;
    }

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount, Vector3 hitDir)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        // 피격 플래시(빨간색 → 원래색)
        if (rend) StartCoroutine(FlashRed());

        // 넉백
        if (rb)
        {
            Vector3 force = hitDir.normalized * knockbackForce + Vector3.up * 0.3f;
            rb.AddForce(force, ForceMode.Impulse);
        }

        // 애니메이션
        if (anim && !string.IsNullOrEmpty(hurtTrigger))
            anim.SetTrigger(hurtTrigger);

        if (currentHP <= 0)
            Die();
    }

    private IEnumerator FlashRed()
    {
        var mat = rend.material;         // 인스턴스화된 머티리얼
        mat.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        mat.color = originalColor;
    }

    private void Die()
    {
        // AI 정지
        if (enemyAI) enemyAI.SetDeadState();

        // 콜라이더 막기
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 애니메이션
        if (anim && !string.IsNullOrEmpty(deadBool))
            anim.SetBool(deadBool, true);

        // 리지드바디 정지
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}
