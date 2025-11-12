using UnityEngine;
using System.Collections;

public class MamaMon : Enemy
{
    #region Fields
    [Header("MamaMon Settings")]
    [SerializeField] private int _noodleCount = 3;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _boilRange = 4f;
    [SerializeField] private int _boilDamage = 10;
    
    [Header("Projectile Attack")]
    [SerializeField] private GameObject _noodleProjectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private int _projectileDamage = 15;

    [Header("Behavior")]
    [SerializeField] private float _healChance = 0.1f; // 10% chance to heal on cooldown
    [SerializeField] private float _healCooldown = 8f;

    [SerializeField] private int _maxHealth = 100; // Set Max HP for MamaMon
    
    private float _nextAttackTime;
    private float _nextHealAttempt;
    #endregion

    #region Unity Lifecycle
    protected override void Update()
    {
        if (_isDisabled) return;

        // Find target if missing
        if (_target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null) _target = player.transform;
        }

        if (_target == null) return;

        // Check distance for attacks
        float distanceToPlayer = Vector2.Distance(transform.position, _target.position);

        if (distanceToPlayer <= _detectionRange)
        {
            // --- Attack Logic ---
            if (Time.time >= _nextAttackTime)
            {
                // Decide which attack to use
                if (distanceToPlayer <= _boilRange)
                {
                    // If player is close, use AOE BoilSplash
                    BoilSplash();
                }
                else
                {
                    // If player is far, throw noodles
                    Attack();
                }
                _nextAttackTime = Time.time + _attackCooldown;
            }

            // --- Heal Logic ---
            if (Time.time >= _nextHealAttempt)
            {
                // Check if HP is below max and roll for heal chance
                if (_health < _maxHealth && Random.value < _healChance)
                {
                    RecoverHP();
                }
                _nextHealAttempt = Time.time + _healCooldown;
            }
        }
    }
    #endregion

    #region Combat
    /// <summary>
    /// Base attack triggers the projectile throw.
    /// </summary>
    public override void Attack()
    {
        Debug.Log($"[{name}] throws boiling noodles!");
        StartCoroutine(ThrowNoodlesRoutine());
    }

    /// <summary>
    /// Throws noodles (projectiles) at the player.
    /// </summary>
    private IEnumerator ThrowNoodlesRoutine()
    {
        if (_noodleProjectilePrefab == null || _firePoint == null || _target == null)
            yield break;

        for (int i = 0; i < _noodleCount; i++)
        {
            var go = Instantiate(_noodleProjectilePrefab, _firePoint.position, Quaternion.identity);
            
            if (go.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
                rb.linearVelocity = aim * _projectileSpeed;
            }

            if (go.TryGetComponent<Projectile>(out var proj))
                proj.SetDamage(_projectileDamage);
            
            // Wait a very short time between each noodle throw
            yield return new WaitForSeconds(0.2f); 
        }
    }

    /// <summary>
    /// AOE damage attack for close-range defense.
    /// </summary>
    public void BoilSplash()
    {
        Debug.Log($"[{name}] creates boiling splash in {_boilRange}m radius!");
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _boilRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out var player))
            {
                player.TakeDamage(_boilDamage);
            }
        }
    }
    #endregion

    #region Utility / Healing
    /// <summary>
    /// Recovers MamaMon's HP by 10 points.
    /// </summary>
    public void RecoverHP()
    {
        base.Heal(10); // Call the base class's Heal method
        Debug.Log($"[{name}] slurps noodles to heal HP!");
    }
    #endregion

    #region Death/Drop
    /// <summary>
    /// Called when this enemy dies. Implements item drop logic.
    /// </summary>
    public override void Die()
    {
        base.Die(); 
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 35% chance
            if (roll < 0.35f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[MamaMon] Dropped: Coin ({roll:F2})");
            }
            // Drop GreenTea with 10% chance 
            else if (roll < 0.45f)
            {
                spawner.DropCollectible(CollectibleType.GreenTea, transform.position);
                Debug.Log($"[MamaMon] Dropped: GreenTea ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[MamaMon] Cannot drop items.");
        }
    }
    #endregion
}