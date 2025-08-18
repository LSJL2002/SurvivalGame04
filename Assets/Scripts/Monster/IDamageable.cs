using UnityEngine;

public interface IDamageable
{
    // 공격자가 있는 방향(hitDir)은 넉백 등에 사용
    void TakeDamage(int amount, Vector3 hitDir);
}
