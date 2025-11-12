using UnityEngine;
using System.Collections;

/// <summary>
/// Dedicated Manager for all time-based effects (Buffs, Debuffs, etc.)
/// This centralizes all Coroutine logic for buffs to ensure scalability and stability.
/// </summary>
public class BuffManager : MonoBehaviour
{
    // Singleton Pattern for easy global access from CollectibleItem.cs
    public static BuffManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
    
    /// <summary>
    /// Entry point for all collectible items to delegate their time-based effects.
    /// </summary>
    public void ApplyCollectibleBuff(CollectibleType type, Player player, int healAmount, float duration)
    {
        switch (type)
        {
            case CollectibleType.Coffee:
                StartCoroutine(CoffeeBuffRoutine(player, healAmount, duration));
                break;
            case CollectibleType.MooKrata:
                StartCoroutine(MooKrataBuffRoutine(duration));
                break;
            // Add other collectible buff logic here
        }
    }

    private IEnumerator CoffeeBuffRoutine(Player player, int healAmount, float duration)
    {
        if (player == null) yield break;

        int oldHP = player.CurrentHealth;
        player.Heal(healAmount);
        Debug.Log($"[BuffManager] Coffee: Heal +{healAmount} applied to {player.PlayerName}");
        
        yield return new WaitForSeconds(duration);

        // Revert HP logic
        if (player != null && !player.IsDead && player.CurrentHealth > oldHP)
        {
            player.TakeDamage(player.CurrentHealth - oldHP); 
            Debug.Log($"[BuffManager] Coffee expired. HP reverted for {player.PlayerName}");
        }
    }

    private IEnumerator MooKrataBuffRoutine(float duration)
    {
        // Apply effect to all enemies in the scene
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.DisableBehavior(duration);

        Debug.Log($"[BuffManager] MooKrata: Disabling {enemies.Length} enemies for {duration}s");
        yield return new WaitForSeconds(duration);
        Debug.Log("[BuffManager] MooKrata ended.");
    }
}