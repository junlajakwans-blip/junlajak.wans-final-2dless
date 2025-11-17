using UnityEngine;
using System.Collections;

/// <summary>
/// ProgrammerDuck – Utility / Debuff career (ID 8, Tier A)
/// Skill (BlueScreen): Stun enemies in 4 Block radius.
/// BuffMon: LotteryMon -> +10 Coin, KahootMon -> 25% chance to disable.
/// BuffMap: School -> Wall behind pushes slowly.
/// </summary>
public class ProgrammerDuck : Player, ISkillUser, IAttackable
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
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies ProgrammerDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var map = GetCurrentMapType();

        // 1. BuffMap Logic
        // School -> Wall behind Push Slowly
        if (map == MapType.School)
        {
            ApplySchoolMapBuff();
            Debug.Log("[ProgrammerDuck] Map Buff applied: School (Wall pushes slowly).");
        }

        // 2. BuffMon Logic (Passive check)
        Debug.Log("[ProgrammerDuck] BuffMon (LotteryMon, KahootMon) logic is active when skill is used.");
    }

    private void ApplySchoolMapBuff()
    {
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // TODO: Requires a public method on MapGeneratorBase
            // mapGen.SetWallPushSpeed(0.3f); // Example: Slower speed
        }
        Debug.Log("[ProgrammerDuck] Slowing down School wall (TODO).");
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

        // 1. Apply BuffMon effects (Instant)
        ApplyBuffMonEffects();
        
        // 2. Apply Stun effect (Instant)
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

    /// <summary>
    /// Applies BuffMon effects once at the start of the skill.
    /// LotteryMon -> +10 Coin, KahootMon -> 25% chance no attack
    /// </summary>
    private void ApplyBuffMonEffects()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.LotteryMon)
            {
                // LotteryMon -> Add Luck -> Drop 10 Coin
                AddCoin(10); 
                Debug.Log("[ProgrammerDuck] BuffMon: Forced LotteryMon to drop 10 Coin!");
            }
            
            if (enemy.EnemyType == EnemyType.KahootMon)
            {
                // KahootMon -> 25% chane no attack
                if (Random.value < 0.25f) 
                {
                    // Disable for the *full skill duration* if 25% chance hits
                    enemy.DisableBehavior(_skillDuration); 
                    Debug.Log($"[ProgrammerDuck] BuffMon: KahootMon 25% chance SUCCESS. Disabled for {_skillDuration}s.");
                }
                else
                {
                    Debug.Log("[ProgrammerDuck] BuffMon: KahootMon 25% chance failed.");
                }
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