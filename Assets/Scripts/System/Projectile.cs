using UnityEngine;

/// <summary>
/// Handles the behavior, damage, and lifespan of a projectile (like the skewer).
/// Requires Rigidbody2D and Collider2D (set as trigger) on the Prefab.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private int _damageAmount;
    [SerializeField] private float _lifetime = 3f; // How long the projectile stays active

    private void Awake()
    {
        // Ensure Rigidbody is set to Kinematic to ignore gravity and external forces
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Start()
    {
        // Destroy the projectile after its lifetime (or return to pool)
        Destroy(gameObject, _lifetime);
    }

    /// <summary>
    /// Sets the damage value, called by the launching entity (MooPingMon).
    /// </summary>
    /// <param name="amount">The damage to inflict.</param>
    public void SetDamage(int amount)
    {
        _damageAmount = amount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the projectile hits the Player
        if (other.TryGetComponent<Player>(out var player))
        {
            // Apply damage using the stored value
            player.TakeDamage(_damageAmount); 

            // Destroy the projectile after hitting the target
            DestroyProjectile();
        }
        
        // Optional: Add logic for hitting walls/obstacles here
    }

    private void DestroyProjectile()
    {
        // Since MooPingMon uses Instantiate, we use Destroy for consistency.
        // For pooling, this should be: ProjectileSpawner.Instance.ReturnToPool(gameObject.name, gameObject);
        Destroy(gameObject); 
    }
}