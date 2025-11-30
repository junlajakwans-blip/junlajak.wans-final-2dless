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


    //chef
    private bool _isAttackDisabled = false;
    //doctor
    private float _doctorAttackSkipChance = 0f;

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

#region Buffs from Career

    /// <summary>
    /// Overrides base method to receive ChefDuck's Buff (Disable Attack).
    /// </summary>
public override void ApplyCareerBuff(DuckCareerData data)
    {
        if (data == null) return;

        // 1. ChefDuck Buff Logic (Full Disable Attack)
        if (data.CareerID == DuckCareer.Chef)
        {
            _isAttackDisabled = true;
            _doctorAttackSkipChance = 0f; // Reset Doctor Buff
            Debug.Log("[PeterMon] Chef Buff Applied: Attack DISABLED (100% skip).");
        }
        
        // 2. DoctorDuck Buff Logic (Chance to Skip Attack)
        else if (data.CareerID == DuckCareer.Doctor)
        {
            _doctorAttackSkipChance = data.PeterMonAttackSkipChance; // Get 30% chance
            _isAttackDisabled = false; // Reset Chef Buff
            Debug.Log($"[PeterMon] Doctor Buff Applied: {_doctorAttackSkipChance * 100:F0}% chance to skip attack.");
        }
        // NOTE: PeterMon.cs จะเช็คค่า _isAttackDisabled หรือ _doctorAttackSkipChance ใน AttackPlayer()
    }
    #endregion


    #region Combat

    private void AttackPlayer()
    {
        // 1. ChefDuck Buff Check (Full Disable - Highest Priority)
        if (_isAttackDisabled)
        {
            // Reset CD to prevent instant attack if the buff ends
            _nextAttackTime = Time.time + _data.PeterAttackCooldown; 
            Debug.Log("[PeterMon] Attack skipped due to Chef Buff (100% Disable).");
            return;
        }
        
        // 2. DoctorDuck Buff Check (30% Chance to Skip)
        if (_doctorAttackSkipChance > 0f)
        {
            float roll = Random.value;
            // Roll: 0.0 to 1.0. If roll is less than 0.30 (30%), skip the attack.
            if (roll < _doctorAttackSkipChance) 
            {
                Debug.Log($"[PeterMon] Attack skipped due to Doctor Buff! (Roll: {roll:F2} < Skip Chance: {_doctorAttackSkipChance * 100:F0}%)");
                // Reset CD even if skipped, to prevent instant attack next frame
                _nextAttackTime = Time.time + _data.PeterAttackCooldown; 
                return;
            }
        }

        // --- Original Attack Timing and Range Checks ---
        
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
        
        // [FIX 2.1]: ตรวจสอบ Pool Reference ที่ถูก Inject
        if (_poolRef == null) 
        {
            Debug.LogError("[PeterMon] Object Pool NOT INJECTED! Cannot spawn projectile.");
            return;
        }

        // [FIX 2.2]: ใช้ Object Pooling แทน Instantiate
        string poolTag = _projectilePrefab.name; // ใช้ชื่อ Prefab เป็น Tag
        var go = _poolRef.SpawnFromPool(poolTag, _firePoint.position, Quaternion.identity);

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
            // Use Data From EnemyData:Unique | Asset: _data.PeterProjectileSpeed
            rb.linearVelocity = aim * _data.PeterProjectileSpeed;
        }
        
        // Set damage value using the Projectile component
        if (go.TryGetComponent<Projectile>(out var projectile))
        {
            // Use Data From EnemyData:Unique | Asset: _data.PeterProjectileDamage
            projectile.SetDamage(_data.PeterProjectileDamage);
        }
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
        // Guard: already dead
        if (_isDead) return;
        _isDead = true;

        Vector3 pos = transform.position;

        // Drop logic based on EnemyData
        if (_data != null)
        {
            float roll = Random.value;
            float totalGreenTeaChance = _data.PeterCoinDropChance + _data.PeterGreenTeaDropChance;

            // Drop Coin
            if (roll < _data.PeterCoinDropChance)
            {
                RequestDrop(CollectibleType.Coin);
                Debug.Log("[PeterMon] Dropped: Coin");
            }
            // Drop GreenTea
            else if (roll < totalGreenTeaChance)
            {
                RequestDrop(CollectibleType.GreenTea);
                Debug.Log("[PeterMon] Dropped: GreenTea");
            }
        }
        else
        {
            Debug.LogWarning("[PeterMon] EnemyData missing. Drop skipped.");
        }

        // Notify spawner and return to pool
        base.Die();
    }
    #endregion

}