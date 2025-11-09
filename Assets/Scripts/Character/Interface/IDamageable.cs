using UnityEngine;

/// <summary>
/// Defines entities that can receive and recover health-based damage.
/// Implemented by <see cref="Enemy"/> and <see cref="Player"/>.
/// </summary>
public interface IDamageable
{
    /// <summary>Applies incoming damage to this entity.</summary>
    void TakeDamage(int amount);

    /// <summary>Heals this entity by the specified amount.</summary>
    void Heal(int amount);
}
