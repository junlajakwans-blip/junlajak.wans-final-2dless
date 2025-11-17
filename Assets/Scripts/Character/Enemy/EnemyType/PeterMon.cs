using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class PeterMon : Enemy // Removed IMoveable as Enemy already has Move()
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs
    
    #region Fields
    [Header("Projectile Attack")]
    [SerializeField] private GameObject _projectilePrefab; // Prefab must remain in MonoBehaviour
    [SerializeField] private Transform _firePoint; // Fire point must remain in MonoBehaviour


    private float _hoverOffsetY;
    private float _nextAttackTime;
    private Vector2 _direction = Vector2.left; // Unused for hovering, but kept if needed
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        
        base.Start();

        //  2. Initialize runtime state and custom timers using loaded data
        _hoverOffsetY = transform.position.y;
        _nextAttackTime = Time.time + _data.PeterAttackCooldown; // ใช้ค่าจาก Asset
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
        
        //  DetectPlayer use _detectionRange in Enemy.cs
        if (_target != null && DetectPlayer(_target.position))
            AttackPlayer();
    }
    #endregion

    #region Movement
    /// <summary>
    /// Hover behaviour (Visual motion only)
    /// </summary>
    private void HoverMotion()
    {
        // Use Data From EnemyData:Unique | Asset: _data.PeterHoverSpeed และ _data.PeterHoverAmplitude
        float newY = _hoverOffsetY + Mathf.Sin(Time.time * _data.PeterHoverSpeed) * _data.PeterHoverAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // PeterMon is stationary and only hovers.
    public override void Move() { } 
    
    // ... (IMoveable methods omitted for brevity) ...
    #endregion

    #region Combat
    private void AttackPlayer()
    {
        // Use Data From EnemyData:Unique | Asset: _data.PeterAttackCooldown
        if (Time.time < _nextAttackTime) return;
        if (_target == null) return;
        
        // Use Data From EnemyData:Unique | Asset: _data.PeterAttackRange
        if (Vector2.Distance(transform.position, _target.position) <= _data.PeterAttackRange)
        {
            ShootProjectile();
            _nextAttackTime = Time.time + _data.PeterAttackCooldown;
        }
    }
    
    private void ShootProjectile()
    {
        if (_projectilePrefab == null || _firePoint == null || _target == null) return;
        
        // NOTE: ควรใช้ Object Pooling แทน Instantiate
        var proj = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);

        // Calculate direction and set velocity (Aiming at target)
        if (proj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
            // Use Data From EnemyData:Unique | Asset: _data.PeterProjectileSpeed
            rb.linearVelocity = aim * _data.PeterProjectileSpeed;
        }
        
        // Set damage value using the Projectile component
        if (proj.TryGetComponent<Projectile>(out var projectile))
            // Use Data From EnemyData:Unique | Asset: _data.PeterProjectileDamage
            projectile.SetDamage(_data.PeterProjectileDamage);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Default melee attack if player runs into PeterMon
        if (_isDisabled) return;
        if (collision.gameObject.TryGetComponent<Player>(out var player))
            // Use Data From EnemyData:Unique | Asset: _data.PeterProjectileDamage
            player.TakeDamage(_data.PeterProjectileDamage); 
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
        // NOTE: PeterMon doesn't move, so no need to zero out velocity, just set flag
        _isDisabled = true;
        yield return new WaitForSeconds(time);
        _isDisabled = false;
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null && _data != null)
        {
            float roll = Random.value;
            
            float totalGreenTeaChance = _data.PeterCoinDropChance + _data.PeterGreenTeaDropChance;
            
            // Drop Coin
            if (roll < _data.PeterCoinDropChance)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
            }
            // Drop GreenTea
            else if (roll < totalGreenTeaChance)
            {
                spawner.DropCollectible(CollectibleType.GreenTea, transform.position);
            }
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[PeterMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}