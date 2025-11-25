using UnityEngine;
using System.Collections; 
using System; // For Enum or Action (if needed)

public class RedlightMon : Enemy
{
    // NOTE: _data field is inherited from Enemy.cs

    #region Fields
    [Header("RedlightMon State")]
    [SerializeField] private string _signalState = "Green"; // Current state: "Red" or "Green"

    private float _nextAttackTime;
    private float _nextSwitchTime;

    private bool _isForcedByBuff = false;
    #endregion

    #region Unity Lifecycle

    
    protected override void Start()
    {
        // This ensures Enemy.cs calls InitializeFromData() to load _data, HP, Speed, etc.
        base.Start();

        // Load Redlight-specific settings from _data
        _nextSwitchTime = Time.time + _data.RedlightSwitchInterval;
        _nextAttackTime = Time.time + _data.RedlightCarCooldown;
    }

    protected override void Update()
    {
        if (_isDisabled || _isForcedByBuff) return;
        
        // **[FIX 1]: Target Check**
        // RedlightMon ไม่ได้ใช้ _target สำหรับการเคลื่อนที่/ตรวจจับระยะ
        // แต่ถ้าไม่มีเป้าหมาย (Player ตาย) ก็ควรหยุด Attack
        if (_target == null) return; 

        // Check and execute attack if the light is green and cooldown is ready
        if (_signalState == "Green" && Time.time >= _nextAttackTime)
        {
            Attack();
            // Use Data From EnemyData:Unique | Asset: RedlightCarCooldown
            _nextAttackTime = Time.time + _data.RedlightCarCooldown;
        }

        // Check and switch light state
        if (Time.time >= _nextSwitchTime)
        {
            if (_signalState == "Red")
            {
                WarnPlayer();
            }
            SwitchLightState();
            // Use Data From EnemyData:Unique | Asset: RedlightSwitchInterval
            _nextSwitchTime = Time.time + _data.RedlightSwitchInterval;
        }
    }
    #endregion

#region Combat
    public override void Attack()
    {
        if (_signalState != "Green") return; 
        
        Debug.Log($"{name} spawns cars to attack!");
        SpawnCarAttack();
    }


    /// <summary>
    /// Spawns cars using the Object Pool Manager based on EnemyData count.
    /// Uses the asset naming convention for car tags.
    /// </summary>
    public void SpawnCarAttack()
    {

        IObjectPool pool = FindFirstObjectByType<ObjectPoolManager>();
        if (pool == null)
        {
            Debug.LogError("[RedlightMon] Cannot find ObjectPoolManager (IObjectPool) to spawn cars!");
            return;
        }

        // ref Prefab in ObjectPoolManager (Tag)
        // Convention: map_asset_RoadTraffic_Car_[int]
        const string CAR_TAG_PREFIX = "map_asset_RoadTraffic_Car_";
        // Link Max Car Type from ENEMYDATA
        int maxCarTypes = _data.RedlightMaxCarTypes; 
        int count = _data.RedlightSpawnCarCount;
        
        Vector3 spawnPosition = transform.position;
        
        Debug.Log($"[RedlightMon] Spawning {count} cars rush forward!");
        
        for (int i = 0; i < count; i++)
        {
            //Random Car
            int carIndex = UnityEngine.Random.Range(1, maxCarTypes + 1); 
            
            // 2. Create Tag  Convention: map_asset_RoadTraffic_Car_1
            string carTag = CAR_TAG_PREFIX + carIndex; 

            // 3. Spawn From Pool
            GameObject car = pool.SpawnFromPool(carTag, spawnPosition, Quaternion.identity);

            if (car != null)
            {
                // TODO: Add initialization for car movement/damage here
                // Note: ถ้ารถเป็น Projectile ต้องเรียก car.GetComponent<Projectile>().SetDependencies(_poolRef, carTag);
                // แต่โค้ดนี้ไม่ได้ใช้ Projectile.cs จึงไม่จำเป็นต้องทำ
                Debug.Log($"Spawned Car Tag: {carTag} at {spawnPosition}");
            }
            else
            {
                 Debug.LogWarning($"[RedlightMon] Failed to spawn car with tag: {carTag}. Check ObjectPoolManager Prefab list.");
            }
        }
    }
    #endregion


    #region Light Logic
    public void SwitchLightState()
    {
        _signalState = _signalState == "Red" ? "Green" : "Red";
        Debug.Log($"Traffic light switched to {_signalState}");
        
        if (_signalState == "Green")
        {
            _nextAttackTime = Time.time;
        }
    }

    public void WarnPlayer()
    {
        Debug.Log($"{name} warns player before light changes to Green!");
        // TODO: Implement visual/audio warning cue here
    }

    public void ForceSignalState(string state, bool forcePermanent)
    {
        // 1. Logic: ตั้งค่าสถานะไฟ
        _signalState = state; 
        
        // 2. Logic: เปิด/ปิด Master Switch
        _isForcedByBuff = forcePermanent; 
        
        // ... (logic อื่นๆ) ...
    }
    #endregion

#region Death/Drop
    /// <summary>
    /// Called when this enemy dies. Implements item drop logic using EnemyData.
    /// </summary>
    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        CollectibleSpawner spawner = _spawnerRef;
        Vector3 enemyDeathPosition = transform.position;
        
        if (spawner != null && _data != null)
        {
            float roll = UnityEngine.Random.value;
            
            // Use Data From EnemyData:Unique | Drop Chance จาก Asset
            float coinChance = _data.RedlightCoinDropChance;
            float coffeeChance = _data.RedlightCoffeeDropChance;

            // Chacne Token (Coin chance + Coffee chance)
            float totalCoffeeChance = coinChance + coffeeChance;
            
            // Drop Coin: (roll < 30%)
            if (roll < coinChance)
            {
                spawner.DropCollectible(CollectibleType.Coin, enemyDeathPosition);
                Debug.Log($"[RedlightMon] Dropped: Coin (Chance: {coinChance * 100:F0}%)");
            }
            // Drop Coffee: (e.g., 30% <= roll < 35%)
            else if (roll < totalCoffeeChance)
            {
                spawner.DropCollectible(CollectibleType.Coffee, enemyDeathPosition); 
                Debug.Log($"[RedlightMon] Dropped: Coffee (Chance: {coffeeChance * 100:F0}%)");
            }
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[RedlightMon] CollectibleSpawner NOT INJECTED! Cannot drop items.");
        }
        OnEnemyDied?.Invoke(this); // Event จะถูกส่งออกไป
    }
    #endregion
}