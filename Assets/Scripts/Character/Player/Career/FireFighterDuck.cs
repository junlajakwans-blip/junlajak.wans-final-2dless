using UnityEngine;
using System.Collections;

/// <summary>
/// FireFighterDuck – Utility / Crowd Control career (ID 6, Tier A) 
/// Skill (WaterSplash): Break floor, Stun/Push enemies right. 
/// BuffMon: MooPingMon -> Drop Buff Item Random 1 piece. 
/// BuffMap: Road Traffic -> All Green Light, No Platform Break. 
/// </summary>
public class FireFighterDuck : Player
{
    #region Fields
    [Header("FireFighter Settings")]
    [SerializeField] private GameObject _waterBallPrefab; // For Attack()
    [SerializeField] private GameObject _splashEffect; // For ChargeAttack()
    [SerializeField] private GameObject _waterSplashSkillEffect; // For UseSkill()

    [SerializeField] private float _waterBallRange = 2f; // 2 Block 
    [SerializeField] private float _splashAttackRange = 4f; // 4 Block 
    [SerializeField] private float _skillPushRange = 5f; // Range to find enemies to push
    [SerializeField] private float _skillPushForce = 10f; // Force to push enemies
    
    [SerializeField] private float _skillDuration = 28f; // 28 Sec 
    [SerializeField] private float _skillCooldown = 22f; // 22 Sec 

    private bool _isSkillActive;
    private bool _isCooldown;


    //Reference to the Spawner for BuffMon
    private EnemySpawner _enemySpawner;
    #endregion

#region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies FireFighter-specific buffs when the career is initialized.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // 1. BuffMap Logic
        var map = GetCurrentMapType();
        if (map == MapType.RoadTraffic)
        {
            ApplyRoadTrafficBuff();
            Debug.Log("[FireFighterDuck] Map Buff applied: Road Traffic (All Green, No Break).");
        }
        
        // 2. BuffMon Setup (Passive, Continuous)
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (_careerSwitcher != null)
            {
                _careerSwitcher.OnRevertToDefaultEvent += HandleCareerRevert;
            }

            if (_enemySpawner == null)
            {
                Debug.LogWarning("[FireFighterDuck] EnemySpawner not found. Cannot apply BuffMon continuously.");
                return; // Stop here if spawner is missing, but keep the CareerSwitcher subscription
            }

            // 2A. Subscribe to EnemySpawner Event
            _enemySpawner.OnEnemySpawned += ApplyBuffToNewEnemy;
            
            // 2B. Apply Buff to enemies already in the scene
            ApplyBuffsToExistingEnemies(careerData); 

