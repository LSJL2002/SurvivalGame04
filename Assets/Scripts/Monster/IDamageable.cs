using UnityEngine;

public interface IDamageable
{
    // �����ڰ� �ִ� ����(hitDir)�� �˹� � ���
    void TakeDamage(int amount, Vector3 hitDir);
}
