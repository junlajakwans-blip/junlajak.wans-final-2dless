using UnityEngine;
using System.Collections; 
using Random = UnityEngine.Random; // Ensure Random refers to Unity's Random

public class MamaMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs
    // NOTE: _target, _isDisabled, _currentHealth, _maxHealth are inherited.

    #region Fields
    [Header("Projectile Attack")]
    [SerializeField] private GameObject _noodleProjectilePrefab; // Prefab must remain in MonoBehaviour
    [SerializeField] private Transform _firePoint; // Fire point must remain in MonoBehaviour
    
    
    private float _nextAttackTime;
    private float _nextHealAttempt;
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        base.Start();

        //  2. Initialize custom timers using loaded data
        // _data is now guaranteed to be loaded and accessible here.
        _nextAttackTime = Time.time + _data.MamaAttackCooldown; 
        _nextHealAttempt = Time.time + _data.MamaHealCooldown;
    }

    protected override void Update()
    {

        if (_isDisabled) return;

        // [FIX 1]: ลบ Logic การค้นหา Player ใน Update() ออก
        // _target ถูก set ใน Enemy.SetDependencies() แล้ว
        if (_target == null) return; 

        // Check distance for attacks
        float distanceToPlayer = Vector2.Distance(transform.position, _target.position);

        if (distanceToPlayer <= _detectionRange)
        {
            // --- Attack Logic ---
            if (Time.time >= _nextAttackTime)
            {
                // Use Data From EnemyData:Unique | Asset: _data.MamaBoilRange
                if (distanceToPlayer <= _data.MamaBoilRange)
                {
                    BoilSplash();
                }
                else
                {
                    Attack();
                }
                // Use Data From EnemyData:Unique | Asset: _data.MamaAttackCooldown
                _nextAttackTime = Time.time + _data.MamaAttackCooldown;
            }

            // --- Heal Logic ---
            if (Time.time >= _nextHealAttempt)
            {
                // Use Data From EnemyData:Unique | Asset: _data.MamaHealChance
                if (_currentHealth < _maxHealth && Random.value < _data.MamaHealChance)
                {
                    RecoverHP();
                }
                // Use Data From EnemyData:Unique | Asset: _data.MamaHealCooldown
                _nextHealAttempt = Time.time + _data.MamaHealCooldown;
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

        // [FIX 2.1]: ตรวจสอบ Pool Reference ที่ถูก Inject
        if (_poolRef == null) 
        {
            Debug.LogError("[MamaMon] Object Pool NOT INJECTED! Cannot spawn projectile.");
            yield break;
        }

        // Use Data From EnemyData:Unique | Asset: _data.MamaNoodleCount
        string poolTag = _noodleProjectilePrefab.name;
        for (int i = 0; i < _data.MamaNoodleCount; i++)
        {
            var go = _poolRef.SpawnFromPool(poolTag, _firePoint.position, Quaternion.identity);
            
            if (go.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
                // Use Data From EnemyData:Unique | Asset: _data.MamaProjectileSpeed
                rb.linearVelocity = aim * _data.MamaProjectileSpeed;
            }

            if (go.TryGetComponent<Projectile>(out var proj))
                // Use Data From EnemyData:Unique | Asset: _data.MamaProjectileDamage
                proj.SetDamage(_data.MamaProjectileDamage); 
            
            yield return new WaitForSeconds(0.2f); 
        }
    }

    /// <summary>
    /// AOE damage attack for close-range defense.
    /// </summary>
    public void BoilSplash()
    {
        // Use Data From EnemyData:Unique | Asset: _data.MamaBoilRange
        Debug.Log($"[{name}] creates boiling splash in {_data.MamaBoilRange}m radius!");
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _data.MamaBoilRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out var player))
            {
                // Use Data From EnemyData:Unique | Asset: _data.MamaBoilDamage
                player.TakeDamage(_data.MamaBoilDamage);
            }
        }
    }
    #endregion

    #region Utility / Healing
    /// <summary>
    /// Recovers MamaMon's HP by a fixed amount (10 points).
    /// </summary>
    public void RecoverHP()
    {
        // NOTE: ค่า Heal 10 points ยังคงเป็น Hardcoded ถ้าต้องการให้ปรับได้ต้องเพิ่มใน EnemyData
        base.Heal(10); 
        Debug.Log($"[{name}] slurps noodles to heal HP!");
    }
    #endregion
    
    #region Death/Drop
    /// <summary>
    /// Called when this enemy dies. Implements item drop logic.
    /// </summary>
    public override void Die()
    {
   
        Vector3 pos = transform.position;

        // Drop logic based on EnemyData
        if (_data != null)
        {
            float roll = Random.value;

            // Chance grouping for GreenTea
            float totalChanceForGreenTea = _data.MamaCoinDropChance + _data.MamaGreenTeaDropChance;

            // Drop Coin: (roll < MamaCoinDropChance)
            if (roll < _data.MamaCoinDropChance)
            {
                RequestDrop(CollectibleType.Coin);
                Debug.Log($"[MamaMon] Dropped: Coin (Chance: {_data.MamaCoinDropChance * 100:F0}%)");
            }
            // Drop GreenTea: (MamaCoinChance <= roll < totalChanceForGreenTea)
            else if (roll < totalChanceForGreenTea)
            {
                RequestDrop(CollectibleType.GreenTea);
                Debug.Log($"[MamaMon] Dropped: GreenTea (Chance: {_data.MamaGreenTeaDropChance * 100:F0}%)");
            }
        }
        else
        {
            Debug.LogWarning("[MamaMon] EnemyData missing. Drop skipped.");
        }

        // Notify spawner and handle despawn
        base.Die();
    }
    #endregion

}