            Debug.Log("[FireFighterDuck] BuffMon Listener successfully registered for MooPingMon.");
        }

    private void ApplyRoadTrafficBuff()
    {
        // 1. Buff: Road Traffic -> All Red Light
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in trafficLights)
        {
            // FIX: Call the assumed public method
            // light.ForceRed(); 
            Debug.Log($"[FireFighterDuck] RedlightMon {light.name} forced to Green.");
        }
        
        // 2. Buff: Road Traffic -> No Platform Break
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // TODO: Requires a public property on MapGeneratorBase
            // FIX: Set the property to disable breaking
            // mapGen.IsPlatformBreakable = false; 
            Debug.Log("[FireFighterDuck] Platform breaking feature disabled on MapGenerator.");
        }
    }

    // ---------------------------------------------------------------------------------
    // HELPER METHODS FOR BUFFMON
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Applies buffs to MooPingMon currently active in the scene (called once on switch).
    /// </summary>
    private void ApplyBuffsToExistingEnemies(DuckCareerData careerData)
    {
        // Target is MooPingMon (EnemyType.MooPingMon)
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int buffsApplied = 0;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy.EnemyType == EnemyType.MooPingMon) 
            {
                // MooPingMon.cs will handle the check for CareerID == Firefighter
                enemy.ApplyCareerBuff(careerData);
                buffsApplied++;
            }
        }
        Debug.Log($"[FireFighterDuck] BuffMon Logic applied to {buffsApplied} existing MooPingMons.");
    }
    
    /// <summary>
    /// Event handler: Applies buff to enemies as they spawn (subscribed to EnemySpawner.OnEnemySpawned).
    /// </summary>
    private void ApplyBuffToNewEnemy(Enemy newEnemy)
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // Target is MooPingMon
        if (newEnemy.EnemyType == EnemyType.MooPingMon) 
        {
            newEnemy.ApplyCareerBuff(careerData);
            Debug.Log($"[FireFighterDuck] BuffMon applied to NEW: {newEnemy.EnemyType}.");
        }
    }

    #endregion

    /// <summary>
    /// Resets Map Buffs when the career ends. This is called by HandleCareerRevert().
    /// </summary>
    private void RevertRoadTrafficBuff()
    {
        // 1. Revert: RedlightMon to default behavior
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in trafficLights)
        {
            // ASSUME: light.RevertToDefault() or similar public method exists on RedlightMon
            // light.RevertToDefault(); 
            Debug.Log($"[FireFighterDuck] RedlightMon {light.name} reverted to normal behavior.");
        }
        
        // 2. Revert: Platform Breakable to true
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // ASSUME: mapGen.IsPlatformBreakable is a public property
            // mapGen.IsPlatformBreakable = true; 
            Debug.Log("[FireFighterDuck] Platform breaking feature re-enabled on MapGenerator.");
        }
    }

    /// <summary>
    /// EVENT HANDLER: Called by CareerSwitcher.OnRevertToDefaultEvent for cleanup.
    /// This method performs all necessary unsubscriptions and state resets.
    /// </summary>
    private void HandleCareerRevert()
    {
        // --- 1. Map Buff Cleanup ---
        if (GetCurrentMapType() == MapType.RoadTraffic)
        {
            RevertRoadTrafficBuff();
        }
        
        // --- 2. BuffMon Listener Cleanup ---
        if (_enemySpawner != null)
        {
            // Unsubscribe from the EnemySpawner event
            _enemySpawner.OnEnemySpawned -= ApplyBuffToNewEnemy;
            Debug.Log("[FireFighterDuck] BuffMon Listener UNREGISTERED (Cleanup complete).");
        }

        // 3. Unsubscribe from the CareerSwitcher event itself
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnRevertToDefaultEvent -= HandleCareerRevert;
        }
    }



    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(WaterSplashRoutine());
    }

    /// <summary>
    /// WaterSplash -> Break 1 Floor Rightest -> Mon in front Stun and Move to Right
    /// </summary>
    private IEnumerator WaterSplashRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: WaterSplash! Duration: {_skillDuration}s");

        if (_waterSplashSkillEffect != null)
            Instantiate(_waterSplashSkillEffect, transform.position, Quaternion.identity);

        // 1. Apply Skill Effects (Break floor, Stun/Push enemies)
        ApplySkillEffects();

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        OnSkillCooldown();
    }
    
    public override void OnSkillCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("[FireFighterDuck] Cooldown finished!");
    }
    #endregion

    #region Skill Effects
    private void ApplySkillEffects()
    {
        // 1. Break 1 Floor (Rightest) 
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // TODO: Requires a public method on MapGeneratorBase
            // mapGen.BreakRightmostPlatform();
            Debug.Log("[FireFighterDuck] Breaking rightmost platform (TODO).");
        }

        // 2. Mon in front (Right) Stun and Move to Right 
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _skillPushRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                // Check if enemy is in front (Right)
                if (enemy.transform.position.x > transform.position.x)
                {
                    // Apply Stun (via DisableBehavior)
                    enemy.DisableBehavior(2f); // Stun for 2s
                    
                    // Apply Push to Right
                    if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        rb.AddForce(Vector2.right * _skillPushForce, ForceMode2D.Impulse);
                    }
                    Debug.Log($"[FireFighterDuck] Pushing {enemy.name} to the right!");
                }
            }
        }
    }

    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] WaterBall (2 Block) 
        Debug.Log($"[{PlayerName}] uses WaterBall (2 Block)!");

        if (_waterBallPrefab != null)
        Instantiate(_waterBallPrefab, transform.position, Quaternion.identity);
        
        // Apply AOE damage using the _waterBallRange
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _waterBallRange);
        foreach (var hit in hits)
        {
            var go = Instantiate(_waterBallPrefab, transform.position + (transform.right * 0.5f), Quaternion.identity);
            if (go.TryGetComponent<Projectile>(out var proj))
                proj.SetDamage(15);
        }
    }

    public override void ChargeAttack(float power)
    {
        // SplashAttack (4 Block) 
        Debug.Log($"[{PlayerName}] uses SplashAttack (4 Block)!");
        if (_splashEffect != null)
            Instantiate(_splashEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _splashAttackRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 20);
        }
    }

    public override void RangeAttack(Transform target)
    {
        // This is covered by Attack() (WaterBall) and ChargeAttack() (SplashAttack)
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}