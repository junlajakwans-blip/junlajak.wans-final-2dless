using UnityEngine;
using System.Collections; // Required for Coroutines if adding Disable/Warn logic

public class RedlightMon : Enemy
{
    #region Fields
    [Header("RedlightMon Settings")]
    [SerializeField] private string _signalState = "Green"; // Current state: "Red" or "Green"
    [SerializeField] private float _cooldownTime = 3f;      // Time between car spawns
    [SerializeField] private float _switchInterval = 5f;    // Time before light state switches
    [SerializeField] private int _spawnCarCount = 2;        // Number of cars to spawn

    private float _nextAttackTime;
    private float _nextSwitchTime;
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        _nextSwitchTime = Time.time + _switchInterval;
        _nextAttackTime = Time.time + _cooldownTime;
    }

    protected override void Update()
    {
        if (_isDisabled) return;

        // Check and execute attack if the light is green and cooldown is ready
        if (_signalState == "Green" && Time.time >= _nextAttackTime)
        {
            Attack();
            _nextAttackTime = Time.time + _cooldownTime;
        }

        // Check and switch light state
        if (Time.time >= _nextSwitchTime)
        {
            // If currently Red, warn the player first before switching to Green
            if (_signalState == "Red")
            {
                WarnPlayer();
                // We switch immediately after warning in this example, or you can delay the switch slightly here
            }
            SwitchLightState();
            _nextSwitchTime = Time.time + _switchInterval;
        }
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        if (_signalState != "Green") return; // Only attacks on Green light
        
        Debug.Log($"{name} spawns cars to attack!");
        SpawnCarAttack();
    }

    public void SpawnCarAttack()
    {
        Debug.Log($"{_spawnCarCount} cars rush forward!");
        // TODO: Implement actual car spawning using Object Pooling/Spawner
    }
    #endregion

    #region Light Logic
    public void SwitchLightState()
    {
        _signalState = _signalState == "Red" ? "Green" : "Red";
        Debug.Log($"Traffic light switched to {_signalState}");
        
        // If switched to Green, reset attack timer for immediate readiness
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
    #endregion

    #region Death/Drop
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 30% chance
            if (roll < 0.30f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[RedlightMon] Dropped: Coin ({roll:F2})");
            }
            // Drop Token with 5% chance (Tokens are often associated with special enemies)
            else if (roll < 0.35f)
            {
                spawner.DropCollectible(CollectibleType.Token, transform.position);
                Debug.Log($"[RedlightMon] Dropped: Token ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[RedlightMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}