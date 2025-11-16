using UnityEngine;
using System.Collections;

/// <summary>
/// MuscleDuck – Berserker / Special career (ID 10, Tier S+)
/// Acquired via 5-card exchange. Max 2 uses per round.
/// Skill (Hulk Smash): Destroy All Enemy And obstacle.
/// Passive: Immortal (No TakeDamage), Coinx2 (AllMap).
/// BuffMon: GoldenMon -> Drop Coinx2, Token -> Drop 1 Token (Handled by GoldenMon.Die()).
/// BuffMap: Roar -> All Mon Fear.
/// </summary>
public class MuscleDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("MuscleDuck Settings")]
    [SerializeField] private GameObject _rageEffect;
    [SerializeField] private GameObject _smashEffect;
    [SerializeField] private GameObject _roarEffect;
    
    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 35f;   // 35 Sec
    [SerializeField] private float _baseCooldown = 40f;    // First use 40 sec
    [SerializeField] private float _cooldownIncrease = 15f; // plus 15 Sec every after usetime
    [SerializeField] private int _maxUsesPerRound = 2;       // ≤ 2 per Round

    [Header("Attack Settings")]
    [SerializeField] private float _ironFistRange = 3f;    // 3 Block
    [SerializeField] private float _pumpedUpRange = 8f;    // 8 Block

    private bool _isSkillActive;
    private bool _isCooldown;
    private int _usesThisRound = 0;
    private float _currentCooldown;
    #endregion

    #region Buffs & Passives Initialization

    /// <summary>
    /// (Override) Applies MuscleDuck-specific passives when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // Set the dynamic cooldown for the first use
        _currentCooldown = _baseCooldown;
        
        // 1. BuffMap Logic
        // Roar -> All Mon Fear
        ApplyMapBuffRoar();
        
        // 2. Passive Logic
        // Coinx2 [AllMap]
        // TODO: Requires a public method in Currency.cs or Player.cs
        // SetCoinMultiplier(2); 
        Debug.Log("[MuscleDuck] Passive Buff applied: Coinx2 (AllMap) (TODO).");

        // BuffMon: GoldenMon -> Drop Coinx2, Token -> Drop 1 Token
        // This is handled by GoldenMon.Die() checking if the player is MuscleDuck.
        Debug.Log("[MuscleDuck] BuffMon (GoldenMon) logic is active.");
    }

    /// <summary>
    /// Roar -> All Mon Fear
    /// </summary>
    private void ApplyMapBuffRoar()
    {
        if (_roarEffect != null)
            Instantiate(_roarEffect, transform.position, Quaternion.identity);
            
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            // TODO: Requires a public "Flee()" or "Fear()" method on Enemy.cs
            // enemy.ApplyFear(5f); // Example: Flee for 5 seconds
        }
        Debug.Log($"[MuscleDuck] ROAR! Applied Fear to {enemies.Length} enemies (TODO).");
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown || _usesThisRound >= _maxUsesPerRound)
        {
            Debug.Log($"[MuscleDuck] Skill not ready! (Active: {_isSkillActive}, Cooldown: {_isCooldown}, Uses: {_usesThisRound}/{_maxUsesPerRound})");
            return;
        }
        StartCoroutine(HulkSmashRoutine());
    }

    /// <summary>
    /// Hulk Smash -> Destroy All Enemy And obstacle
    /// </summary>
    private IEnumerator HulkSmashRoutine()
    {
        _isSkillActive = true;
        _usesThisRound++;
        Debug.Log($"{PlayerName} uses skill: HULK SMASH! (Use {_usesThisRound}/{_maxUsesPerRound}). Duration: {_skillDuration}s");

        if (_smashEffect != null)
            Instantiate(_smashEffect, transform.position, Quaternion.identity);

        // 1. Destroy All Enemies
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.TakeDamage(9999); // Instant kill
        }
        
        // 2. Destroy All Obstacles
        // TODO: Requires finding objects with "Obstacle" tag/layer
        // var obstacles = FindObjectsByType<Obstacle>(...);
        // foreach (var obstacle in obstacles) obstacle.Destroy();

        Debug.Log($"[MuscleDuck] Smashed {enemies.Length} enemies and 0 obstacles (TODO).");

        yield return new WaitForSeconds(_skillDuration);
        
        _isSkillActive = false;
        OnSkillCooldown();
    }
    
    public override void OnSkillCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    /// <summary>
    /// CoolDown Card plus 15 Sec every after usetime
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        Debug.Log($"[{PlayerName}] skill on cooldown for {_currentCooldown}s.");
        
        yield return new WaitForSeconds(_currentCooldown);
        
        // Increase cooldown for the *next* use
        _currentCooldown += _cooldownIncrease; 
        
        _isCooldown = false;
        Debug.Log($"[{PlayerName}] skill ready! Next cooldown will be {_currentCooldown}s.");
    }
    #endregion

    #region Immunity
    /// <summary>
    /// (Override) TakeDamage: No (Immortal)
    /// </summary>
    public override void TakeDamage(int damage)
    {
        // MuscleDuck is IMMORTAL while this career is active.
        Debug.Log($"[{PlayerName}] IMMORTAL! Ignored {damage} damage.");
        return; 
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] Iron Fist(AOE) (3 Block)
        Debug.Log($"[{PlayerName}] uses Iron Fist (3 Block)!");
        if (_rageEffect != null)
            Instantiate(_rageEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _ironFistRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 50); // High damage
        }
    }

    public override void ChargeAttack(float power)
    {
        // Pumped Up (8 Block)
        Debug.Log($"[{PlayerName}] uses Pumped Up (8 Block)!");
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _pumpedUpRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 100); // Massive damage
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Covered by Attack() (3 Block) and ChargeAttack() (8 Block)
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}