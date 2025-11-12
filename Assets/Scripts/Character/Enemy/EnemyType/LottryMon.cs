using UnityEngine;
using System.Collections;

public class LotteryMon : Enemy
{
    #region Fields
    [Header("LotteryMon Settings")]
    [SerializeField] private float _luckFactor = 0.15f; // Chance (15%) of giving Good Luck
    [SerializeField] private float _curseDuration = 4f; // Bad Luck duration
    [SerializeField] private int _baseCoinDrop = 1;      // Base minimum coins on defeat (FIXED to 1)
    [SerializeField] private int _maxCoinDrop = 40;      // Max coins on defeat (BALANCED to 40)

    private float _nextAttackTime;
    private float _attackCooldown = 5f;
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        _nextAttackTime = Time.time + _attackCooldown;
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
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        if (_target == null || _target.TryGetComponent<Player>(out var player) == false) return;

        float roll = Random.value;
        
        if (roll < _luckFactor) // 15% chance for good luck
        {
            ApplyGoodLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player wins!");
        }
        else // 85% chance for bad luck
        {
            ApplyBadLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player loses!");
        }
    }

    public void ApplyGoodLuck(Player player)
    {
        // Give player a random amount of Coin immediately (1-10)
        int coinAmount = Random.Range(1, 11);
        player.AddCoin(coinAmount);
        Debug.Log($"[Lottery] {player.name} got lucky: +{coinAmount} Coin!");
    }

    public void ApplyBadLuck(Player player)
    {
        if (BuffManager.Instance != null)
        {
            player.ApplySpeedModifier(0.5f, _curseDuration);
        }
        Debug.Log($"[Lottery] {player.name} got cursed: Speed reduced for {_curseDuration}s!");
    }
    #endregion

    #region Death/Drop
    /// <summary>
    /// Drops a guaranteed amount of Coin between 1 and 40 upon defeat.
    /// </summary>
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            // Drop Coin amount between 1 and 40 (inclusive)
            int coinAmount = Random.Range(_baseCoinDrop, _maxCoinDrop + 1);
            
            for (int i = 0; i < coinAmount; i++)
            {
                // Spawn individual coins (scattered position)
                spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
            }
            Debug.Log($"[LotteryMon] Dropped {coinAmount} coins on defeat.");
        }
        else
        {
            Debug.LogWarning("[LotteryMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}