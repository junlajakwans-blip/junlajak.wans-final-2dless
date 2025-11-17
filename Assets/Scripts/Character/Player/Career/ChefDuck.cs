using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// ChefDuck – AoE / Support career (ID 2, Tier A)
/// Implements BuffMon and BuffMap logic via OOP override.
/// </summary>
public class ChefDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("ChefDuck Settings")]
    [SerializeField] private GameObject _panEffect;
    [SerializeField] private float _burnRange = 5f; // 5 Block
    [SerializeField] private int _burnDamage = 25;
    [SerializeField] private float _speedMultiplier = 1.5f;
    [SerializeField] private float _buffTime = 5f; // Duration for temp speed boost / enemy disable
    [SerializeField] private float _skillDuration = 26f; // 26 Sec
    [SerializeField] private float _skillCooldown = 20f; // 20 Sec
    [SerializeField] private int _minCoinBonus = 3; // 3 Coin
    [SerializeField] private int _maxCoinBonus = 8; // 8 Coin
    
    private bool _isSkillActive;
    private bool _isCooldown;
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies ChefDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // GetCurrentMapType() is inherited from Player.cs
        var map = GetCurrentMapType(); 

        // 1. BuffMap Logic
        switch (map)
        {
            case MapType.Kitchen:
                var mapGen = FindFirstObjectByType<MapGeneratorBase>();
                if (mapGen != null)
                {
                    // TODO: Implement actual wall push speed adjustment in MapGeneratorBase
                    Debug.Log("[ChefDuck] Map Buff applied: Kitchen → Wall behind pushes slowly.");
                }
                break;
            default:
                Debug.Log($"[ChefDuck] No map-specific buff active. (Current: {map})");
                break;
        }

        // 2. BuffMon Logic (Passive check)
        Debug.Log("[ChefDuck] BuffMon (Doggo, MooPing, Peter, Lottery) is active when skill is used.");
    }
    #endregion

    #region ISkillUser Implementation

    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown)
        {
            Debug.Log($"[{PlayerName}] Skill is not ready! Active: {_isSkillActive}, Cooldown: {_isCooldown}");
            return;
        }
        StartCoroutine(DuckliciousRoutine());
    }

    /// <summary>
    /// Ducklicious → Roast Duckeddon Chef Skill
    /// </summary>
    private IEnumerator DuckliciousRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"[{PlayerName}] uses skill: Ducklicious - Roast Duckeddon! Duration: {_skillDuration}s");
        
        ApplyBurnDamage(); 
        StartCoroutine(CookBuffRoutine());

        yield return new WaitForSeconds(_skillDuration);
        
        _isSkillActive = false;
        OnSkillCooldown(); 
    }
    
    /// <summary>
    /// (Override) Starts the skill cooldown coroutine.
    /// </summary>
    public override void OnSkillCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }
    
    private IEnumerator CooldownRoutine() // Skill Cooldown
    {
        _isCooldown = true;
        Debug.Log($"[{PlayerName}] skill on cooldown for {_skillCooldown}s.");
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log($"[{PlayerName}] skill ready!");
    }

    #endregion
    
    #region Attack Effects

    /// <summary>
    /// Applies the 5-block burn damage (Roast Duckeddon)
    /// </summary>
    private void ApplyBurnDamage()
    {
        if (_panEffect != null)
            Instantiate(_panEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _burnRange); // 5 Block range
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
            {
                ApplyDamage(target, _burnDamage); 
                Debug.Log($"[{PlayerName}] Roasting {hit.name} for {_burnDamage} damage!");
            }
        }
    }

    /// <summary>
    /// BuffMon Logic: Disables specific enemies and forces LotteryMon to drop coins.
    /// </summary>
    private IEnumerator CookBuffRoutine()
    {
        Debug.Log($"[{PlayerName}] starts CookBuff! Enemy debuff duration: {_buffTime}s");
        
        ApplySpeedModifier(_speedMultiplier, _buffTime); // Apply Player speed boost
        
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            switch (enemy.EnemyType)
            {
                case EnemyType.DoggoMon: // DoggoMon -> No Barking
                case EnemyType.MooPingMon: // MooPingMon -> No ThrowSkewer
                case EnemyType.PeterMon: // PeterMon -> No Attack
                    enemy.DisableBehavior(_buffTime); 
                    Debug.Log($"[{PlayerName}] BuffMon applied: {enemy.EnemyType} attacks disabled for {_buffTime}s");
                    break;
                
                // FIX: ใช้ LotteryMon ตามโค้ดของคุณ และแก้ไข Logic ให้ถูกต้อง
                case EnemyType.LotteryMon: 
                    // LotteryMon -> % Drop Coin Between 3-8 Coin
                    int bonusCoin = Random.Range(_minCoinBonus, _maxCoinBonus + 1);
                    AddCoin(bonusCoin); // Add coin directly to player
                    Debug.Log($"[{PlayerName}] BuffMon applied: LotteryMon dropped {bonusCoin} bonus coins instantly.");
                    break;
            }
        }

        yield return new WaitForSeconds(_buffTime); 
        Debug.Log($"[{PlayerName}] CookBuff (Temp Buffs) ended!");
    }
    
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] Flying Pan (Assuming 1.5f range for base attack)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15);
        }
    }

    public override void ChargeAttack(float power)
    {
        // Spicy Mode -> Add lenght attack Between 2-4
        float range = Mathf.Lerp(2f, 4f, power);
        int baseDamage = 20;
        int scaledDamage = Mathf.RoundToInt(baseDamage * Mathf.Clamp(power, 1f, 2f)); 

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, scaledDamage);
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Flying Pan 2 Block
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= 2f)
        {
            if (target.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 15);
        }
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}