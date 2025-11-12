using UnityEngine;
using System.Collections;

public class PeterMon : Enemy, IMoveable
{
    #region Fields
    [Header("PeterMon Settings")]
    [SerializeField] private float _hoverAmplitude = 0.25f;
    [SerializeField] private float _hoverSpeed = 2f;
    [SerializeField] private float _attackRange = 4.5f;
    [SerializeField] private float _attackCooldown = 2.5f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _projectileDamage = 10;
    [SerializeField] private float _projectileSpeed = 4f;

    private float _hoverOffsetY;
    private float _nextAttackTime;
    private Vector2 _direction = Vector2.left;
    private bool _isChasing = false; // Unused in stationary mon
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        _hoverOffsetY = transform.position.y;
    }

    protected override void Update()
    {
        if (_isDisabled) return;

        HoverMotion();
        if (_target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null) _target = player.transform;
        }
        if (_target != null && DetectPlayer(_target.position))
            AttackPlayer();
    }
    #endregion

    #region Movement
    // Hover behaviour (Visual motion only)
    private void HoverMotion()
    {
        float newY = _hoverOffsetY + Mathf.Sin(Time.time * _hoverSpeed) * _hoverAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // PeterMon is stationary and only hovers.
    public override void Move() { } 
    
    // Unused in stationary mon, kept for IMoveable interface compliance.
    public void ChasePlayer(Player player) { } 
    
    // Unused in stationary mon, kept for IMoveable interface compliance.
    public void Stop() { }
    
    // Unused in stationary mon, kept for IMoveable interface compliance.
    public void SetDirection(Vector2 direction) { }
    #endregion

    #region Combat
    private void AttackPlayer()
    {
        if (Time.time < _nextAttackTime) return;
        if (_target == null) return;
        
        if (Vector2.Distance(transform.position, _target.position) <= _attackRange)
        {
            ShootProjectile();
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }
    
    private void ShootProjectile()
    {
        if (_projectilePrefab == null || _firePoint == null || _target == null) return;
        
        // Instantiate the projectile (Should ideally use Pooling via Spawner)
        var proj = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);

        // Calculate direction and set velocity (Aiming at target)
        if (proj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
            rb.linearVelocity = aim * _projectileSpeed;
        }
        
        // Set damage value using the Projectile component
        if (proj.TryGetComponent<Projectile>(out var projectile))
            projectile.SetDamage(_projectileDamage);
            
        // ⚠️ REMOVED DUPLICATE CODE BLOCK that was causing errors.
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Default melee attack if player runs into PeterMon
        if (_isDisabled) return;
        if (collision.gameObject.TryGetComponent<Player>(out var player))
            player.TakeDamage(_projectileDamage); 
    }
    #endregion

    #region DisableBehavior
    public override void DisableBehavior(float duration)
    {
        if (_isDisabled) return;
        StartCoroutine(DisableRoutine(duration));
    }

    private IEnumerator DisableRoutine(float time)
    {
        _isDisabled = true;
        yield return new WaitForSeconds(time);
        _isDisabled = false;
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        base.Die();
        
        // Find Spawner instance (Quick Fix for Manager access)
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 25% chance (0.00% <= roll < 25.00%)
            if (roll < 0.25f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
            }
            // Drop GreenTea with 5% chance (25.00% <= roll < 30.00%)
            else if (roll < 0.30f)
            {
                spawner.DropCollectible(CollectibleType.GreenTea, transform.position);
            }
        }
        else
        {
            Debug.LogWarning("[PeterMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}