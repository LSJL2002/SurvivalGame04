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
    public float knockbackForce = 3f;
    public float flashDuration = 0.12f;

    [Header("Animation Params (optional)")]
    public string hurtTrigger = "Hurt";
    public string deadBool = "Dead";

    int currentHP;
    Rigidbody rb;
    Animator anim;
    Renderer rend;
    Color originalColor;
    Enemy enemyAI;

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

    public void SetMaxHP(int newMax, bool fill = true)
    {
        maxHP = newMax;
        currentHP = fill ? newMax : Mathf.Min(currentHP, newMax);
    }

    // 무기에서 호출되는 데미지 처리
    public void TakeDamage(int amount, Vector3 hitDir)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        if (rend) StartCoroutine(FlashRed());

        // 🔥 Enemy.cs에 있는 Knockback 호출
        if (enemyAI)
        {
            enemyAI.ApplyKnockback(hitDir);   // ← 무기 종류 상관없이 넉백!
        }

        if (rb)
        {
            Vector3 force = hitDir.normalized * knockbackForce + Vector3.up * 0.3f;
            rb.AddForce(force, ForceMode.Impulse);
        }

        if (anim && !string.IsNullOrEmpty(hurtTrigger))
            anim.SetTrigger(hurtTrigger);

        if (currentHP <= 0)
            Die();
    }

    IEnumerator FlashRed()
    {
        var mat = rend.material;
        mat.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        mat.color = originalColor;
    }

    void Die()
    {
        if (enemyAI) enemyAI.SetDeadState();

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (anim && !string.IsNullOrEmpty(deadBool))
            anim.SetBool(deadBool, true);

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}
