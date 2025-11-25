using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 
using System.Linq; // Required for Player.GetCurrentCareerID() logic if used

public class GoldenMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    #region Movement/Attack
    public override void Move()
    {
        Debug.Log($"{name} dances elegantly across the map! (Speed: {Speed:F1})");
        // TODO: Implement actual elegant movement here
    }

    public void DanceAttack()
    {
        Debug.Log($"{name} performs a golden dance attack!");
        // TODO: Implement AOE dance attack logic here
    }

    public void BreakPlatform()
    {
        //  Use Data From EnemyData:Unique | Asset: _data.GoldenMonBreakPlatformCount
        Debug.Log($"{name} destroys {_data.GoldenMonBreakPlatformCount} platforms!");
        // TODO: Implement platform destruction logic here
    }

    /// <summary>
    /// Drops a guaranteed amount of golden coins based on EnemyData multipliers.
    /// </summary>
    public void DropGoldenCoins()
    {
        CollectibleSpawner spawner = _spawnerRef;
        if (spawner == null)
        {
            Debug.LogWarning("[GoldenMon] CollectibleSpawner NOT INJECTED for coin drop!");
            return;
        }

        //  Use Data From EnemyData:Unique | Asset: Min/Max Coin ก่อนคูณ และตัวคูณ
        int baseCoins = Random.Range(_data.GoldenMonBaseMinCoin, _data.GoldenMonBaseMaxCoin + 1);
        int coins = baseCoins * _data.GoldenMonCoinDropMultiplier;
        
        Debug.Log($"{name} drops {coins} GOLD coins! (Base: {baseCoins}, Multiplier: {_data.GoldenMonCoinDropMultiplier})");

        // Spawn individual coins (scattered position)
        for (int i = 0; i < coins; i++)
        {
            spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
        }
    }
    #endregion

#region Death/Drop
    /// <summary>
    /// Guaranteed drop of Career Card + massive Coin drop + potential Token bonus.
    /// </summary>
    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;

        CardManager cardManager = _cardManagerRef;
        Player player = _playerRef;
        CollectibleSpawner spawner = _spawnerRef;
        Vector3 enemyDeathPosition = transform.position;
        
        // --- 2. Guaranteed Career Card Drop (Data-Linked) ---
        if (cardManager != null && _data != null)
        {
            // 🚨 Check drop chance (should be 1.0 for guaranteed)
            if (UnityEngine.Random.value < _data.GoldenCardDropChance)
            {
                // Note: The CardManager.AddCareerCard() method implicitly uses CardType.Career
                // which aligns with the GoldenGuaranteedCardType setting.
                cardManager.AddCareerCard();
                
                Debug.Log($"[GoldenMon] Card Dropped ({_data.GoldenGuaranteedCardType}) with chance {_data.GoldenCardDropChance * 100:F0}%");
            }
        }
        else
        {
            Debug.LogWarning("[GoldenMon] CardManager or EnemyData NOT INJECTED! Cannot drop card.");
        }


        // --- 3. Special Token Drop (MuscleDuck/Berserk Condition) ---
        // MuscleDuck ID Enum is DuckCareer.Muscle = 10
        if (player != null && player.GetCurrentCareerID() == DuckCareer.Muscle) 
        {
             if (spawner != null)
             {
                 spawner.DropCollectible(CollectibleType.Token, enemyDeathPosition);
                 Debug.Log("[GoldenMon] MuscleDuck Bonus: Dropped 1 Token!");
             }
             else
             {
                 Debug.LogWarning("[GoldenMon] Spawner NOT INJECTED for Token drop!");
             }
        }
        
        // --- 4. Massive Coin Drop ---
        DropGoldenCoins(); 
        OnEnemyDied?.Invoke(this); // Event จะถูกส่งออกไป
    }
    #endregion
}