using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class LotteryMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    #region Fields
    [Header("LotteryMon State")]
    // 🔥 ลบ Fields ตัวเลขทั้งหมดออก (ถูกย้ายไป EnemyData แล้ว)

    private float _nextAttackTime;
    // 🔥 _attackCooldown ถูกลบออก (ใช้ _data.LotteryAttackCooldown แทน)
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        // 🚨 1. เรียก Base.Start() เพื่อให้ Enemy.InitializeFromData() รันก่อน
        base.Start(); 
        
        // 🚨 2. Initialize custom timers using loaded data
        // _data.LotteryAttackCooldown ถูกโหลดจาก EnemyData
        _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
    }

    protected override void Update()
    {
        if (_isDisabled) return;
        
        if (_target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null) _target = player.transform;
        }

        if (_target != null && Time.time >= _nextAttackTime)
        {
            Attack();
            // Use Data From EnemyData:Unique | Asset: _data.LotteryAttackCooldown
            _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
        }
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        if (_target == null || _target.TryGetComponent<Player>(out var player) == false) return;

        float roll = Random.value;
        
        // Use Data From EnemyData:Unique | Asset: _data.LotteryLuckFactor
        if (roll < _data.LotteryLuckFactor) // Chance for good luck
        {
            ApplyGoodLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player wins!");
        }
        else // Chance for bad luck
        {
            ApplyBadLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player loses!");
        }
    }

    public void ApplyGoodLuck(Player player)
    {
        // Use Data From EnemyData:Unique | Asset: LotteryGoodLuckMinCoin และ LotteryGoodLuckMaxCoin
        int coinAmount = Random.Range(_data.LotteryGoodLuckMinCoin, _data.LotteryGoodLuckMaxCoin + 1);
        player.AddCoin(coinAmount);
        Debug.Log($"[Lottery] {player.name} got lucky: +{coinAmount} Coin! (Roll chance: {_data.LotteryLuckFactor * 100:F0}%)");
    }

    public void ApplyBadLuck(Player player)
    {
        if (BuffManager.Instance != null)
        {
            // Use Data From EnemyData:Unique | Asset: _data.LotteryCurseDuration
            player.ApplySpeedModifier(0.5f, _data.LotteryCurseDuration);
        }
        Debug.Log($"[Lottery] {player.name} got cursed: Speed reduced for {_data.LotteryCurseDuration}s!");
    }
    #endregion

    #region Death/Drop
    /// <summary>
    /// Drops a guaranteed amount of Coin between min and max upon defeat.
    /// </summary>
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null && _data != null)
        {
            // Use Data From EnemyData:Unique | Asset: LotteryMinCoinDrop และ LotteryMaxCoinDrop
            int coinAmount = Random.Range(_data.LotteryMinCoinDrop, _data.LotteryMaxCoinDrop + 1);
            
            for (int i = 0; i < coinAmount; i++)
            {
                // Spawn individual coins (scattered position)
                spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
            }
            Debug.Log($"[LotteryMon] Dropped {coinAmount} coins on defeat (Range: {_data.LotteryMinCoinDrop} - {_data.LotteryMaxCoinDrop}).");
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[LotteryMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}