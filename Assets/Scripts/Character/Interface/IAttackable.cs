using UnityEngine;

/// <summary>
/// Defines attack behaviors for any entity capable of offensive actions.
/// Implemented by <see cref="Enemy"/> and possibly by future bosses or traps.
/// </summary>
public interface IAttackable
{
    /// <summary>Performs a direct attack action.</summary>
    void Attack();

    /// <summary>Performs a charged attack with specified power.</summary>
    void ChargeAttack(float power);

    /// <summary>Executes a ranged attack towards the given target.</summary>
    void RangeAttack(Transform target);

    /// <summary>Applies damage to the given target.</summary>
    void ApplyDamage(IDamageable target, int amount);
}
