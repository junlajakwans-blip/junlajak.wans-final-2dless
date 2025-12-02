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
    /// Spawn rushing cars from ObjectPool (RoadTraffic)
    /// Random car type + random lane + auto Y + auto scale + start movement
    /// </summary>
    public void SpawnCarAttack()
    {
        IObjectPool pool = FindFirstObjectByType<ObjectPoolManager>();
        if (pool == null)
        {
            Debug.LogError("[RedlightMon] Cannot find ObjectPoolManager to spawn cars!");
            return;
        }

        // Get player target for targeting cars
        Player player = _target?.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogWarning("[RedlightMon] Player target is null or missing Player component — cannot spawn targeting cars!");
            return;
        }

        string[] carTags =
        {
            "map_asset_RoadTraffic_RushCarRed",
            "map_asset_RoadTraffic_RushCarGreen",
            "map_asset_RoadTraffic_RushCarBlue"
        };

        int count = _data.RedlightSpawnCarCount;
        float[] laneYOffset = { -0.12f, -0.32f, -0.52f };

        Vector3 basePos = transform.position;
        Debug.Log($"[RedlightMon] 🚗 Spawn {count} cars targeting player!");

        for (int i = 0; i < count; i++)
        {
            string carTag = carTags[UnityEngine.Random.Range(0, carTags.Length)];
            float lane = laneYOffset[UnityEngine.Random.Range(0, laneYOffset.Length)];
            Vector3 spawnPos = new Vector3(basePos.x, lane, 0);

            GameObject car = pool.SpawnFromPool(carTag, spawnPos, Quaternion.identity);
            if (car == null)
            {
                Debug.LogWarning($"[RedlightMon] Failed to spawn car: {carTag}");
                continue;
            }

            // scaling
            car.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            // movement: target the player
            Vector3 directionToPlayer = (player.transform.position - spawnPos).normalized;
            
            if (car.TryGetComponent<Rigidbody2D>(out var rb))
            {
                // Combine leftward movement with targeting offset toward player's Y position
                // Primary: move left (across the road), secondary: bias toward player Y
                float targetYBias = Mathf.Sign(directionToPlayer.y) * Mathf.Abs(directionToPlayer.y) * _data.RedlightCarSpeed * 0.3f;
                rb.linearVelocity = new Vector2(-_data.RedlightCarSpeed, targetYBias);
                
                Debug.Log($"[RedlightMon] 🎯 Car spawned at {spawnPos}, targeting player at {player.transform.position}");
            }

            // ⚡ hit player → damage + despawn
            if (car.TryGetComponent<CarHit>(out var hit))
                hit.Init(pool, carTag, _data.RedlightCarDamage);
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


        Vector3 pos = transform.position;

        if (_data != null)
        {
            float roll = UnityEngine.Random.value;

            float coinChance   = _data.RedlightCoinDropChance;
            float coffeeChance = _data.RedlightCoffeeDropChance;
            float totalCoffeeChance = coinChance + coffeeChance;

            // Drop Coin
            if (roll < coinChance)
            {
                RequestDrop(CollectibleType.Coin);
                Debug.Log($"[RedlightMon] Dropped: Coin (Chance: {coinChance * 100:F0}%)");
            }
            // Drop Coffee
            else if (roll < totalCoffeeChance)
            {
                RequestDrop(CollectibleType.Coffee);
                Debug.Log($"[RedlightMon] Dropped: Coffee (Chance: {coffeeChance * 100:F0}%)");
            }
        }
        else
        {
            Debug.LogWarning("[RedlightMon] EnemyData missing. Drop skipped.");
        }

        base.Die();
    }
    #endregion

}