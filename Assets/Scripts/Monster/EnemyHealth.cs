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

    // 프로젝트의 IDamageable 시그니처에 맞춤
    public void TakeDamage(int amount, Vector3 hitDir)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;

        if (rend) StartCoroutine(FlashRed());

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
        // Enemy AI 쪽에 '죽음 상태' 알림 (이동/공격/충돌 정지 등)
        if (enemyAI) enemyAI.SetDeadState();

        // 이 오브젝트의 충돌 비활성화
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 죽음 애니
        if (anim && !string.IsNullOrEmpty(deadBool))
            anim.SetBool(deadBool, true);

        // 물리 멈춤
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 일정 시간 후 삭제
        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}
