using UnityEngine;
using System.Collections;

/// <summary>
/// FireFighterDuck – Utility / Crowd Control career (ID 6, Tier A) 
/// Skill (WaterSplash): Break floor, Stun/Push enemies right. 
/// BuffMon: MooPingMon -> Drop Buff Item Random 1 piece. 
/// BuffMap: Road Traffic -> All Green Light, No Platform Break. 
/// </summary>
public class FireFighterDuck : Player, ISkillUser, IAttackable
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
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies FireFighter-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var map = GetCurrentMapType();

        // 1. BuffMap Logic
        // Road Traffic -> All Green Light & BreakPlatform no break 
        if (map == MapType.RoadTraffic)
        {
            ApplyRoadTrafficBuff();
            Debug.Log("[FireFighterDuck] Map Buff applied: Road Traffic (All Green, No Break).");
        }

        // 2. BuffMon Logic (Handled in UseSkill)
        Debug.Log("[FireFighterDuck] BuffMon (MooPingMon) logic is active when skill is used.");
    }

    private void ApplyRoadTrafficBuff()
    {
        // Find all RedlightMon and force them to Green
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in trafficLights)
        {
            // TODO: Requires a public method on RedlightMon to set state
            // light.ForceState("Green"); 
        }
        
        // Find MapGenerator to disable platform breaking
        var mapGen = FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
        {
            // TODO: Requires a public property on MapGeneratorBase
            // mapGen.IsPlatformBreakable = false;
        }
    }
    #endregion

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
        
        // 2. Apply BuffMon (MooPingMon drop item)
        ApplyBuffMonEffect();

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

    /// <summary>
    /// BuffMon -> MooPingMon -> Drop Buff Item Random 1 piece 
    /// (Following ChefDuck's pattern, this triggers *once* when skill is used)
    /// </summary>
    private void ApplyBuffMonEffect()
    {
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        if (spawner == null) 
        {
            Debug.LogWarning("[FireFighterDuck] CollectibleSpawner not found for BuffMon!");
            return;
        }
        
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.MooPingMon)
            {
                // Force drop one random item (e.g., GreenTea)
                spawner.SpawnAtPosition(enemy.transform.position); // SpawnAtPosition spawns a random item
                Debug.Log($"[FireFighterDuck] BuffMon: Forced MooPingMon to drop a random item!");
                break; // Only affects one MooPingMon per skill use
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