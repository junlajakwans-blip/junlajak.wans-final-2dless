using UnityEngine;

public interface IAttackable
{
    void Attack();
    void ChargeAttack(float power);
    void RangeAttack(Transform target);
    void ApplyDamage(IDamageable target, int amount);
}
