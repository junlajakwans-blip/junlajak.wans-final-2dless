using UnityEngine;
using System.Collections;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// MotorcycleDuck – Movement / Evasion career (ID 7, Tier B)
/// Skill (Street Duck): Forward Dash + 15% damage immunity.
/// BuffMon: RedlightMon -> Jump Higher.
/// BuffMap: Road Traffic -> Always Red Light (Safety).
/// </summary>
public class MotorcycleDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("Motorcycle Settings")]
    [SerializeField] private GameObject _dashEffect;
    [SerializeField] private float _dashMultiplier = 3f;   // Speed multiplier for the dash
    [SerializeField] private float _dashDuration = 0.5f;   // How long the dash burst lasts
    [SerializeField] private float _immunityChance = 0.15f; // 15% Chance no take any damage
    [SerializeField] private float _jumpBonus = 1.2f;      // Jump Higher (BuffMon)

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 24f;   // 24 Sec
    [SerializeField] private float _skillCooldown = 18f;   // 18 Sec

    [Header("Attack Settings")]
    [SerializeField] private float _handlebarRange = 3f;   // 3 Block
    [SerializeField] private float _slideRange = 5f;       // 5 Block
    [SerializeField] private float _slideKnockback = 8f;   // Knockback Enemy

    private bool _isSkillActive; 
    private bool _isCooldown;
    private bool _isDashing;     
    private bool _hasJumpBuff;   
    
    // NEW/FIX: Fields for Passive BuffMon Logic
    private EnemySpawner _enemySpawner;
    private int _redlightMonCount = 0; 
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies MotorcycleDuck-specific buffs and registers listeners.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // 1. BuffMap Logic
        if (GetCurrentMapType() == MapType.RoadTraffic)
        {
            ApplyRoadTrafficBuff(true); // True to force permanent Red Light
            Debug.Log("[MotorcycleDuck] Map Buff applied: Road Traffic (Always Red).");
        }

        // 2. BuffMon Setup (Passive, Continuous)
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnRevertToDefaultEvent += HandleCareerRevert;
        }

        if (_enemySpawner == null)
        {
            Debug.LogWarning("[MotorcycleDuck] EnemySpawner not found. Cannot apply BuffMon continuously.");
            return; 
        }

        // 2A. Subscribe to Events: Handle spawning and dying of RedlightMon
        _enemySpawner.OnEnemySpawned += ApplyBuffToNewEnemy;
        // Need to subscribe to enemy death event. Assume EnemySpawner provides a list of active enemies
        // and we will subscribe to their individual OnEnemyDied event (or use a cleaner global event).
        // --- For simplicity, we assume EnemySpawner exposes the death event for all active enemies ---
        
        // 2B. Apply Buff to enemies already in the scene (initial check and count update)
        _redlightMonCount = 0; // Reset count
        ApplyBuffsToExistingEnemies(careerData); 

        Debug.Log($"[MotorcycleDuck] BuffMon Listener registered. RedlightMon Count: {_redlightMonCount}");
    }

    /// <summary>
    /// FIX: Forces all RedlightMon to the specified state (Road Traffic BuffMap).
    /// </summary>
    private void ApplyRoadTrafficBuff(bool forceRed)
    {
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        string targetState = forceRed ? "Red" : "Green"; // Red is the intended buff (Safety)
        
        foreach (var light in trafficLights)
        {
            light.ForceSignalState(targetState, true); // Force state permanently
        }
        Debug.Log($"[MotorcycleDuck] Forcing {trafficLights.Length} traffic lights to {targetState}.");
    }

    /// <summary>
    /// HELPER: Applies Buffs to existing enemies and updates the count.
    /// </summary>
    private void ApplyBuffsToExistingEnemies(DuckCareerData careerData)
    {
        // Target is RedlightMon
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        _redlightMonCount = 0;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy.EnemyType == EnemyType.RedlightMon) 
            {
                enemy.ApplyCareerBuff(careerData); 
                enemy.OnEnemyDied += HandleRedlightMonDeath; // Subscribe to death event
                _redlightMonCount++;
            }
        }
        UpdateJumpBuffStatus();
    }
    
    /// <summary>
    /// HELPER: Event handler: Applies buff to enemies as they spawn and updates count.
    /// </summary>
    private void ApplyBuffToNewEnemy(Enemy newEnemy)
    {
        var careerData = _careerSwitcher?.CurrentCareer; 
        if (careerData == null) return;
        
        // Target is RedlightMon
        if (newEnemy.EnemyType == EnemyType.RedlightMon) 
        {
            // Apply Buff (for jump status check on player side)
            newEnemy.ApplyCareerBuff(careerData);
            
            // Subscribe to death event
            newEnemy.OnEnemyDied += HandleRedlightMonDeath;
            
            // Update count and jump buff status
            _redlightMonCount++;
            UpdateJumpBuffStatus();
            
            Debug.Log($"[MotorcycleDuck] BuffMon applied to NEW: {newEnemy.EnemyType}. Count: {_redlightMonCount}");
        }
    }
    
    /// <summary>
    /// HELPER: Event handler for when a RedlightMon is defeated.
    /// </summary>
    private void HandleRedlightMonDeath(Enemy deadEnemy)
    {
        if (deadEnemy.EnemyType == EnemyType.RedlightMon)
        {
            _redlightMonCount--;
            UpdateJumpBuffStatus();
            Debug.Log($"[MotorcycleDuck] RedlightMon died. Remaining Count: {_redlightMonCount}");

            // Unsubscribe from its death event to prevent memory leaks/errors
            deadEnemy.OnEnemyDied -= HandleRedlightMonDeath; 
        }
    }
    
    /// <summary>
    /// Updates the player's jump buff status based on the RedlightMon count.
    /// </summary>
    private void UpdateJumpBuffStatus()
    {
        _hasJumpBuff = _redlightMonCount > 0;
        Debug.Log($"[MotorcycleDuck] Jump Buff Status: {_hasJumpBuff}");
    }

    // ---------------------------------------------------------------------------------
    // CLEANUP/REVERT LOGIC
    // ---------------------------------------------------------------------------------
    
    private void RevertRoadTrafficBuff()
    {
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in trafficLights)
        {
            // By setting forcePermanent to false, we allow it to return to its natural cycle
            light.ForceSignalState("Green", false); // Force a switch to Green to start natural cycle again
        }
        Debug.Log("[MotorcycleDuck] Reverting Road Traffic Buff: Lights cycle normally.");
    }

    /// <summary>
    /// EVENT HANDLER: Called by CareerSwitcher.OnRevertToDefaultEvent for cleanup.
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
            _enemySpawner.OnEnemySpawned -= ApplyBuffToNewEnemy;
            Debug.Log("[MotorcycleDuck] BuffMon Listener UNREGISTERED (Cleanup complete).");
        }
        
        // Ensure all existing RedlightMon's death events are unsubscribed
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
             if (enemy.EnemyType == EnemyType.RedlightMon)
             {
                 enemy.OnEnemyDied -= HandleRedlightMonDeath;
             }
        }
        
        // 3. Unsubscribe from the CareerSwitcher event itself
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnRevertToDefaultEvent -= HandleCareerRevert;
        }
        
        _hasJumpBuff = false; // Reset BuffMon Status
    }
    
    #endregion

    #region ISkillUser Implementation
    // ... (UseSkill, StreetDuckSkillRoutine, DashRoutine, OnSkillCooldown, CooldownRoutine remain the same) ...
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(StreetDuckSkillRoutine());
    }

    /// <summary>
    /// Street Duck -> Forward Dash -> Immune
    /// </summary>
    private IEnumerator StreetDuckSkillRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: Street Duck! Duration: {_skillDuration}s");

        // 1. Apply Forward Dash (short burst)
        StartCoroutine(DashRoutine());

        // 2. Wait for the full skill duration (24s)
        yield return new WaitForSeconds(_skillDuration);
        
        _isSkillActive = false;
        OnSkillCooldown();
    }
    
    /// <summary>
    /// The short dash action (0.5s)
    /// </summary>
    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        if (_dashEffect != null)
            Instantiate(_dashEffect, transform.position, Quaternion.identity);

        // Temporarily boost speed
        ApplySpeedModifier(_dashMultiplier, _dashDuration);

        yield return new WaitForSeconds(_dashDuration);
        _isDashing = false;
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
        Debug.Log("[MotorcycleDuck] Cooldown finished!");
    }
    #endregion

    #region Immunity & Movement
    /// <summary>
    /// [Immune] If use skill 15% Chance no take any damage
    /// </summary>
    public override void TakeDamage(int damage)
    {
        // Check if skill is active and if the 15% chance succeeds
        if (_isSkillActive && Random.value < _immunityChance)
        {
            Debug.Log($"[{PlayerName}] IMMUNE! Dodged {damage} damage (15% chance).");
            return; // Skip damage
        }
        
        base.TakeDamage(damage);
    }

    /// <summary>
    /// BuffMon -> Jump Higher (if _hasJumpBuff is true)
    /// </summary>
    public override void Jump()
    {
        // Apply jump buff if active
        if (_hasJumpBuff)
        {
            // ASSUME: _jumpForce is defined in base Player
            _rigidbody.AddForce(Vector2.up * (_jumpForce * _jumpBonus), ForceMode2D.Impulse);
            Debug.Log($"[{PlayerName}] Jumped HIGH! ({_jumpForce * _jumpBonus})");
        }
        else
        {
            base.Jump(); // Use default jump force
        }
    }
    #endregion

    #region IAttackable Implementation
    // ... (Attack, ChargeAttack, RangeAttack, ApplyDamage methods remain the same) ...
    public override void Attack()
    {
        // [CareerAttack] Handlebar Swing (3 Block)
        Debug.Log($"[{PlayerName}] uses Handlebar Swing (3 Block)!");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _handlebarRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 20); // Example damage
        }
    }

    public override void ChargeAttack(float power)
    {
        // Power Slide -> Knockback Enemy (5 Block)
        Debug.Log($"[{PlayerName}] uses Power Slide (5 Block)!");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _slideRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                // Apply damage
                ApplyDamage(enemy, 25);
                
                // Apply Knockback
                if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 direction = (enemy.transform.position - transform.position).normalized;
                    rb.AddForce(direction * _slideKnockback, ForceMode2D.Impulse);
                }
            }
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Covered by Attack() and ChargeAttack()
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}