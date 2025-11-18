using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class LotteryMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    #region Fields
    [Header("LotteryMon State")]
    private int _chefCoinBonusMin = 0;
    private int _chefCoinBonusMax = 0;

    private float _nextAttackTime;
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        
        base.Start(); 
        
        // 2. Initialize custom timers using loaded data
        // _data.LotteryAttackCooldown from EnemyData
        _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
    }

protected override void Update()
    {
        if (_isDisabled) return;

        if (_target != null && Time.time >= _nextAttackTime)
        {
            Attack();
            // Use Data From EnemyData:Unique | Asset: _data.LotteryAttackCooldown
            _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
        }
        
        // ถ้า _target เป็น null (เช่น Player ตาย) ให้หยุดทำงาน
        if (_target == null) return; 
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
        if (player == null) return;
        {
            // Use Data From EnemyData:Unique | Asset: _data.LotteryCurseDuration
            player.ApplySpeedModifier(0.5f, _data.LotteryCurseDuration);
        }
        Debug.Log($"[Lottery] {player.name} got cursed: Speed reduced for {_data.LotteryCurseDuration}s!");
    }
    #endregion


    #region Buffs
        /// <summary>
        /// Overrides base method to receive ChefDuck's Coin Bonus Buff.
        /// </summary>
        public override void ApplyCareerBuff(DuckCareerData data)
        {
            if (data != null)
            {

                _chefCoinBonusMin = data.ChefMonCoinMinBonusValue; 
                _chefCoinBonusMax = data.ChefMonCoinMaxBonusValue;
                
                Debug.Log($"[LotteryMon] Chef Buff Applied: +{_chefCoinBonusMin}-{_chefCoinBonusMax} Bonus Coins.");
            }
        }
    #endregion


    #region Death/Drop
    /// <summary>
    /// Drops a guaranteed amount of Coin between min and max upon defeat.
    /// </summary>
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = _spawnerRef;
        
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


            // --- 2. ChefDuck Buff Drop Logic ---
            // Check if the ChefDuck coin bonus buff was applied
            if (_chefCoinBonusMax > 0)
            {
                int bonusAmount = Random.Range(_chefCoinBonusMin, _chefCoinBonusMax + 1);
                for (int i = 0; i < bonusAmount; i++)
                {
                    // Spawn bonus coins with slight scattering
                    spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
                }
                Debug.Log($"[LotteryMon] Chef Bonus Drop: +{bonusAmount} coins.");
            }
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[LotteryMon] CollectibleSpawner NOT INJECTED! Cannot drop items.");
        }
    }
    #endregion
}