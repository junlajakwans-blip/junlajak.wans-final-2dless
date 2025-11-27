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
        if (UnityEngine.Random.value < _data.GoldenCardDropChance)
        {
            cardManager.AddCareerCard();
            Debug.Log($"[GoldenMon] Card Dropped ({_data.GoldenGuaranteedCardType})");
        }
    }

    // --- 3. Special Token Drop (MuscleDuck Condition) ---
    if (player != null && player.GetCurrentCareerID() == DuckCareer.Muscle)
        spawner?.DropCollectible(CollectibleType.Token, enemyDeathPosition);

    // --- 4. Massive Coin Drop ---
    DropGoldenCoins();

    // call event แค่ครั้งเดียว
    OnEnemyDied?.Invoke(this);

    // ปิด event ไม่ให้ยิงซ้ำหลัง return จาก Pool
    OnEnemyDied = null;

    // ปิดการทำงานของ GoldenMon ทันทีในเฟรมนี้ (กัน Update ทำงานต่อ)
    gameObject.SetActive(false);
}

    #endregion
}