using UnityEngine;
using System.Collections;

public class GoldenMon : Enemy
{
    #region Fields
    [Header("GoldenMon Settings")]
    //[SerializeField] private float _danceDuration = 3f;
    [SerializeField] private int _breakPlatformCount = 2;
    [SerializeField] private int _coinDropMultiplier = 5; // Multiplier for massive coin drop
    #endregion

    #region Movement/Attack
    public override void Move()
    {
        Debug.Log($"{name} dances elegantly across the map!");
        // TODO: Implement actual elegant movement here
    }

    public void DanceAttack()
    {
        Debug.Log($"{name} performs a golden dance attack!");
        // TODO: Implement AOE dance attack logic here
    }

    public void BreakPlatform()
    {
        Debug.Log($"{name} destroys {_breakPlatformCount} platforms!");
        // TODO: Implement platform destruction logic here
    }

    public void DropGoldenCoins()
    {
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[GoldenMon] CollectibleSpawner not found for coin drop!");
            return;
        }

        int baseCoins = Random.Range(10, 20);
        int coins = baseCoins * _coinDropMultiplier;
        
        Debug.Log($"{name} drops {coins} GOLD coins!");

        // Spawn individual coins
        for (int i = 0; i < coins; i++)
        {
             spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
        }
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        // 1. Send OnEnemyDied event
        base.Die(); 

        // Find necessary Managers
        CardManager cardManager = FindFirstObjectByType<CardManager>();
        Player player = FindFirstObjectByType<Player>(); // Find player to check MuslceDuck status
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();

        // --- 2. Guaranteed Career Card Drop ---
        if (cardManager != null)
        {
            // Assuming AddCareerCard() handles spawning the CardPickup collectible
            cardManager.AddCareerCard(); 
            Debug.Log("[GoldenMon] Guaranteed Career Card dropped.");
        }
        else
        {
            Debug.LogWarning("[GoldenMon] CardManager not found!");
        }

        // --- 3. Special Token Drop (MuscleDuck/Berserk Condition) ---
        // MuscleDuck ID Enum is DuckCareer.Muscle = 10 (from PDF)
        // We check if the nearby player is currently in the MuscleDuck state.
        if (player != null && player.GetCurrentCareerID() == DuckCareer.Muscle) 
        {
             if (spawner != null)
             {
                 spawner.DropCollectible(CollectibleType.Token, transform.position);
                 Debug.Log("[GoldenMon] MuscleDuck Bonus: Dropped 1 Token!");
             }
             else
             {
                 Debug.LogWarning("[GoldenMon] Spawner missing for Token drop!");
             }
        }
        
        // --- 4. Massive Coin Drop ---
        DropGoldenCoins(); 
    }
    #endregion
}