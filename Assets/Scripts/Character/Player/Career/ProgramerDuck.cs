using UnityEngine;
using System.Collections;

/// <summary>
/// ProgrammerDuck – Utility / Debuff career (ID 8, Tier A)
/// Skill (BlueScreen): Stun enemies in 4 Block radius.
/// BuffMon: LotteryMon -> +10 Coin, KahootMon -> 25% chance to disable.
/// BuffMap: School -> Wall behind pushes slowly.
/// </summary>
public class ProgrammerDuck : Player
{
    #region Fields
    [Header("Programmer Settings")]
    [SerializeField] private GameObject _codeEffect;
    [SerializeField] private GameObject _bugBombEffect;
    [SerializeField] private float _stunRadius = 4f;       // BlueScreen 4 Block
    [SerializeField] private float _stunDuration = 3f;       // (Using skeleton's 3s for stun time)
    
    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 27f;   // 27 Sec
    [SerializeField] private float _skillCooldown = 23f;   // 23 Sec
    
    [Header("Attack Settings")]
    [SerializeField] private float _bugBombRange = 4f;     // BugBomb 4 Block
    [SerializeField] private float _chargeRange = 6f;      // ChargeAttack 6 Block

    private bool _isSkillActive;
    private bool _isCooldown;
    
    // Reference to the Spawner for BuffMon
    private EnemySpawner _enemySpawner;
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies ProgrammerDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // 1. BuffMap Logic
        var map = GetCurrentMapType();
        if (map == MapType.School)
        {
            ApplySchoolMapBuff();
            Debug.Log("[ProgrammerDuck] Map Buff applied: School (Wall pushes slowly).");
        }

        // 2. BuffMon Setup (Passive, Continuous)
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        // Subscribe to CareerSwitcher Revert Event for cleanup
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnRevertToDefaultEvent += HandleCareerRevert;
        }

        if (_enemySpawner == null)
        {
            Debug.LogWarning("[ProgrammerDuck] EnemySpawner not found. Cannot apply BuffMon continuously.");
            return; 
        }

        // 2A. Subscribe to Event: ApplyBuffToNewEnemy whenever a new enemy spawns
        _enemySpawner.OnEnemySpawned += ApplyBuffToNewEnemy;
        
        // 2B. Apply Buff to enemies already in the scene (initial check)
        ApplyBuffsToExistingEnemies(careerData); 

        Debug.Log("[ProgrammerDuck] BuffMon Listener successfully registered for LotteryMon/KahootMon.");
    }

    private void ApplySchoolMapBuff()
    {
        // ASSUME: DuckCareerData มีค่าความเร็ว _schoolWallPushSpeed (เช่น 0.3f)
        float slowSpeed = 0.3f; // ใช้ค่าคงที่แทนชั่วคราว
        
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
 
            // mapGen.SetWallPushSpeed(slowSpeed); 
            Debug.Log($"[ProgrammerDuck] Slowing down School wall (TODO) to {slowSpeed}.");
        }
    }

    /// <summary>
    /// Applies buffs to target enemies currently active in the scene (called once on switch).
    /// </summary>
    private void ApplyBuffsToExistingEnemies(DuckCareerData careerData)
    {
        // Target is LotteryMon or KahootMon
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int buffsApplied = 0;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy.EnemyType == EnemyType.LotteryMon || enemy.EnemyType == EnemyType.KahootMon) 
            {
                enemy.ApplyCareerBuff(careerData); 
                buffsApplied++;
            }
        }
        Debug.Log($"[ProgrammerDuck] BuffMon Logic applied to {buffsApplied} existing enemies.");
    }
    
    /// <summary>
    /// Event handler: Applies buff to enemies as they spawn.
    /// </summary>
    private void ApplyBuffToNewEnemy(Enemy newEnemy)
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // Target is LotteryMon or KahootMon
        if (newEnemy.EnemyType == EnemyType.LotteryMon || newEnemy.EnemyType == EnemyType.KahootMon) 
        {
            newEnemy.ApplyCareerBuff(careerData);
            Debug.Log($"[ProgrammerDuck] BuffMon applied to NEW: {newEnemy.EnemyType}.");
        }
    }

    // ---------------------------------------------------------------------------------
    // CLEANUP/REVERT LOGIC
    // ---------------------------------------------------------------------------------
    
    private void RevertSchoolMapBuff()
    {
        // ASSUME: DuckCareerData มีค่าความเร็ว default _defaultWallPushSpeed
        float defaultSpeed = 1.0f; // ใช้ค่าคงที่แทนชั่วคราว
        
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // 🚨 FIX 3: Revert Wall Push Speed
            // mapGen.SetWallPushSpeed(defaultSpeed); 
            Debug.Log($"[ProgrammerDuck] Reverting School wall speed (TODO) to {defaultSpeed}.");
        }
    }

    /// <summary>
    /// EVENT HANDLER: Called by CareerSwitcher.OnRevertToDefaultEvent for cleanup.
    /// </summary>
    private void HandleCareerRevert()
    {
        // --- 1. Map Buff Cleanup ---
        if (GetCurrentMapType() == MapType.School)
        {
            RevertSchoolMapBuff();
        }
        
        // --- 2. BuffMon Listener Cleanup ---
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemySpawned -= ApplyBuffToNewEnemy;
            Debug.Log("[ProgrammerDuck] BuffMon Listener UNREGISTERED (Cleanup complete).");
        }

        // 3. Unsubscribe from the CareerSwitcher event itself
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnRevertToDefaultEvent -= HandleCareerRevert;
        }
    }

    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(BlueScreenRoutine());
    }

    /// <summary>
    /// BlueScreen -> Stun Enemy in 4 Block
    /// </summary>
    private IEnumerator BlueScreenRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: BlueScreen! Duration: {_skillDuration}s");

        if (_codeEffect != null)
            Instantiate(_codeEffect, transform.position, Quaternion.identity);

        // Apply Stun effect (Instant)
        ApplyBlueScreenStun();

        // Wait for the full skill duration (27s)
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
        Debug.Log("[ProgrammerDuck] Cooldown finished!");
    }
    #endregion

    #region Skill Effects (BuffMon & Stun)
    /// <summary>
    /// Applies the BlueScreen stun (4 Block radius for 3 seconds)
    /// </summary>
    private void ApplyBlueScreenStun()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _stunRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                // Use DisableBehavior for a safe stun
                enemy.DisableBehavior(_stunDuration); 
                Debug.Log($"[{PlayerName}] BlueScreen: Stunned {enemy.name} for {_stunDuration}s.");
            }
        }
    }

    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] BugBomb (4 Block)
        Debug.Log($"[{PlayerName}] uses BugBomb (4 Block)!");
        if (_bugBombEffect != null)
            Instantiate(_bugBombEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _bugBombRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15); // Example damage
        }
    }

    public override void ChargeAttack(float power)
    {
        // [CareerAttack] BugBomb -> Add lenght attack to 6 (6 Block)
        Debug.Log($"[{PlayerName}] uses Charged BugBomb (6 Block)!");
        if (_bugBombEffect != null)
            Instantiate(_bugBombEffect, transform.position, Quaternion.identity);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _chargeRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 25); // Example damage
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Covered by Attack() (4 Block) and ChargeAttack() (6 Block)
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}